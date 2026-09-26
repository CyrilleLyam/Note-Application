using System.ComponentModel.DataAnnotations;

namespace server.src.Dtos;

public class UpdateProfileDto
{
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
    public string? Username { get; set; }

    [MaxLength(100, ErrorMessage = "Display name must be at most 100 characters.")]
    public string? DisplayName { get; set; }

    [MaxLength(500, ErrorMessage = "Bio must be at most 500 characters.")]
    public string? Bio { get; set; }
}
