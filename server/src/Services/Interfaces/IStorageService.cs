namespace server.src.Services.Interfaces;

public interface IStorageService
{
    Task<string> UploadFile(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<(Stream Stream, string ContentType)?> GetFile(string fileUrlOrName, CancellationToken cancellationToken = default);
    Task DeleteFile(string fileUrlOrName, CancellationToken cancellationToken = default);
}
