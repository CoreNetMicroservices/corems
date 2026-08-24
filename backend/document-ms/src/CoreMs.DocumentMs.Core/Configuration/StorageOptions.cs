using CoreMs.Common.Extensions;

namespace CoreMs.DocumentMs.Core.Configuration;

[Options]
public class StorageOptions
{

    // S3/MinIO settings (local dev)
    public string Endpoint { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Bucket { get; set; } = "documents";
    public bool ForcePathStyle { get; set; } = true;

    // Azure Blob settings (production)
    public string ConnectionString { get; set; } = string.Empty;
    public string Container { get; set; } = "documents";

    /// <summary>
    /// Auto-detected: true when ConnectionString is populated.
    /// </summary>
    public bool UseAzureBlob => !string.IsNullOrEmpty(ConnectionString);
}
