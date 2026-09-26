using server.src.Dtos;

namespace server.src.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> Register(RegisterDto registerDto, CancellationToken cancellationToken);
    Task<AuthResponseDto> Login(LoginDto loginDto, CancellationToken cancellationToken);
    Task<AuthResponseDto> RefreshToken(RefreshTokenDto refreshTokenDto, CancellationToken cancellationToken);
    Task RevokeToken(RefreshTokenDto refreshTokenDto, CancellationToken cancellationToken);
}
