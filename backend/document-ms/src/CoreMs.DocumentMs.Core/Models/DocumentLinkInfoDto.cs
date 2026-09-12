namespace CoreMs.DocumentMs.Core.Models;

public record DocumentLinkInfoDto(
    long Id,
    string InfoUrl,
    string ViewUrl,
    string DownloadUrl,
    DateTime ExpiresAt,
    bool IsRevoked,
    DateTime? RevokedAt,
    int AccessCount,
    DateTime? LastAccessedAt,
    DateTime CreatedAt
);
