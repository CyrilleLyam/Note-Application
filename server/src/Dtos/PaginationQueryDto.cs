using Microsoft.AspNetCore.Mvc;

namespace server.src.Dtos;

public class PaginationQueryDto
{
    private const int MaxPageSize = 100;
    private int _page = 1;
    private int _pageSize = 10;

    [FromQuery(Name = "page")]
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    [FromQuery(Name = "page_size")]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 10 : value);
    }
}
