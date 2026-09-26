using Dapper;
using server.src.Data.Interfaces;
using server.src.Models;
using server.src.Repositories.Interfaces;

namespace server.src.Repositories;

public class EmailLogRepository : IEmailLogRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EmailLogRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<EmailLog> Create(EmailLog emailLog, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO email_logs (recipient, subject, template_type, payload, status, retry_count, max_retries, next_retry_at, error_message, sent_at)
            OUTPUT INSERTED.id, INSERTED.recipient, INSERTED.subject, INSERTED.template_type,
                   INSERTED.payload, INSERTED.status, INSERTED.retry_count, INSERTED.max_retries,
                   INSERTED.next_retry_at, INSERTED.error_message, INSERTED.sent_at, INSERTED.created_at
            VALUES (@Recipient, @Subject, @TemplateType, @Payload, @Status, @RetryCount, @MaxRetries, @NextRetryAt, @ErrorMessage, @SentAt)
            """;

        await using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<EmailLog>(
            new CommandDefinition(sql, emailLog, cancellationToken: cancellationToken));
    }

    public async Task Update(EmailLog emailLog, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE email_logs
            SET status = @Status,
                retry_count = @RetryCount,
                next_retry_at = @NextRetryAt,
                error_message = @ErrorMessage,
                sent_at = @SentAt
            WHERE id = @Id
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(sql, emailLog, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<EmailLog>> GetPendingRetries(int batchSize, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (@BatchSize) id, recipient, subject, template_type, payload, status,
                   retry_count, max_retries, next_retry_at, error_message, sent_at, created_at
            FROM email_logs
            WHERE status = 'PENDING_RETRY'
              AND retry_count < max_retries
              AND (next_retry_at IS NULL OR next_retry_at <= SYSUTCDATETIME())
            ORDER BY created_at ASC
            """;

        await using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<EmailLog>(
            new CommandDefinition(sql, new { BatchSize = batchSize }, cancellationToken: cancellationToken));
    }
}
