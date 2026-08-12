namespace CoreMs.DocumentMs.Core.Services;

public interface IStorageService
{
    Task UploadAsync(Stream stream, string objectKey, string contentType, long size, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default);
    Task DeleteAsync(string objectKey, CancellationToken ct = default);
    Task<bool> ExistsAsync(string objectKey, CancellationToken ct = default);
    Task EnsureContainerExistsAsync(CancellationToken ct = default);
}
