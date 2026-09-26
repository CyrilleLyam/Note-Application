using Dapper;
using server.src.Data.Interfaces;
using server.src.Models;
using server.src.Repositories.Interfaces;

namespace server.src.Repositories;

public class TagRepository : ITagRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TagRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<TagSummary>> GetAll(int userId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT t.name, COUNT(n.id) AS note_count
            FROM tags t
            INNER JOIN note_tags nt ON nt.tag_id = t.id
            INNER JOIN notes n ON n.id = nt.note_id AND n.deleted_at IS NULL
            WHERE t.user_id = @UserId
            GROUP BY t.name
            """;

        await using var connection = _connectionFactory.CreateConnection();
        var tags = await connection.QueryAsync<TagSummary>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
        return tags.OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase);
    }
}
