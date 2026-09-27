using Dapper;
using server.src.Data.Interfaces;
using server.src.Models;
using server.src.Repositories.Interfaces;

namespace server.src.Repositories;

public class NoteShareLinkRepository : INoteShareLinkRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public NoteShareLinkRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<string?> GetToken(int noteId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT token
            FROM note_share_links
            WHERE note_id = @NoteId
            """;

        await using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(sql, new { NoteId = noteId }, cancellationToken: cancellationToken));
    }

    public async Task<string> GetOrCreateToken(int noteId, string newToken, CancellationToken cancellationToken)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM note_share_links WITH (UPDLOCK, HOLDLOCK) WHERE note_id = @NoteId)
                INSERT INTO note_share_links (note_id, token) VALUES (@NoteId, @Token);

            SELECT token
            FROM note_share_links
            WHERE note_id = @NoteId;
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var token = await connection.QuerySingleAsync<string>(
            new CommandDefinition(sql, new { NoteId = noteId, Token = newToken }, transaction, cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return token;
    }

    public async Task<bool> Delete(int noteId, CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE FROM note_share_links
            WHERE note_id = @NoteId
            """;

        await using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { NoteId = noteId }, cancellationToken: cancellationToken));
        return affected > 0;
    }

    public async Task<SharedNote?> GetSharedNote(string token, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT n.title, n.content, COALESCE(u.display_name, u.username) AS author,
                   n.created_at, n.updated_at
            FROM note_share_links l
            INNER JOIN notes n ON n.id = l.note_id
            INNER JOIN users u ON u.id = n.user_id
            WHERE l.token = @Token AND n.deleted_at IS NULL
            """;

        await using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<SharedNote>(
            new CommandDefinition(sql, new { Token = token }, cancellationToken: cancellationToken));
    }
}
