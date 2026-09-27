namespace server.src.Dtos;

public class SharedNoteResponseDto
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
