using Dapper;
using server.src.Data.Interfaces;
using server.src.Models;
using server.src.Repositories.Interfaces;

namespace server.src.Repositories;

public class NoteShareRepository : INoteShareRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public NoteShareRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<string?> GetPermission(int noteId, int userId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CASE WHEN n.user_id = @UserId THEN N'owner' ELSE s.permission END
            FROM notes n
            LEFT JOIN note_shares s ON s.note_id = n.id AND s.user_id = @UserId
            WHERE n.id = @NoteId AND n.deleted_at IS NULL
            """;

        await using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<string?>(
            new CommandDefinition(sql, new { NoteId = noteId, UserId = userId }, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<NoteShare>> GetAll(int noteId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT s.note_id, s.user_id, u.username, u.display_name, u.email, s.permission, s.created_at
            FROM note_shares s
            INNER JOIN users u ON u.id = s.user_id
            WHERE s.note_id = @NoteId
            ORDER BY s.created_at, u.username
            """;

        await using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<NoteShare>(
            new CommandDefinition(sql, new { NoteId = noteId }, cancellationToken: cancellationToken));
    }

    public async Task<NoteShare> Upsert(int noteId, int userId, string permission, CancellationToken cancellationToken)
    {
        const string sql = """
            IF EXISTS (SELECT 1 FROM note_shares WITH (UPDLOCK, HOLDLOCK) WHERE note_id = @NoteId AND user_id = @UserId)
                UPDATE note_shares
                SET permission = @Permission
                WHERE note_id = @NoteId AND user_id = @UserId;
            ELSE
                INSERT INTO note_shares (note_id, user_id, permission)
                VALUES (@NoteId, @UserId, @Permission);

            SELECT s.note_id, s.user_id, u.username, u.display_name, u.email, s.permission, s.created_at
            FROM note_shares s
            INNER JOIN users u ON u.id = s.user_id
            WHERE s.note_id = @NoteId AND s.user_id = @UserId;
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var share = await connection.QuerySingleAsync<NoteShare>(
            new CommandDefinition(sql, new { NoteId = noteId, UserId = userId, Permission = permission }, transaction, cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return share;
    }

    public async Task<NoteShare?> UpdatePermission(int noteId, int userId, string permission, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE note_shares
            SET permission = @Permission
            WHERE note_id = @NoteId AND user_id = @UserId;

            SELECT s.note_id, s.user_id, u.username, u.display_name, u.email, s.permission, s.created_at
            FROM note_shares s
            INNER JOIN users u ON u.id = s.user_id
            WHERE s.note_id = @NoteId AND s.user_id = @UserId;
            """;

        await using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<NoteShare>(
            new CommandDefinition(sql, new { NoteId = noteId, UserId = userId, Permission = permission }, cancellationToken: cancellationToken));
    }

    public async Task<bool> Delete(int noteId, int userId, CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE FROM note_shares
            WHERE note_id = @NoteId AND user_id = @UserId
            """;

        await using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { NoteId = noteId, UserId = userId }, cancellationToken: cancellationToken));
        return affected > 0;
    }
}
