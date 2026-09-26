using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MapsterMapper;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using server.src.Config;
using server.src.Dtos;
using server.src.Events;
using server.src.Events.Interfaces;
using server.src.Models;
using server.src.Repositories.Interfaces;
using server.src.Services.Interfaces;

namespace server.src.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IMapper mapper,
        IEventPublisher eventPublisher,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<AuthResponseDto> Register(RegisterDto registerDto, CancellationToken cancellationToken)
    {
        var user = _mapper.Map<User>(registerDto);

        var existingUserByEmail = await _userRepository.GetByEmail(user.Email, cancellationToken);
        if (existingUserByEmail != null)
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var existingUserByUsername = await _userRepository.GetByUsername(user.Username, cancellationToken);
        if (existingUserByUsername != null)
        {
            throw new InvalidOperationException("Username is already taken.");
        }

        user.Password = _passwordHasher.Hash(registerDto.Password);

        User createdUser;
        try
        {
            createdUser = await _userRepository.Create(user, cancellationToken);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            if (ex.Message.Contains("ix_users_username", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Username is already taken.");
            }

            if (ex.Message.Contains("ix_users_email", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Email is already registered.");
            }

            throw;
        }

        _logger.LogInformation("User registered successfully with ID {UserId}, emitting UserRegisteredEvent", createdUser.Id);
        await _eventPublisher.PublishAsync(
            new UserRegisteredEvent(createdUser.Id, createdUser.Username, createdUser.Email),
            cancellationToken);

        return await GenerateAuthResponse(createdUser, cancellationToken);
    }

    public async Task<AuthResponseDto> Login(LoginDto loginDto, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmail(loginDto.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user == null || !_passwordHasher.Verify(user.Password, loginDto.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        return await GenerateAuthResponse(user, cancellationToken);
    }

    public async Task<AuthResponseDto> RefreshToken(RefreshTokenDto refreshTokenDto, CancellationToken cancellationToken)
    {
        var storedToken = await _refreshTokenRepository.GetByToken(refreshTokenDto.RefreshToken, cancellationToken);
        if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new SecurityTokenException("Invalid or expired refresh token.");
        }

        storedToken.IsRevoked = true;
        await _refreshTokenRepository.Update(storedToken, cancellationToken);

        return await GenerateAuthResponse(storedToken.User, cancellationToken);
    }

    public async Task RevokeToken(RefreshTokenDto refreshTokenDto, CancellationToken cancellationToken)
    {
        var storedToken = await _refreshTokenRepository.GetByToken(refreshTokenDto.RefreshToken, cancellationToken);
        if (storedToken == null || storedToken.IsRevoked)
        {
            return;
        }

        storedToken.IsRevoked = true;
        await _refreshTokenRepository.Update(storedToken, cancellationToken);
    }

    private async Task<AuthResponseDto> GenerateAuthResponse(User user, CancellationToken cancellationToken)
    {
        var minutes = EnvValidator.GetRequiredInt("ACCESS_TOKEN_EXPIRATION_MINUTES");
        var accessExpires = DateTime.UtcNow.AddMinutes(minutes);
        var accessToken = GenerateAccessToken(user, accessExpires);

        var days = EnvValidator.GetRequiredInt("REFRESH_TOKEN_EXPIRATION_DAYS");
        var refreshExpires = DateTime.UtcNow.AddDays(days);
        var refreshToken = new RefreshToken
        {
            Token = GenerateSecureRandomToken(),
            ExpiresAt = refreshExpires,
            IsRevoked = false,
            UserId = user.Id
        };

        await _refreshTokenRepository.Create(refreshToken, cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            AccessTokenExpiresAt = accessExpires,
            RefreshTokenExpiresAt = refreshExpires,
            User = _mapper.Map<UserDto>(user)
        };
    }

    private static string GenerateAccessToken(User user, DateTime expires)
    {
        var secret = EnvValidator.GetRequired("JWT_SECRET");
        var issuer = EnvValidator.GetRequired("JWT_ISSUER");
        var audience = EnvValidator.GetRequired("JWT_AUDIENCE");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateSecureRandomToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
