namespace CoreMs.UserMs.Core.Entities;

public class LoginTokenEntity
{
    public long Id { get; set; }
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public UserEntity User { get; set; } = null!;
}
