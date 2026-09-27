using Dapper;
using server.src.Data.Interfaces;
using server.src.Dtos;
using server.src.Models;
using server.src.Repositories.Interfaces;

namespace server.src.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public NotificationRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<(IEnumerable<Notification> Items, int TotalCount)> GetAll(int userId, PaginationQueryDto queryDto, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*) FROM notifications WHERE user_id = @UserId;

            SELECT id, user_id, type, note_id, note_title, actor_name, permission, created_at, read_at
            FROM notifications
            WHERE user_id = @UserId
            ORDER BY created_at DESC, id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var parameters = new
        {
            UserId = userId,
            Offset = (queryDto.Page - 1) * queryDto.PageSize,
            queryDto.PageSize
        };

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        var totalCount = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<Notification>()).ToList();
        return (items, totalCount);
    }

    public async Task<int> GetUnreadCount(int userId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM notifications
            WHERE user_id = @UserId AND read_at IS NULL
            """;

        await using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
    }

    public async Task<Notification> Create(Notification notification, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO notifications (user_id, type, note_id, note_title, actor_name, permission)
            OUTPUT INSERTED.id, INSERTED.user_id, INSERTED.type, INSERTED.note_id, INSERTED.note_title,
                   INSERTED.actor_name, INSERTED.permission, INSERTED.created_at, INSERTED.read_at
            VALUES (@UserId, @Type, @NoteId, @NoteTitle, @ActorName, @Permission)
            """;

        await using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<Notification>(
            new CommandDefinition(sql, notification, cancellationToken: cancellationToken));
    }

    public async Task<bool> MarkRead(int id, int userId, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE notifications
            SET read_at = COALESCE(read_at, SYSUTCDATETIME())
            WHERE id = @Id AND user_id = @UserId
            """;

        await using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: cancellationToken));
        return affected > 0;
    }

    public async Task<int> MarkAllRead(int userId, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE notifications
            SET read_at = SYSUTCDATETIME()
            WHERE user_id = @UserId AND read_at IS NULL
            """;

        await using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
    }
}
