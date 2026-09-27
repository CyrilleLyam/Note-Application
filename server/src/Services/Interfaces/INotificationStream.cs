using server.src.Dtos;

namespace server.src.Services.Interfaces;

public interface INotificationStream
{
    IAsyncEnumerable<NotificationResponseDto?> Subscribe(int userId, TimeSpan heartbeatInterval, CancellationToken cancellationToken);
    void Publish(int userId, NotificationResponseDto notification);
}
