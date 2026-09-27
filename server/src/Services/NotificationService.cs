using MapsterMapper;
using server.src.Dtos;
using server.src.Models;
using server.src.Repositories.Interfaces;
using server.src.Services.Interfaces;

namespace server.src.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationStream _notificationStream;
    private readonly IMapper _mapper;

    public NotificationService(INotificationRepository notificationRepository, INotificationStream notificationStream, IMapper mapper)
    {
        _notificationRepository = notificationRepository;
        _notificationStream = notificationStream;
        _mapper = mapper;
    }

    public async Task<BaseResponse<IEnumerable<NotificationResponseDto>>> GetAll(int userId, PaginationQueryDto query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _notificationRepository.GetAll(userId, query, cancellationToken);

        return new BaseResponse<IEnumerable<NotificationResponseDto>>(
            _mapper.Map<IEnumerable<NotificationResponseDto>>(items),
            new PaginationMeta
            {
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            }
        );
    }

    public Task<int> GetUnreadCount(int userId, CancellationToken cancellationToken)
    {
        return _notificationRepository.GetUnreadCount(userId, cancellationToken);
    }

    public Task<bool> MarkRead(int id, int userId, CancellationToken cancellationToken)
    {
        return _notificationRepository.MarkRead(id, userId, cancellationToken);
    }

    public Task<int> MarkAllRead(int userId, CancellationToken cancellationToken)
    {
        return _notificationRepository.MarkAllRead(userId, cancellationToken);
    }

    public async Task NotifyNoteShared(int recipientId, string actorName, int noteId, string noteTitle, string permission, CancellationToken cancellationToken)
    {
        var notification = await _notificationRepository.Create(new Notification
        {
            UserId = recipientId,
            Type = NotificationTypes.NoteShared,
            NoteId = noteId,
            NoteTitle = noteTitle,
            ActorName = actorName,
            Permission = permission
        }, cancellationToken);

        _notificationStream.Publish(recipientId, _mapper.Map<NotificationResponseDto>(notification));
    }
}
