using System.ComponentModel.DataAnnotations;
using server.src.Models;

namespace server.src.Dtos;

public class AddNoteShareDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email is not a valid email address.")]
    [MaxLength(255, ErrorMessage = "Email must be at most 255 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Permission is required.")]
    [AllowedValues(NotePermissions.View, NotePermissions.Edit, ErrorMessage = "Permission must be 'view' or 'edit'.")]
    public string Permission { get; set; } = NotePermissions.View;
}
