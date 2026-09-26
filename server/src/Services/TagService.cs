using MapsterMapper;
using server.src.Dtos;
using server.src.Repositories.Interfaces;
using server.src.Services.Interfaces;

namespace server.src.Services;

public class TagService : ITagService
{
    private readonly ITagRepository _tagRepository;
    private readonly IMapper _mapper;

    public TagService(ITagRepository tagRepository, IMapper mapper)
    {
        _tagRepository = tagRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<TagResponseDto>> GetAll(int userId, CancellationToken cancellationToken)
    {
        var tags = await _tagRepository.GetAll(userId, cancellationToken);
        return _mapper.Map<IEnumerable<TagResponseDto>>(tags);
    }
}
