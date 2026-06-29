namespace CoreMs.DocumentMs.Core.Entities;

public class DocumentAccessTokenEntity
{
    public long Id { get; set; }
    public Guid DocumentUuid { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public Guid? RevokedBy { get; set; }
    public DateTime? RevokedAt { get; set; }
    public int AccessCount { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
