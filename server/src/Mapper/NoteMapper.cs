using Mapster;
using server.src.Dtos;
using server.src.Models;
using server.src.Validation;

namespace server.src.Mapper;

public class NoteMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateNoteDto, Note>()
            .Map(dest => dest.Title, src => src.Title.Trim())
            .Map(dest => dest.Content, src => string.IsNullOrWhiteSpace(src.Content) ? null : src.Content)
            .Map(dest => dest.Tags, src => TagListAttribute.Normalize(src.Tags));

        config.NewConfig<UpdateNoteDto, Note>()
            .Map(dest => dest.Title, src => src.Title.Trim())
            .Map(dest => dest.Content, src => string.IsNullOrWhiteSpace(src.Content) ? null : src.Content)
            .Map(dest => dest.Tags, src => TagListAttribute.Normalize(src.Tags));

        config.NewConfig<Note, NoteResponseDto>();

        config.NewConfig<TagSummary, TagResponseDto>();
    }
}
