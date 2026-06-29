using CoreMs.DocumentMs.Core.Enums;

namespace CoreMs.DocumentMs.Core.Entities;

public class DocumentEntity
{
    public long Id { get; set; }
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OriginalFilename { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Extension { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public string ObjectKey { get; set; } = string.Empty;
    public DocumentVisibility Visibility { get; set; } = DocumentVisibility.Private;
    public Guid? UploadedById { get; set; }
    public UploadedByType UploadedByType { get; set; } = UploadedByType.User;
    public string? Checksum { get; set; }
    public string? Description { get; set; }
    public int Version { get; set; } = 1;

    // Soft delete
    public bool IsDeleted { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Tags stored as JSON column
    public List<string> Tags { get; set; } = [];
}
