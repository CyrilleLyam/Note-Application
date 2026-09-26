using server.src.Dtos;

namespace server.src.Services.Interfaces;

public interface INoteService
{
    Task<BaseResponse<IEnumerable<NoteResponseDto>>> GetAll(int userId, NoteQueryDto query, CancellationToken cancellationToken);
    Task<NoteResponseDto?> GetById(int id, int userId, CancellationToken cancellationToken);
    Task<NoteResponseDto> Create(int userId, CreateNoteDto createNoteDto, CancellationToken cancellationToken);
    Task<NoteResponseDto?> Update(int id, int userId, UpdateNoteDto updateNoteDto, CancellationToken cancellationToken);
    Task<NoteResponseDto?> SetPinned(int id, int userId, bool isPinned, CancellationToken cancellationToken);
    Task<bool> MoveToTrash(int id, int userId, CancellationToken cancellationToken);
    Task<NoteResponseDto?> Restore(int id, int userId, CancellationToken cancellationToken);
    Task<bool> DeletePermanently(int id, int userId, CancellationToken cancellationToken);
    Task<int> EmptyTrash(int userId, CancellationToken cancellationToken);
}
