namespace CoreMs.TemplateMs.Core.Models;

public record TemplateDto(
    Guid Id,
    string TemplateId,
    string Language,
    string Name,
    string? Description,
    string Content,
    string Category,
    Dictionary<string, object>? ParamSchema,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Guid? CreatedBy,
    Guid? UpdatedBy);
