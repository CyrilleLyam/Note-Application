using System.ComponentModel.DataAnnotations;
using server.src.Validation;

namespace server.src.Dtos;

public class UpdateNoteDto
{
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(200, ErrorMessage = "Title must be at most 200 characters.")]
    public string Title { get; set; } = string.Empty;

    public string? Content { get; set; }

    [TagList]
    public List<string>? Tags { get; set; }

    [Required(ErrorMessage = "Row version is required.")]
    [Length(8, 8, ErrorMessage = "Row version is invalid.")]
    public byte[]? RowVersion { get; set; }
}
