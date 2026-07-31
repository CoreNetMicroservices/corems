namespace CoreMs.TemplateMs.Core.Models;

public record TemplateMetadataDto(
    string TemplateId,
    string Language,
    string Name,
    string? Description,
    string Category,
    Dictionary<string, object>? ParamSchema,
    IReadOnlyList<string> RequiredParameters);
