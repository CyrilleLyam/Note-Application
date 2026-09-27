using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using server.src.Config;
using server.src.Dtos;
using server.src.Extensions;
using server.src.Services.Interfaces;

namespace server.src.Controllers;

[Authorize]
[ApiController]
[Route("api")]
public class NoteShareController : ControllerBase
{
    private readonly INoteShareService _noteShareService;

    public NoteShareController(INoteShareService noteShareService)
    {
        _noteShareService = noteShareService;
    }

    [HttpGet("notes/{id:int}/share")]
    public async Task<ActionResult<BaseResponse<ShareLinkDto>>> GetShareLink(int id, CancellationToken cancellationToken)
    {
        var shareLink = await _noteShareService.GetShareLink(id, User.GetUserId(), cancellationToken);
        if (shareLink == null)
        {
            return NoteNotFound();
        }
        return Ok(new BaseResponse<ShareLinkDto>(shareLink));
    }

    [HttpPost("notes/{id:int}/share")]
    public async Task<ActionResult<BaseResponse<ShareLinkDto>>> CreateShareLink(int id, CancellationToken cancellationToken)
    {
        var shareLink = await _noteShareService.CreateShareLink(id, User.GetUserId(), cancellationToken);
        if (shareLink == null)
        {
            return NoteNotFound();
        }
        return Ok(new BaseResponse<ShareLinkDto>(shareLink));
    }

    [HttpDelete("notes/{id:int}/share")]
    public async Task<IActionResult> RevokeShareLink(int id, CancellationToken cancellationToken)
    {
        var revoked = await _noteShareService.RevokeShareLink(id, User.GetUserId(), cancellationToken);
        if (!revoked)
        {
            return NoteNotFound();
        }
        return NoContent();
    }

    [HttpGet("notes/{id:int}/shares")]
    public async Task<ActionResult<BaseResponse<IEnumerable<NoteShareResponseDto>>>> GetShares(int id, CancellationToken cancellationToken)
    {
        var shares = await _noteShareService.GetShares(id, User.GetUserId(), cancellationToken);
        if (shares == null)
        {
            return NoteNotFound();
        }
        return Ok(new BaseResponse<IEnumerable<NoteShareResponseDto>>(shares));
    }

    [HttpPost("notes/{id:int}/shares")]
    public async Task<ActionResult<BaseResponse<NoteShareResponseDto>>> AddShare(int id, [FromBody] AddNoteShareDto addNoteShareDto, CancellationToken cancellationToken)
    {
        try
        {
            var share = await _noteShareService.AddShare(id, User.GetUserId(), addNoteShareDto, cancellationToken);
            if (share == null)
            {
                return NoteNotFound();
            }
            return Ok(new BaseResponse<NoteShareResponseDto>(share));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = true, status = StatusCodes.Status404NotFound, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = true, status = StatusCodes.Status400BadRequest, message = ex.Message });
        }
    }

    [HttpPatch("notes/{id:int}/shares/{userId:int}")]
    public async Task<ActionResult<BaseResponse<NoteShareResponseDto>>> UpdateShare(int id, int userId, [FromBody] UpdateNoteShareDto updateNoteShareDto, CancellationToken cancellationToken)
    {
        var share = await _noteShareService.UpdateShare(id, User.GetUserId(), userId, updateNoteShareDto, cancellationToken);
        if (share == null)
        {
            return ShareNotFound();
        }
        return Ok(new BaseResponse<NoteShareResponseDto>(share));
    }

    [HttpDelete("notes/{id:int}/shares/{userId:int}")]
    public async Task<IActionResult> RemoveShare(int id, int userId, CancellationToken cancellationToken)
    {
        var removed = await _noteShareService.RemoveShare(id, User.GetUserId(), userId, cancellationToken);
        if (!removed)
        {
            return ShareNotFound();
        }
        return NoContent();
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Public)]
    [HttpGet("shared/{token}")]
    public async Task<ActionResult<BaseResponse<SharedNoteResponseDto>>> GetSharedNote(string token, CancellationToken cancellationToken)
    {
        var sharedNote = await _noteShareService.GetSharedNote(token, cancellationToken);
        if (sharedNote == null)
        {
            return NotFound(new { error = true, status = StatusCodes.Status404NotFound, message = "This shared note does not exist or is no longer shared." });
        }
        return Ok(new BaseResponse<SharedNoteResponseDto>(sharedNote));
    }

    private NotFoundObjectResult NoteNotFound()
    {
        return NotFound(new { error = true, status = StatusCodes.Status404NotFound, message = "Note not found." });
    }

    private NotFoundObjectResult ShareNotFound()
    {
        return NotFound(new { error = true, status = StatusCodes.Status404NotFound, message = "Share not found." });
    }
}
