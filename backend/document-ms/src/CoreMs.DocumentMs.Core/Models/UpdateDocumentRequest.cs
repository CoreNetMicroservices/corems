using CoreMs.DocumentMs.Core.Enums;

namespace CoreMs.DocumentMs.Core.Models;

public record UpdateDocumentRequest(
    string? Name,
    string? Description,
    DocumentVisibility? Visibility,
    List<string>? Tags
);
