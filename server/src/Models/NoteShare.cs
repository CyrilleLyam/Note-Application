namespace server.src.Models;

public class NoteShare
{
    public int NoteId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Permission { get; set; } = NotePermissions.View;
    public DateTime CreatedAt { get; set; }
}
