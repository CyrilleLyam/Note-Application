using Mapster;
using server.src.Config;
using server.src.Dtos;
using server.src.Models;

namespace server.src.Mapper;

public class UserMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<RegisterDto, User>()
            .Ignore(dest => dest.Password)
            .Map(dest => dest.Username, src => src.Username.Trim())
            .Map(dest => dest.Email, src => src.Email.Trim().ToLowerInvariant());

        config.NewConfig<User, UserDto>()
            .Map(dest => dest.AvatarUrl, src => BuildAvatarUrl(src.Id, src.AvatarKey));
    }

    public static string? BuildAvatarUrl(int userId, string? avatarKey)
    {
        return avatarKey == null ? null : $"/api/user/{userId}/avatar/{StoragePaths.Split(avatarKey).FileName}";
    }
}
