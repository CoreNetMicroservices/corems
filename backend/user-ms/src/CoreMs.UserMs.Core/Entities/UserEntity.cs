namespace CoreMs.UserMs.Core.Entities;

public class UserEntity
{
    public long Id { get; set; }
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public string Provider { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ImageUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Password { get; set; }
    public bool EmailVerified { get; set; }
    public bool? PhoneVerified { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public ICollection<UserRoleEntity> Roles { get; set; } = new List<UserRoleEntity>();
    public ICollection<LoginTokenEntity> Tokens { get; set; } = new List<LoginTokenEntity>();
    public ICollection<ActionTokenEntity> ActionTokens { get; set; } = new List<ActionTokenEntity>();
}
