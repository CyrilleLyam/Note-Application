using MapsterMapper;
using server.src.Dtos;
using server.src.Repositories.Interfaces;
using server.src.Services.Interfaces;

namespace server.src.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public UserService(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<UserDto?> GetById(int id, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetById(id, cancellationToken);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }
}
