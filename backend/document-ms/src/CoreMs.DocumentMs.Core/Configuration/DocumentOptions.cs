namespace CoreMs.DocumentMs.Core.Configuration;

public class DocumentOptions
{
    public const string SectionName = "Document";

    public long MaxUploadSizeBytes { get; set; } = 10 * 1024 * 1024; // 10 MB
    public string[] AllowedExtensions { get; set; } = ["pdf", "png", "jpg", "jpeg", "gif", "doc", "docx", "xls", "xlsx", "txt", "csv", "zip"];
    public string BaseUrl { get; set; } = "http://localhost:5102";
    public int StreamBufferSize { get; set; } = 81920; // 80 KB
    public string LinkSigningKey { get; set; } = string.Empty;
    public int DefaultLinkExpirationMinutes { get; set; } = 1440; // 24 hours
}
