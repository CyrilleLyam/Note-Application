using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using server.src.Events.Interfaces;
using server.src.Services.Interfaces;

namespace server.src.Events.Handlers;

public class SendWelcomeEmailHandler : IEventHandler<UserRegisteredEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SendWelcomeEmailHandler> _logger;

    public SendWelcomeEmailHandler(IServiceScopeFactory scopeFactory, ILogger<SendWelcomeEmailHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task HandleAsync(UserRegisteredEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling UserRegisteredEvent: dispatching welcome email for UserId {UserId} ({Email}) to background task.",
            @event.UserId,
            @event.Email);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                await emailService.SendWelcomeEmail(@event.Email, @event.Username, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unhandled exception while sending welcome email to {Email} in background.",
                    @event.Email);
            }
        });

        return Task.CompletedTask;
    }
}
