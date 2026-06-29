namespace CoreMs.DocumentMs.Core.Configuration;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string Endpoint { get; set; } = "http://localhost:9000";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Bucket { get; set; } = "documents";
    public bool ForcePathStyle { get; set; } = true;
}
