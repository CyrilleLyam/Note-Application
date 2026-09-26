using server.src.Models;

namespace server.src.Services.Interfaces;

public interface IEmailService
{
    Task SendWelcomeEmail(string toEmail, string username, CancellationToken cancellationToken = default);
    Task<bool> RetryEmail(EmailLog emailLog, CancellationToken cancellationToken = default);
}
