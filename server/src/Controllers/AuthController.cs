using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using server.src.Config;
using server.src.Dtos;
using server.src.Extensions;
using server.src.Services.Interfaces;

namespace server.src.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;

    public AuthController(IAuthService authService, IUserService userService)
    {
        _authService = authService;
        _userService = userService;
    }

    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<ActionResult<BaseResponse<AuthResponseDto>>> Register([FromBody] RegisterDto registerDto, CancellationToken cancellationToken)
    {
        var result = await _authService.Register(registerDto, cancellationToken);
        return Ok(new BaseResponse<AuthResponseDto>(result));
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<ActionResult<BaseResponse<AuthResponseDto>>> Login([FromBody] LoginDto loginDto, CancellationToken cancellationToken)
    {
        var result = await _authService.Login(loginDto, cancellationToken);
        return Ok(new BaseResponse<AuthResponseDto>(result));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<BaseResponse<AuthResponseDto>>> Refresh([FromBody] RefreshTokenDto refreshTokenDto, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshToken(refreshTokenDto, cancellationToken);
        return Ok(new BaseResponse<AuthResponseDto>(result));
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RefreshTokenDto refreshTokenDto, CancellationToken cancellationToken)
    {
        await _authService.RevokeToken(refreshTokenDto, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<BaseResponse<UserDto>>> Me(CancellationToken cancellationToken)
    {
        var user = await _userService.GetById(User.GetUserId(), cancellationToken);
        if (user == null)
        {
            return Unauthorized(new { error = true, status = StatusCodes.Status401Unauthorized, message = "User no longer exists." });
        }
        return Ok(new BaseResponse<UserDto>(user));
    }
}
