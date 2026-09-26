using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using server.src.Repositories.Interfaces;
using server.src.Services.Interfaces;

namespace server.src.Services;

public class EmailRetryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailRetryBackgroundService> _logger;

    public EmailRetryBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<EmailRetryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailRetryBackgroundService started.");

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var emailLogRepo = scope.ServiceProvider.GetRequiredService<IEmailLogRepository>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var pendingEmails = (await emailLogRepo.GetPendingRetries(batchSize: 10, stoppingToken)).ToList();

                if (pendingEmails.Count > 0)
                {
                    _logger.LogInformation(
                        "Found {Count} pending emails to retry.",
                        pendingEmails.Count);

                    foreach (var emailLog in pendingEmails)
                    {
                        if (stoppingToken.IsCancellationRequested)
                        {
                            break;
                        }

                        await emailService.RetryEmail(emailLog, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during email retry processing.");
            }
        }

        _logger.LogInformation("EmailRetryBackgroundService stopped.");
    }
}
