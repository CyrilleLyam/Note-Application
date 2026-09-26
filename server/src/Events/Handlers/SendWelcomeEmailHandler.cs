using Microsoft.Extensions.Logging;
using server.src.Events.Interfaces;
using server.src.Services.Interfaces;

namespace server.src.Events.Handlers;

public class SendWelcomeEmailHandler : IEventHandler<UserRegisteredEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendWelcomeEmailHandler> _logger;

    public SendWelcomeEmailHandler(IEmailService emailService, ILogger<SendWelcomeEmailHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task HandleAsync(UserRegisteredEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling UserRegisteredEvent: dispatching welcome email for UserId {UserId} ({Email})",
            @event.UserId,
            @event.Email);

        await _emailService.SendWelcomeEmail(@event.Email, @event.Username, cancellationToken);
    }
}
