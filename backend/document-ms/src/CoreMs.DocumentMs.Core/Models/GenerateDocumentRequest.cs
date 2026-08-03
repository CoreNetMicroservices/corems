using CoreMs.DocumentMs.Core.Enums;

namespace CoreMs.DocumentMs.Core.Models;

public record GenerateDocumentRequest
{
    public required string TemplateId { get; init; }
    public string? Language { get; init; }
    public Dictionary<string, object> Parameters { get; init; } = new();
    public string? FileName { get; init; }
    public string? Description { get; init; }
    public DocumentVisibility? Visibility { get; init; }
}
