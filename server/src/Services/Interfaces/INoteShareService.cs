using server.src.Dtos;

namespace server.src.Services.Interfaces;

public interface INoteShareService
{
    Task<ShareLinkDto?> GetShareLink(int noteId, int userId, CancellationToken cancellationToken);
    Task<ShareLinkDto?> CreateShareLink(int noteId, int userId, CancellationToken cancellationToken);
    Task<bool> RevokeShareLink(int noteId, int userId, CancellationToken cancellationToken);
    Task<SharedNoteResponseDto?> GetSharedNote(string token, CancellationToken cancellationToken);
    Task<IEnumerable<NoteShareResponseDto>?> GetShares(int noteId, int ownerId, CancellationToken cancellationToken);
    Task<NoteShareResponseDto?> AddShare(int noteId, int ownerId, AddNoteShareDto addNoteShareDto, CancellationToken cancellationToken);
    Task<NoteShareResponseDto?> UpdateShare(int noteId, int ownerId, int userId, UpdateNoteShareDto updateNoteShareDto, CancellationToken cancellationToken);
    Task<bool> RemoveShare(int noteId, int currentUserId, int userId, CancellationToken cancellationToken);
}
