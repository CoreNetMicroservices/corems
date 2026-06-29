namespace CoreMs.DocumentMs.Core.Models;

public record DocumentLinkDto(
    string Token,
    string Url,
    DateTime ExpiresAt
);
