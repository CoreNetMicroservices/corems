namespace CoreMs.UserMs.Core.Models;

/// <summary>
/// User information extracted from an external OAuth2 provider after authentication.
/// </summary>
public record ExternalLoginInfo(
    string Email,
    string? FirstName,
    string? LastName,
    string? ImageUrl,
    string ProviderId);
