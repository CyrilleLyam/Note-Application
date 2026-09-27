using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using server.src.Dtos;
using server.src.Extensions;
using server.src.Services.Interfaces;

namespace server.src.Controllers;

[Authorize]
[ApiController]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(25);

    private readonly INotificationService _notificationService;
    private readonly INotificationStream _notificationStream;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public NotificationController(INotificationService notificationService, INotificationStream notificationStream, IOptions<JsonOptions> jsonOptions)
    {
        _notificationService = notificationService;
        _notificationStream = notificationStream;
        _jsonSerializerOptions = jsonOptions.Value.JsonSerializerOptions;
    }

    [HttpGet]
    public async Task<ActionResult<BaseResponse<IEnumerable<NotificationResponseDto>>>> GetAll([FromQuery] PaginationQueryDto query, CancellationToken cancellationToken)
    {
        var notifications = await _notificationService.GetAll(User.GetUserId(), query, cancellationToken);
        return Ok(notifications);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<BaseResponse<UnreadCountDto>>> GetUnreadCount(CancellationToken cancellationToken)
    {
        var count = await _notificationService.GetUnreadCount(User.GetUserId(), cancellationToken);
        return Ok(new BaseResponse<UnreadCountDto>(new UnreadCountDto { Count = count }));
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken cancellationToken)
    {
        var found = await _notificationService.MarkRead(id, User.GetUserId(), cancellationToken);
        if (!found)
        {
            return NotFound(new { error = true, status = StatusCodes.Status404NotFound, message = "Notification not found." });
        }
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await _notificationService.MarkAllRead(User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpGet("stream")]
    public IResult Stream(CancellationToken cancellationToken)
    {
        var notifications = _notificationStream.Subscribe(User.GetUserId(), HeartbeatInterval, cancellationToken);
        return TypedResults.ServerSentEvents(ToEvents(notifications, cancellationToken));
    }

    private async IAsyncEnumerable<SseItem<string>> ToEvents(IAsyncEnumerable<NotificationResponseDto?> notifications, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new SseItem<string>("{}", "ready");

        await foreach (var notification in notifications.WithCancellation(cancellationToken))
        {
            yield return notification == null
                ? new SseItem<string>("{}", "ping")
                : new SseItem<string>(JsonSerializer.Serialize(notification, _jsonSerializerOptions), "notification");
        }
    }
}
