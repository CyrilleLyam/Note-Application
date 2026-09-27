using System.ComponentModel.DataAnnotations;
using server.src.Models;

namespace server.src.Dtos;

public class UpdateNoteShareDto
{
    [Required(ErrorMessage = "Permission is required.")]
    [AllowedValues(NotePermissions.View, NotePermissions.Edit, ErrorMessage = "Permission must be 'view' or 'edit'.")]
    public string Permission { get; set; } = NotePermissions.View;
}
