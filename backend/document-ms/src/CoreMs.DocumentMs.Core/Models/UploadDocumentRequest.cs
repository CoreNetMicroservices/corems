using CoreMs.DocumentMs.Core.Enums;

namespace CoreMs.DocumentMs.Core.Models;

public record UploadDocumentRequest(
    string? Name,
    string? Description,
    DocumentVisibility? Visibility,
    List<string>? Tags,
    bool Replace = false
);
