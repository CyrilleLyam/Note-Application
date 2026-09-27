using server.src.Models;

namespace server.src.Repositories.Interfaces;

public interface INoteShareRepository
{
    Task<string?> GetPermission(int noteId, int userId, CancellationToken cancellationToken);
    Task<IEnumerable<NoteShare>> GetAll(int noteId, CancellationToken cancellationToken);
    Task<NoteShare> Upsert(int noteId, int userId, string permission, CancellationToken cancellationToken);
    Task<NoteShare?> UpdatePermission(int noteId, int userId, string permission, CancellationToken cancellationToken);
    Task<bool> Delete(int noteId, int userId, CancellationToken cancellationToken);
}
