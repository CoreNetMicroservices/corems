using CoreMs.UserMs.Domain.Enums;

namespace CoreMs.UserMs.Domain.Entities;

public class ActionTokenEntity
{
    public long Id { get; set; }
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public string TokenHash { get; set; } = string.Empty;
    public ActionTokenType ActionType { get; set; }
    public long UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool Used { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public UserEntity User { get; set; } = null!;
}
