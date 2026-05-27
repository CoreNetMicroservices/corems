namespace CoreMs.UserMs.Domain.Entities;

public class AuthorizationCodeEntity
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public long UserId { get; set; }
    public string? Scope { get; set; }
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; }
    public string? Nonce { get; set; }
    public string? State { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public UserEntity User { get; set; } = null!;
}
