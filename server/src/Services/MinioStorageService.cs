using Minio;
using Minio.DataModel.Args;
using server.src.Config;
using server.src.Services.Interfaces;

namespace server.src.Services;

public class MinioStorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
    private readonly string _publicUrl;
    private readonly ILogger<MinioStorageService> _logger;
    private bool _bucketInitialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public MinioStorageService(ILogger<MinioStorageService> logger)
    {
        _logger = logger;

        var endpoint = EnvValidator.GetRequired("MINIO_ENDPOINT");
        var accessKey = EnvValidator.GetRequired("MINIO_ACCESS_KEY");
        var secretKey = EnvValidator.GetRequired("MINIO_SECRET_KEY");
        _bucketName = EnvValidator.GetRequired("MINIO_BUCKET_NAME");
        var useSsl = EnvValidator.GetOptionalBool("MINIO_USE_SSL", false);
        _publicUrl = EnvValidator.GetOptional("MINIO_PUBLIC_URL") ?? (useSsl ? $"https://{endpoint}" : $"http://{endpoint}");

        var clientBuilder = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey);

        if (useSsl)
        {
            clientBuilder = clientBuilder.WithSSL();
        }

        _minioClient = clientBuilder.Build();
    }

    private async Task EnsureBucketExists(CancellationToken cancellationToken)
    {
        if (_bucketInitialized)
        {
            return;
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_bucketInitialized)
            {
                return;
            }

            var beArgs = new BucketExistsArgs().WithBucket(_bucketName);
            bool found = await _minioClient.BucketExistsAsync(beArgs, cancellationToken);
            if (!found)
            {
                var mbArgs = new MakeBucketArgs().WithBucket(_bucketName);
                await _minioClient.MakeBucketAsync(mbArgs, cancellationToken);
                _logger.LogInformation("MinIO bucket {Bucket} created.", _bucketName);
            }

            _bucketInitialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<string> UploadFile(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExists(cancellationToken);

        var extension = Path.GetExtension(fileName);
        var objectName = $"avatars/{Guid.NewGuid():N}{extension}";

        var putObjectArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(contentType);

        await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);
        _logger.LogInformation("File {FileName} uploaded to MinIO bucket {Bucket} as {Object}", fileName, _bucketName, objectName);

        var storedFileName = Path.GetFileName(objectName);
        return $"/api/user/avatar/{storedFileName}";
    }

    public async Task<(Stream Stream, string ContentType)?> GetFile(string fileUrlOrName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrlOrName))
        {
            return null;
        }

        await EnsureBucketExists(cancellationToken);

        var objectName = ExtractObjectName(fileUrlOrName);
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        try
        {
            var statArgs = new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName);
            var stat = await _minioClient.StatObjectAsync(statArgs, cancellationToken);

            var memoryStream = new MemoryStream();
            var getArgs = new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithCallbackStream(s => s.CopyTo(memoryStream));

            await _minioClient.GetObjectAsync(getArgs, cancellationToken);
            memoryStream.Position = 0;

            var contentType = !string.IsNullOrWhiteSpace(stat.ContentType) ? stat.ContentType : "application/octet-stream";
            return (memoryStream, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve object {Object} from MinIO bucket {Bucket}", objectName, _bucketName);
            return null;
        }
    }

    public async Task DeleteFile(string fileUrlOrName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrlOrName))
        {
            return;
        }

        await EnsureBucketExists(cancellationToken);

        var objectName = ExtractObjectName(fileUrlOrName);
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return;
        }

        try
        {
            var rmArgs = new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName);

            await _minioClient.RemoveObjectAsync(rmArgs, cancellationToken);
            _logger.LogInformation("Deleted object {Object} from MinIO bucket {Bucket}", objectName, _bucketName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete object {Object} from MinIO bucket {Bucket}", objectName, _bucketName);
        }
    }

    private string ExtractObjectName(string fileUrlOrName)
    {
        if (string.IsNullOrWhiteSpace(fileUrlOrName))
        {
            return string.Empty;
        }

        const string apiPrefix = "/api/user/avatar/";
        if (fileUrlOrName.StartsWith(apiPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return $"avatars/{fileUrlOrName[apiPrefix.Length..]}";
        }

        var prefix = $"{_publicUrl.TrimEnd('/')}/{_bucketName}/";
        if (fileUrlOrName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return fileUrlOrName[prefix.Length..];
        }

        var relativePrefix = $"/{_bucketName}/";
        if (fileUrlOrName.StartsWith(relativePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return fileUrlOrName[relativePrefix.Length..];
        }

        if (!fileUrlOrName.Contains('/') && !fileUrlOrName.Contains('\\'))
        {
            return $"avatars/{fileUrlOrName}";
        }

        return fileUrlOrName;
    }
}
