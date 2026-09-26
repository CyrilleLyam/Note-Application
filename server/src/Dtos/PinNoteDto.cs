using System.ComponentModel.DataAnnotations;

namespace server.src.Dtos;

public class PinNoteDto
{
    [Required(ErrorMessage = "Pinned state is required.")]
    public bool? IsPinned { get; set; }
}
