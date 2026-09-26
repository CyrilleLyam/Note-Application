using server.src.Models;

namespace server.src.Repositories.Interfaces;

public interface ITagRepository
{
    Task<IEnumerable<TagSummary>> GetAll(int userId, CancellationToken cancellationToken);
}
