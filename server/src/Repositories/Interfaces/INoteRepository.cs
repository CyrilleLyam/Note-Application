using server.src.Dtos;
using server.src.Models;

namespace server.src.Repositories.Interfaces;

public interface INoteRepository
{
    Task<(IEnumerable<Note> Items, int TotalCount)> GetAll(int userId, NoteQueryDto queryDto, CancellationToken cancellationToken);
    Task<Note?> GetById(int id, int userId, CancellationToken cancellationToken);
    Task<bool> ExistsActive(int id, int userId, CancellationToken cancellationToken);
    Task<Note> Create(Note note, CancellationToken cancellationToken);
    Task<Note?> Update(Note note, CancellationToken cancellationToken);
    Task<bool> UpdateAsEditor(Note note, CancellationToken cancellationToken);
    Task<Note?> SetPinned(int id, int userId, bool isPinned, CancellationToken cancellationToken);
    Task<bool> MoveToTrash(int id, int userId, CancellationToken cancellationToken);
    Task<Note?> Restore(int id, int userId, CancellationToken cancellationToken);
    Task<bool> DeletePermanently(int id, int userId, CancellationToken cancellationToken);
    Task<int> EmptyTrash(int userId, CancellationToken cancellationToken);
}
