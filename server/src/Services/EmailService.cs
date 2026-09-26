using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using server.src.Config;
using server.src.Models;
using server.src.Repositories.Interfaces;
using server.src.Services.Interfaces;

namespace server.src.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IEmailLogRepository _emailLogRepository;

    public EmailService(ILogger<EmailService> logger, IEmailLogRepository emailLogRepository)
    {
        _logger = logger;
        _emailLogRepository = emailLogRepository;
    }

    public async Task SendWelcomeEmail(string toEmail, string username, CancellationToken cancellationToken = default)
    {
        const string templateType = "WelcomeEmail";
        const string subject = "Welcome to Note Application!";
        var payload = JsonSerializer.Serialize(new { username });

        var clientInfo = CreateSmtpClient();

        if (clientInfo == null)
        {
            _logger.LogInformation(
                "[EmailService] SMTP_HOST is not configured. Simulated sending welcome email to {Email} for user {Username}.",
                toEmail,
                username);

            await _emailLogRepository.Create(
                EmailLog.Simulated(toEmail, subject, templateType, payload),
                cancellationToken);

            return;
        }

        var (smtpClient, fromEmail) = clientInfo.Value;
        using (smtpClient)
        {
            var body = BuildWelcomeEmailBody(username);
            using var mailMessage = BuildMailMessage(fromEmail, toEmail, subject, body);

            const int maxImmediateAttempts = 3;
            var attempt = 0;
            Exception? lastException = null;

            while (attempt < maxImmediateAttempts)
            {
                attempt++;
                try
                {
                    _logger.LogInformation(
                        "Attempt {Attempt}/{MaxAttempts}: Sending welcome email to {Email}...",
                        attempt,
                        maxImmediateAttempts,
                        toEmail);

                    await smtpClient.SendMailAsync(mailMessage, cancellationToken);
                    _logger.LogInformation("Welcome email sent successfully to {Email}", toEmail);

                    await _emailLogRepository.Create(
                        EmailLog.Success(toEmail, subject, templateType, payload, attempt - 1),
                        cancellationToken);

                    return;
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    lastException = ex;
                    _logger.LogWarning(
                        ex,
                        "Attempt {Attempt} to send welcome email to {Email} failed.",
                        attempt,
                        toEmail);

                    if (attempt < maxImmediateAttempts)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
                    }
                }
            }

            _logger.LogError(
                lastException,
                "All {Attempts} immediate attempts to send welcome email to {Email} failed. Scheduling background retry.",
                maxImmediateAttempts,
                toEmail);

            try
            {
                await _emailLogRepository.Create(
                    EmailLog.PendingRetry(toEmail, subject, templateType, payload, lastException?.Message),
                    CancellationToken.None);
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Failed to record pending retry email log for {Email}", toEmail);
            }
        }
    }

    public async Task<bool> RetryEmail(EmailLog emailLog, CancellationToken cancellationToken = default)
    {
        var clientInfo = CreateSmtpClient();
        if (clientInfo == null)
        {
            _logger.LogWarning("[EmailService] Cannot retry email ID {Id}: SMTP_HOST is not configured.", emailLog.Id);
            return false;
        }

        var (smtpClient, fromEmail) = clientInfo.Value;
        using (smtpClient)
        {
            string body;
            if (emailLog.TemplateType == "WelcomeEmail")
            {
                var username = ExtractUsernameFromPayload(emailLog.Payload);
                body = BuildWelcomeEmailBody(username);
            }
            else
            {
                body = $"Email dispatched for {emailLog.TemplateType}";
            }

            using var mailMessage = BuildMailMessage(fromEmail, emailLog.Recipient, emailLog.Subject, body);

            try
            {
                _logger.LogInformation("Retrying email ID {Id} (Attempt {Attempt}/{MaxRetries}) to {Email}...",
                    emailLog.Id, emailLog.RetryCount + 1, emailLog.MaxRetries, emailLog.Recipient);

                await smtpClient.SendMailAsync(mailMessage, cancellationToken);

                _logger.LogInformation("Retry succeeded for email ID {Id} to {Email}", emailLog.Id, emailLog.Recipient);

                emailLog.Status = "SUCCESS";
                emailLog.SentAt = DateTime.UtcNow;
                emailLog.ErrorMessage = null;
                emailLog.NextRetryAt = null;

                await _emailLogRepository.Update(emailLog, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                emailLog.RetryCount += 1;
                emailLog.ErrorMessage = ex.Message;

                if (emailLog.RetryCount >= emailLog.MaxRetries)
                {
                    emailLog.Status = "FAILED";
                    emailLog.NextRetryAt = null;
                    _logger.LogError(ex, "Email ID {Id} reached max retries ({MaxRetries}). Marked as FAILED.",
                        emailLog.Id, emailLog.MaxRetries);
                }
                else
                {
                    emailLog.Status = "PENDING_RETRY";
                    var backoffMinutes = Math.Pow(2, emailLog.RetryCount) * 2;
                    emailLog.NextRetryAt = DateTime.UtcNow.AddMinutes(backoffMinutes);
                    _logger.LogWarning(ex, "Retry failed for email ID {Id}. Scheduled next retry at {NextRetryAt}.",
                        emailLog.Id, emailLog.NextRetryAt);
                }

                await _emailLogRepository.Update(emailLog, CancellationToken.None);
                return false;
            }
        }
    }

    private static (SmtpClient SmtpClient, string FromEmail)? CreateSmtpClient()
    {
        var smtpHost = EnvValidator.GetOptional("SMTP_HOST");
        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            return null;
        }

        var port = int.TryParse(EnvValidator.GetOptional("SMTP_PORT"), out var parsedPort) ? parsedPort : 587;
        var fromEmail = EnvValidator.GetOptional("SMTP_FROM") ?? "no-reply@noteapp.com";
        var smtpUsername = EnvValidator.GetOptional("SMTP_USERNAME");
        var smtpPassword = EnvValidator.GetOptional("SMTP_PASSWORD");

        var smtpClient = new SmtpClient(smtpHost, port)
        {
            EnableSsl = true
        };

        if (!string.IsNullOrWhiteSpace(smtpUsername) && !string.IsNullOrWhiteSpace(smtpPassword))
        {
            smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
        }

        return (smtpClient, fromEmail);
    }

    private static MailMessage BuildMailMessage(string fromEmail, string toEmail, string subject, string body)
    {
        var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        mailMessage.To.Add(toEmail);
        return mailMessage;
    }

    private static string BuildWelcomeEmailBody(string username) =>
        $"Hi {username},\n\nWelcome to Note Application! Your account has been registered successfully.\n\nBest regards,\nNote App Team";

    private static string ExtractUsernameFromPayload(string payloadJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            if (doc.RootElement.TryGetProperty("username", out var prop))
            {
                return prop.GetString() ?? string.Empty;
            }
        }
        catch
        {
        }
        return string.Empty;
    }
}
