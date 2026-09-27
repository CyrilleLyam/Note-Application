namespace server.src.Services.Interfaces;

public interface IStorageService
{
    Task UploadFile(string folder, string fileName, Stream stream, string contentType, CancellationToken cancellationToken = default);
    Task<(Stream Stream, string ContentType)?> GetFile(string folder, string fileName, CancellationToken cancellationToken = default);
    Task DeleteFile(string folder, string fileName, CancellationToken cancellationToken = default);
}
