using server.src.Models;

namespace server.src.Repositories.Interfaces;

public interface IEmailLogRepository
{
    Task<EmailLog> Create(EmailLog emailLog, CancellationToken cancellationToken);
    Task Update(EmailLog emailLog, CancellationToken cancellationToken);
    Task<IEnumerable<EmailLog>> GetPendingRetries(int batchSize, CancellationToken cancellationToken);
}
