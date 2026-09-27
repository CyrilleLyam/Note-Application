using server.src.Dtos;

namespace server.src.Services.Interfaces;

public interface INotificationService
{
    Task<BaseResponse<IEnumerable<NotificationResponseDto>>> GetAll(int userId, PaginationQueryDto query, CancellationToken cancellationToken);
    Task<int> GetUnreadCount(int userId, CancellationToken cancellationToken);
    Task<bool> MarkRead(int id, int userId, CancellationToken cancellationToken);
    Task<int> MarkAllRead(int userId, CancellationToken cancellationToken);
    Task NotifyNoteShared(int recipientId, string actorName, int noteId, string noteTitle, string permission, CancellationToken cancellationToken);
}
