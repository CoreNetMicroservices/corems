namespace CoreMs.UserMs.Core.Models;

/// <summary>
/// User information returned from admin endpoints.
/// </summary>
public record UserInfoDto
{
    public Guid Uuid { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
    public string? ImageUrl { get; init; }
    public string Provider { get; init; } = string.Empty;
    public bool EmailVerified { get; init; }
    public bool? PhoneVerified { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
}
