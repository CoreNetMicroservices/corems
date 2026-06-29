namespace CoreMs.DocumentMs.Core.Models;

public record GenerateLinkRequest(
    int ExpiresInMinutes = 1440
);
