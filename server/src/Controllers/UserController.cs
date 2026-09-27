using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.src.Dtos;
using server.src.Extensions;
using server.src.Services.Interfaces;

namespace server.src.Controllers;

[Authorize]
[ApiController]
[Route("api/user")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<BaseResponse<UserDto>>> GetProfile(CancellationToken cancellationToken)
    {
        var user = await _userService.GetById(User.GetUserId(), cancellationToken);
        if (user == null)
        {
            return NotFound(new { error = true, status = StatusCodes.Status404NotFound, message = "User not found." });
        }
        return Ok(new BaseResponse<UserDto>(user));
    }

    [HttpPut("profile")]
    public async Task<ActionResult<BaseResponse<UserDto>>> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken cancellationToken)
    {
        var user = await _userService.UpdateProfile(User.GetUserId(), dto, cancellationToken);
        return Ok(new BaseResponse<UserDto>(user));
    }

    [HttpPost("profile/avatar")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<BaseResponse<UserDto>>> UploadAvatar(IFormFile file, CancellationToken cancellationToken)
    {
        var user = await _userService.UpdateAvatar(User.GetUserId(), file, cancellationToken);
        return Ok(new BaseResponse<UserDto>(user));
    }

    [HttpDelete("profile/avatar")]
    public async Task<ActionResult<BaseResponse<UserDto>>> DeleteAvatar(CancellationToken cancellationToken)
    {
        var user = await _userService.DeleteAvatar(User.GetUserId(), cancellationToken);
        return Ok(new BaseResponse<UserDto>(user));
    }

    [AllowAnonymous]
    [HttpGet("{userId:int}/avatar/{fileName}")]
    public async Task<IActionResult> GetAvatar(int userId, string fileName, CancellationToken cancellationToken)
    {
        var fileResult = await _userService.GetAvatar(userId, fileName, cancellationToken);
        if (fileResult == null)
        {
            return NotFound(new { error = true, status = StatusCodes.Status404NotFound, message = "Avatar not found." });
        }

        Response.Headers.CacheControl = "public, max-age=86400";
        return File(fileResult.Value.Stream, fileResult.Value.ContentType);
    }
}
