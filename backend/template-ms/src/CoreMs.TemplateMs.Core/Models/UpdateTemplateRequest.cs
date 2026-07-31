namespace CoreMs.TemplateMs.Core.Models;

public record UpdateTemplateRequest
{
    public string? TemplateId { get; init; }
    public string? Language { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Content { get; init; }
    public string? Category { get; init; }
    public Dictionary<string, object>? ParamSchema { get; init; }
}
