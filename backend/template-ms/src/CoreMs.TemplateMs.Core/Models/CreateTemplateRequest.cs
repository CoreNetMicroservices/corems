namespace CoreMs.TemplateMs.Core.Models;

public record CreateTemplateRequest
{
    public required string TemplateId { get; init; }
    public string Language { get; init; } = "en";
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Content { get; init; }
    public required string Category { get; init; }
    public Dictionary<string, object>? ParamSchema { get; init; }
}
