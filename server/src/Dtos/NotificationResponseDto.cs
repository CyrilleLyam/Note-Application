namespace server.src.Dtos;

public class NotificationResponseDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public int? NoteId { get; set; }
    public string? NoteTitle { get; set; }
    public string? ActorName { get; set; }
    public string? Permission { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
