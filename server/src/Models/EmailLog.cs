namespace server.src.Models;

public class EmailLog
{
    public int Id { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string TemplateType { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
    public string Status { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public DateTime? NextRetryAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public static EmailLog Success(string to, string subject, string template, string payload, int retryCount = 0) =>
        new()
        {
            Recipient = to,
            Subject = subject,
            TemplateType = template,
            Payload = payload,
            Status = "SUCCESS",
            RetryCount = retryCount,
            SentAt = DateTime.UtcNow
        };

    public static EmailLog Simulated(string to, string subject, string template, string payload) =>
        new()
        {
            Recipient = to,
            Subject = subject,
            TemplateType = template,
            Payload = payload,
            Status = "SIMULATED",
            SentAt = DateTime.UtcNow
        };

    public static EmailLog PendingRetry(
        string to,
        string subject,
        string template,
        string payload,
        string? error,
        int retryCount = 1,
        int maxRetries = 3,
        DateTime? nextRetryAt = null) =>
        new()
        {
            Recipient = to,
            Subject = subject,
            TemplateType = template,
            Payload = payload,
            Status = "PENDING_RETRY",
            RetryCount = retryCount,
            MaxRetries = maxRetries,
            NextRetryAt = nextRetryAt ?? DateTime.UtcNow.AddMinutes(2),
            ErrorMessage = error
        };
}
