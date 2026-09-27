using server.src.Dtos;
using server.src.Models;

namespace server.src.Repositories.Interfaces;

public interface INotificationRepository
{
    Task<(IEnumerable<Notification> Items, int TotalCount)> GetAll(int userId, PaginationQueryDto queryDto, CancellationToken cancellationToken);
    Task<int> GetUnreadCount(int userId, CancellationToken cancellationToken);
    Task<Notification> Create(Notification notification, CancellationToken cancellationToken);
    Task<bool> MarkRead(int id, int userId, CancellationToken cancellationToken);
    Task<int> MarkAllRead(int userId, CancellationToken cancellationToken);
}
