using Minio;
using Minio.DataModel.Args;
using server.src.Config;
using server.src.Services.Interfaces;

namespace server.src.Services;

public class MinioStorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
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

    public async Task UploadFile(string folder, string fileName, Stream stream, string contentType, CancellationToken cancellationToken = default)
    {
        var objectName = BuildObjectName(folder, fileName);

        await EnsureBucketExists(cancellationToken);

        var putObjectArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(contentType);

        await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);
        _logger.LogInformation("Uploaded object {Object} to MinIO bucket {Bucket}", objectName, _bucketName);
    }

    public async Task<(Stream Stream, string ContentType)?> GetFile(string folder, string fileName, CancellationToken cancellationToken = default)
    {
        var objectName = BuildObjectName(folder, fileName);

        await EnsureBucketExists(cancellationToken);

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

    public async Task DeleteFile(string folder, string fileName, CancellationToken cancellationToken = default)
    {
        var objectName = BuildObjectName(folder, fileName);

        await EnsureBucketExists(cancellationToken);

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

    private static string BuildObjectName(string folder, string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return $"{folder.Trim('/')}/{fileName}";
    }
}
