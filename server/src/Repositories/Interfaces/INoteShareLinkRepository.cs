using server.src.Models;

namespace server.src.Repositories.Interfaces;

public interface INoteShareLinkRepository
{
    Task<string?> GetToken(int noteId, CancellationToken cancellationToken);
    Task<string> GetOrCreateToken(int noteId, string newToken, CancellationToken cancellationToken);
    Task<bool> Delete(int noteId, CancellationToken cancellationToken);
    Task<SharedNote?> GetSharedNote(string token, CancellationToken cancellationToken);
}
