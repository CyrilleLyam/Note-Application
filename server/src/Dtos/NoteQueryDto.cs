using Microsoft.AspNetCore.Mvc;

namespace server.src.Dtos;

public class NoteQueryDto : PaginationQueryDto
{
    [FromQuery(Name = "search")]
    public string? Search { get; set; }

    [FromQuery(Name = "created_from")]
    public DateTime? CreatedFrom { get; set; }

    [FromQuery(Name = "created_to")]
    public DateTime? CreatedTo { get; set; }

    [FromQuery(Name = "sort_by")]
    public string? SortBy { get; set; }

    [FromQuery(Name = "sort_order")]
    public string? SortOrder { get; set; }

    [FromQuery(Name = "tag")]
    public string? Tag { get; set; }

    [FromQuery(Name = "trashed")]
    public bool Trashed { get; set; }

    [FromQuery(Name = "shared")]
    public bool Shared { get; set; }
}
