using Microsoft.AspNetCore.Http;
using server.src.Dtos;

namespace server.src.Services.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetById(int id, CancellationToken cancellationToken);
    Task<UserDto> UpdateProfile(int id, UpdateProfileDto dto, CancellationToken cancellationToken);
    Task<UserDto> UpdateAvatar(int id, IFormFile file, CancellationToken cancellationToken);
    Task<UserDto> DeleteAvatar(int id, CancellationToken cancellationToken);
    Task<(Stream Stream, string ContentType)?> GetAvatar(int userId, string fileName, CancellationToken cancellationToken);
}
