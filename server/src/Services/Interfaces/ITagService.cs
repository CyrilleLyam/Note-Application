using server.src.Dtos;

namespace server.src.Services.Interfaces;

public interface ITagService
{
    Task<IEnumerable<TagResponseDto>> GetAll(int userId, CancellationToken cancellationToken);
}
