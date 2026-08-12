using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CoreMs.DocumentMs.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreMs.DocumentMs.Core.Services;

public class AzureBlobStorageService : IStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(IOptions<StorageOptions> options, ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;
        var opts = options.Value;
        var serviceClient = new BlobServiceClient(opts.ConnectionString);
        _containerClient = serviceClient.GetBlobContainerClient(opts.Container);
    }

    public async Task UploadAsync(Stream stream, string objectKey, string contentType, long size, CancellationToken ct = default)
    {
        var blobClient = _containerClient.GetBlobClient(objectKey);
        var headers = new BlobHttpHeaders { ContentType = contentType };
        await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = headers }, ct);
    }

    public async Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default)
    {
        var blobClient = _containerClient.GetBlobClient(objectKey);
        var response = await blobClient.DownloadStreamingAsync(cancellationToken: ct);
        return response.Value.Content;
    }

    public async Task DeleteAsync(string objectKey, CancellationToken ct = default)
    {
        var blobClient = _containerClient.GetBlobClient(objectKey);
        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
    }

    public async Task<bool> ExistsAsync(string objectKey, CancellationToken ct = default)
    {
        var blobClient = _containerClient.GetBlobClient(objectKey);
        var response = await blobClient.ExistsAsync(ct);
        return response.Value;
    }

    public async Task EnsureContainerExistsAsync(CancellationToken ct = default)
    {
        try
        {
            await _containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
            _logger.LogInformation("Ensured container '{Container}' exists", _containerClient.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure container '{Container}' exists", _containerClient.Name);
            throw;
        }
    }
}
