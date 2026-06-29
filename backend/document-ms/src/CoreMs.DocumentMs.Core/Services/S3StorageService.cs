using Amazon.S3;
using Amazon.S3.Model;
using CoreMs.Common.Extensions;
using CoreMs.DocumentMs.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreMs.DocumentMs.Core.Services;

[Service]
public class S3StorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly StorageOptions _options;
    private readonly ILogger<S3StorageService> _logger;

    public S3StorageService(IOptions<StorageOptions> options, ILogger<S3StorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var config = new AmazonS3Config
        {
            ServiceURL = _options.Endpoint,
            ForcePathStyle = _options.ForcePathStyle
        };

        _s3Client = new AmazonS3Client(_options.AccessKey, _options.SecretKey, config);
    }

    internal S3StorageService(IAmazonS3 s3Client, IOptions<StorageOptions> options, ILogger<S3StorageService> logger)
    {
        _s3Client = s3Client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task UploadObjectAsync(Stream stream, string objectKey, string contentType, long size, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = objectKey,
            InputStream = stream,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(request, ct);
    }

    public async Task<Stream> GetObjectStreamAsync(string objectKey, CancellationToken ct = default)
    {
        var request = new GetObjectRequest
        {
            BucketName = _options.Bucket,
            Key = objectKey
        };

        var response = await _s3Client.GetObjectAsync(request, ct);
        return response.ResponseStream;
    }

    public async Task DeleteObjectAsync(string objectKey, CancellationToken ct = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _options.Bucket,
            Key = objectKey
        };

        await _s3Client.DeleteObjectAsync(request, ct);
    }

    public async Task<bool> ObjectExistsAsync(string objectKey, CancellationToken ct = default)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _options.Bucket,
                Key = objectKey
            };

            await _s3Client.GetObjectMetadataAsync(request, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task EnsureBucketExistsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _s3Client.ListBucketsAsync(ct);
            var bucketExists = response.Buckets.Any(b => b.BucketName == _options.Bucket);

            if (!bucketExists)
            {
                await _s3Client.PutBucketAsync(new PutBucketRequest { BucketName = _options.Bucket }, ct);
                _logger.LogInformation("Created bucket '{Bucket}'", _options.Bucket);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure bucket '{Bucket}' exists", _options.Bucket);
            throw;
        }
    }
}
