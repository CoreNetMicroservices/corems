namespace CoreMs.UserMs.Domain.Entities;

public class UserRoleEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public UserEntity User { get; set; } = null!;
}
