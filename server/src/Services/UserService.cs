using MapsterMapper;
using Microsoft.AspNetCore.Http;
using server.src.Dtos;
using server.src.Repositories.Interfaces;
using server.src.Services.Interfaces;
using server.src.Validation;

namespace server.src.Services;

public class UserService : IUserService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private const long MaxAvatarSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly IUserRepository _userRepository;
    private readonly IStorageService _storageService;
    private readonly IMapper _mapper;

    public UserService(IUserRepository userRepository, IStorageService storageService, IMapper mapper)
    {
        _userRepository = userRepository;
        _storageService = storageService;
        _mapper = mapper;
    }

    public async Task<UserDto?> GetById(int id, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetById(id, cancellationToken);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> UpdateProfile(int id, UpdateProfileDto dto, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetById(id, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Username) && !string.Equals(dto.Username.Trim(), user.Username, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _userRepository.GetByUsername(dto.Username.Trim(), cancellationToken);
            if (existing != null && existing.Id != id)
            {
                throw new InvalidOperationException("Username is already taken.");
            }
            user.Username = dto.Username.Trim();
        }

        if (dto.DisplayName != null)
        {
            user.DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? null : dto.DisplayName.Trim();
        }

        if (dto.Bio != null)
        {
            user.Bio = string.IsNullOrWhiteSpace(dto.Bio) ? null : dto.Bio.Trim();
        }

        var updatedUser = await _userRepository.Update(user, cancellationToken);
        return _mapper.Map<UserDto>(updatedUser);
    }

    public async Task<UserDto> UpdateAvatar(int id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            throw new InvalidOperationException("No file was uploaded.");
        }

        if (file.Length > MaxAvatarSizeBytes)
        {
            throw new InvalidOperationException("Avatar file size must not exceed 5 MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains(file.ContentType))
        {
            throw new InvalidOperationException("Invalid image format. Allowed formats: JPG, PNG, WEBP, GIF.");
        }

        await using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        var header = new byte[32];
        var bytesRead = await memoryStream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);

        if (ImageValidator.IsExecutableOrScript(header.AsSpan(0, bytesRead)))
        {
            throw new InvalidOperationException("The uploaded file is an executable or script disguised as an image. Upload rejected.");
        }

        if (!ImageValidator.TryDetectImageFormat(header.AsSpan(0, bytesRead), out var detectedFormat, out var detectedContentType, out var canonicalExtension))
        {
            throw new InvalidOperationException("The file content is not a valid image. Only genuine JPG, PNG, WEBP, or GIF files are allowed.");
        }

        memoryStream.Position = 0;

        var user = await _userRepository.GetById(id, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
        {
            await _storageService.DeleteFile(user.AvatarUrl, cancellationToken);
        }

        var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}{canonicalExtension}";
        var avatarUrl = await _storageService.UploadFile(memoryStream, fileName, detectedContentType, cancellationToken);

        user.AvatarUrl = avatarUrl;
        var updatedUser = await _userRepository.Update(user, cancellationToken);
        return _mapper.Map<UserDto>(updatedUser);
    }

    public async Task<UserDto> DeleteAvatar(int id, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetById(id, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
        {
            await _storageService.DeleteFile(user.AvatarUrl, cancellationToken);
            user.AvatarUrl = null;
            user = await _userRepository.Update(user, cancellationToken);
        }

        return _mapper.Map<UserDto>(user);
    }

    public Task<(Stream Stream, string ContentType)?> GetAvatar(string fileName, CancellationToken cancellationToken)
    {
        return _storageService.GetFile(fileName, cancellationToken);
    }
}
