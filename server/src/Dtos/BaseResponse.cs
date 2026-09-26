using System.Text.Json.Serialization;

namespace server.src.Dtos;

public class BaseResponse<T>
{
    public bool Error { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PaginationMeta? Meta { get; set; }

    public BaseResponse()
    {
    }

    public BaseResponse(T data, PaginationMeta? meta = null, string? message = null)
    {
        Error = false;
        Data = data;
        Meta = meta;
        Message = message;
    }
}
