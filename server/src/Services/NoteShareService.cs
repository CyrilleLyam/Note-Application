using System.Buffers.Text;
using System.Security.Cryptography;
using MapsterMapper;
using server.src.Dtos;
using server.src.Repositories.Interfaces;
using server.src.Services.Interfaces;

namespace server.src.Services;

public class NoteShareService : INoteShareService
{
    private const int TokenByteLength = 32;
    private const int MaxTokenLength = 64;

    private readonly INoteRepository _noteRepository;
    private readonly INoteShareLinkRepository _shareLinkRepository;
    private readonly INoteShareRepository _noteShareRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;

    public NoteShareService(
        INoteRepository noteRepository,
        INoteShareLinkRepository shareLinkRepository,
        INoteShareRepository noteShareRepository,
        IUserRepository userRepository,
        INotificationService notificationService,
        IMapper mapper)
    {
        _noteRepository = noteRepository;
        _shareLinkRepository = shareLinkRepository;
        _noteShareRepository = noteShareRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
        _mapper = mapper;
    }

    public async Task<ShareLinkDto?> GetShareLink(int noteId, int userId, CancellationToken cancellationToken)
    {
        if (!await _noteRepository.ExistsActive(noteId, userId, cancellationToken))
        {
            return null;
        }

        var token = await _shareLinkRepository.GetToken(noteId, cancellationToken);
        return new ShareLinkDto { Token = token };
    }

    public async Task<ShareLinkDto?> CreateShareLink(int noteId, int userId, CancellationToken cancellationToken)
    {
        if (!await _noteRepository.ExistsActive(noteId, userId, cancellationToken))
        {
            return null;
        }

        var newToken = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenByteLength));
        var token = await _shareLinkRepository.GetOrCreateToken(noteId, newToken, cancellationToken);
        return new ShareLinkDto { Token = token };
    }

    public async Task<bool> RevokeShareLink(int noteId, int userId, CancellationToken cancellationToken)
    {
        if (!await _noteRepository.ExistsActive(noteId, userId, cancellationToken))
        {
            return false;
        }

        await _shareLinkRepository.Delete(noteId, cancellationToken);
        return true;
    }

    public async Task<SharedNoteResponseDto?> GetSharedNote(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > MaxTokenLength)
        {
            return null;
        }

        var sharedNote = await _shareLinkRepository.GetSharedNote(token, cancellationToken);
        return sharedNote == null ? null : _mapper.Map<SharedNoteResponseDto>(sharedNote);
    }

    public async Task<IEnumerable<NoteShareResponseDto>?> GetShares(int noteId, int ownerId, CancellationToken cancellationToken)
    {
        if (!await _noteRepository.ExistsActive(noteId, ownerId, cancellationToken))
        {
            return null;
        }

        var shares = await _noteShareRepository.GetAll(noteId, cancellationToken);
        return _mapper.Map<IEnumerable<NoteShareResponseDto>>(shares);
    }

    public async Task<NoteShareResponseDto?> AddShare(int noteId, int ownerId, AddNoteShareDto addNoteShareDto, CancellationToken cancellationToken)
    {
        if (!await _noteRepository.ExistsActive(noteId, ownerId, cancellationToken))
        {
            return null;
        }

        var recipient = await _userRepository.GetByEmail(addNoteShareDto.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (recipient == null)
        {
            throw new KeyNotFoundException("No account uses that email.");
        }

        if (recipient.Id == ownerId)
        {
            throw new InvalidOperationException("You can't share a note with yourself.");
        }

        var isNewShare = await _noteShareRepository.GetPermission(noteId, recipient.Id, cancellationToken) == null;
        var share = await _noteShareRepository.Upsert(noteId, recipient.Id, addNoteShareDto.Permission, cancellationToken);

        if (isNewShare)
        {
            await NotifyNoteShared(noteId, ownerId, recipient.Id, share.Permission, cancellationToken);
        }

        return _mapper.Map<NoteShareResponseDto>(share);
    }

    public async Task<NoteShareResponseDto?> UpdateShare(int noteId, int ownerId, int userId, UpdateNoteShareDto updateNoteShareDto, CancellationToken cancellationToken)
    {
        if (!await _noteRepository.ExistsActive(noteId, ownerId, cancellationToken))
        {
            return null;
        }

        var share = await _noteShareRepository.UpdatePermission(noteId, userId, updateNoteShareDto.Permission, cancellationToken);
        return share == null ? null : _mapper.Map<NoteShareResponseDto>(share);
    }

    private async Task NotifyNoteShared(int noteId, int ownerId, int recipientId, string permission, CancellationToken cancellationToken)
    {
        var note = await _noteRepository.GetById(noteId, ownerId, cancellationToken);
        var owner = await _userRepository.GetById(ownerId, cancellationToken);
        if (note == null || owner == null)
        {
            return;
        }

        var actorName = string.IsNullOrWhiteSpace(owner.DisplayName) ? owner.Username : owner.DisplayName;
        await _notificationService.NotifyNoteShared(recipientId, actorName, noteId, note.Title, permission, cancellationToken);
    }

    public async Task<bool> RemoveShare(int noteId, int currentUserId, int userId, CancellationToken cancellationToken)
    {
        var isLeaving = currentUserId == userId;
        if (!isLeaving && !await _noteRepository.ExistsActive(noteId, currentUserId, cancellationToken))
        {
            return false;
        }

        return await _noteShareRepository.Delete(noteId, userId, cancellationToken);
    }
}
