using System.Data;
using MapsterMapper;
using server.src.Dtos;
using server.src.Exceptions;
using server.src.Models;
using server.src.Repositories.Interfaces;
using server.src.Services.Interfaces;

namespace server.src.Services;

public class NoteService : INoteService
{
    private readonly INoteRepository _noteRepository;
    private readonly INoteShareRepository _noteShareRepository;
    private readonly IMapper _mapper;

    public NoteService(INoteRepository noteRepository, INoteShareRepository noteShareRepository, IMapper mapper)
    {
        _noteRepository = noteRepository;
        _noteShareRepository = noteShareRepository;
        _mapper = mapper;
    }

    public async Task<BaseResponse<IEnumerable<NoteResponseDto>>> GetAll(int userId, NoteQueryDto query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _noteRepository.GetAll(userId, query, cancellationToken);

        return new BaseResponse<IEnumerable<NoteResponseDto>>(
            _mapper.Map<IEnumerable<NoteResponseDto>>(items),
            new PaginationMeta
            {
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            }
        );
    }

    public async Task<NoteResponseDto?> GetById(int id, int userId, CancellationToken cancellationToken)
    {
        var note = await _noteRepository.GetById(id, userId, cancellationToken);
        return note == null ? null : _mapper.Map<NoteResponseDto>(note);
    }

    public async Task<NoteResponseDto> Create(int userId, CreateNoteDto createNoteDto, CancellationToken cancellationToken)
    {
        var note = _mapper.Map<Note>(createNoteDto);
        note.UserId = userId;

        var created = await _noteRepository.Create(note, cancellationToken);
        return _mapper.Map<NoteResponseDto>(created);
    }

    public async Task<NoteResponseDto?> Update(int id, int userId, UpdateNoteDto updateNoteDto, CancellationToken cancellationToken)
    {
        var permission = await _noteShareRepository.GetPermission(id, userId, cancellationToken);
        if (permission == null)
        {
            return null;
        }

        if (permission == NotePermissions.View)
        {
            throw new ForbiddenException("You can only view this note.");
        }

        var note = _mapper.Map<Note>(updateNoteDto);
        note.Id = id;
        note.UserId = userId;

        if (permission == NotePermissions.Owner)
        {
            var updated = await _noteRepository.Update(note, cancellationToken);
            if (updated != null)
            {
                return _mapper.Map<NoteResponseDto>(updated);
            }
        }
        else if (await _noteRepository.UpdateAsEditor(note, cancellationToken))
        {
            var updated = await _noteRepository.GetById(id, userId, cancellationToken);
            return updated == null ? null : _mapper.Map<NoteResponseDto>(updated);
        }

        if (await _noteShareRepository.GetPermission(id, userId, cancellationToken) != null)
        {
            throw new DBConcurrencyException("This note was changed somewhere else after you opened it.");
        }

        return null;
    }

    public async Task<NoteResponseDto?> SetPinned(int id, int userId, bool isPinned, CancellationToken cancellationToken)
    {
        var note = await _noteRepository.SetPinned(id, userId, isPinned, cancellationToken);
        return note == null ? null : _mapper.Map<NoteResponseDto>(note);
    }

    public async Task<bool> MoveToTrash(int id, int userId, CancellationToken cancellationToken)
    {
        return await _noteRepository.MoveToTrash(id, userId, cancellationToken);
    }

    public async Task<NoteResponseDto?> Restore(int id, int userId, CancellationToken cancellationToken)
    {
        var note = await _noteRepository.Restore(id, userId, cancellationToken);
        return note == null ? null : _mapper.Map<NoteResponseDto>(note);
    }

    public async Task<bool> DeletePermanently(int id, int userId, CancellationToken cancellationToken)
    {
        return await _noteRepository.DeletePermanently(id, userId, cancellationToken);
    }

    public async Task<int> EmptyTrash(int userId, CancellationToken cancellationToken)
    {
        return await _noteRepository.EmptyTrash(userId, cancellationToken);
    }
}
