namespace CoreMs.TemplateMs.Core.Models;

public record RenderTemplateRequest
{
    public required string TemplateId { get; init; }
    public string Language { get; init; } = "en";
    public Dictionary<string, object> Parameters { get; init; } = new();
}
