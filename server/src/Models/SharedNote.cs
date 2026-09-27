namespace server.src.Models;

public class SharedNote
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
