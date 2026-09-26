using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.src.Dtos;
using server.src.Extensions;
using server.src.Services.Interfaces;

namespace server.src.Controllers;

[Authorize]
[ApiController]
[Route("api/tags")]
public class TagController : ControllerBase
{
    private readonly ITagService _tagService;

    public TagController(ITagService tagService)
    {
        _tagService = tagService;
    }

    [HttpGet]
    public async Task<ActionResult<BaseResponse<IEnumerable<TagResponseDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var tags = await _tagService.GetAll(User.GetUserId(), cancellationToken);
        return Ok(new BaseResponse<IEnumerable<TagResponseDto>>(tags));
    }
}
