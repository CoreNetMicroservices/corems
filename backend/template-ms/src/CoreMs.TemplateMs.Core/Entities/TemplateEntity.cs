namespace CoreMs.TemplateMs.Core.Entities;

public class TemplateEntity
{
    public long Id { get; set; }
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public string TemplateId { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Dictionary<string, object>? ParamSchema { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}
