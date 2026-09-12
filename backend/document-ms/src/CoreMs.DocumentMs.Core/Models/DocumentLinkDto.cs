namespace CoreMs.DocumentMs.Core.Models;

public record DocumentLinkDto(
    string Token,
    string InfoUrl,
    string ViewUrl,
    string DownloadUrl,
    DateTime ExpiresAt
);
