using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.src.Dtos;
using server.src.Extensions;
using server.src.Services.Interfaces;

namespace server.src.Controllers;

[Authorize]
[ApiController]
[Route("api/notes")]
public class NoteController : ControllerBase
{
    private readonly INoteService _noteService;

    public NoteController(INoteService noteService)
    {
        _noteService = noteService;
    }

    [HttpGet]
    public async Task<ActionResult<BaseResponse<IEnumerable<NoteResponseDto>>>> GetAll([FromQuery] NoteQueryDto query, CancellationToken cancellationToken)
    {
        var notes = await _noteService.GetAll(User.GetUserId(), query, cancellationToken);
        return Ok(notes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BaseResponse<NoteResponseDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        var note = await _noteService.GetById(id, User.GetUserId(), cancellationToken);
        if (note == null)
        {
            return NoteNotFound();
        }
        return Ok(new BaseResponse<NoteResponseDto>(note));
    }

    [HttpPost]
    public async Task<ActionResult<BaseResponse<NoteResponseDto>>> Create([FromBody] CreateNoteDto createNoteDto, CancellationToken cancellationToken)
    {
        var created = await _noteService.Create(User.GetUserId(), createNoteDto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, new BaseResponse<NoteResponseDto>(created));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BaseResponse<NoteResponseDto>>> Update(int id, [FromBody] UpdateNoteDto updateNoteDto, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _noteService.Update(id, User.GetUserId(), updateNoteDto, cancellationToken);
            if (updated == null)
            {
                return NoteNotFound();
            }
            return Ok(new BaseResponse<NoteResponseDto>(updated));
        }
        catch (DBConcurrencyException ex)
        {
            return Conflict(new { error = true, status = StatusCodes.Status409Conflict, message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/pin")]
    public async Task<ActionResult<BaseResponse<NoteResponseDto>>> SetPinned(int id, [FromBody] PinNoteDto pinNoteDto, CancellationToken cancellationToken)
    {
        var note = await _noteService.SetPinned(id, User.GetUserId(), pinNoteDto.IsPinned!.Value, cancellationToken);
        if (note == null)
        {
            return NoteNotFound();
        }
        return Ok(new BaseResponse<NoteResponseDto>(note));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> MoveToTrash(int id, CancellationToken cancellationToken)
    {
        var trashed = await _noteService.MoveToTrash(id, User.GetUserId(), cancellationToken);
        if (!trashed)
        {
            return NoteNotFound();
        }
        return NoContent();
    }

    [HttpPost("{id:int}/restore")]
    public async Task<ActionResult<BaseResponse<NoteResponseDto>>> Restore(int id, CancellationToken cancellationToken)
    {
        var note = await _noteService.Restore(id, User.GetUserId(), cancellationToken);
        if (note == null)
        {
            return NotFound(new { error = true, status = StatusCodes.Status404NotFound, message = "Note not found in trash." });
        }
        return Ok(new BaseResponse<NoteResponseDto>(note));
    }

    [HttpDelete("{id:int}/permanent")]
    public async Task<IActionResult> DeletePermanently(int id, CancellationToken cancellationToken)
    {
        var deleted = await _noteService.DeletePermanently(id, User.GetUserId(), cancellationToken);
        if (!deleted)
        {
            return NotFound(new { error = true, status = StatusCodes.Status404NotFound, message = "Note not found in trash." });
        }
        return NoContent();
    }

    [HttpDelete("trash")]
    public async Task<ActionResult<BaseResponse<EmptyTrashResponseDto>>> EmptyTrash(CancellationToken cancellationToken)
    {
        var deletedCount = await _noteService.EmptyTrash(User.GetUserId(), cancellationToken);
        return Ok(new BaseResponse<EmptyTrashResponseDto>(new EmptyTrashResponseDto { DeletedCount = deletedCount }));
    }

    private NotFoundObjectResult NoteNotFound()
    {
        return NotFound(new { error = true, status = StatusCodes.Status404NotFound, message = "Note not found." });
    }
}
