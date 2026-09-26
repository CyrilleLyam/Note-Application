using server.src.Dtos;

namespace server.src.Services.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetById(int id, CancellationToken cancellationToken);
}
