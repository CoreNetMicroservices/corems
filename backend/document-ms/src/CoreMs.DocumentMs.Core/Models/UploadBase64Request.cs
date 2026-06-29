using CoreMs.DocumentMs.Core.Enums;

namespace CoreMs.DocumentMs.Core.Models;

public record UploadBase64Request(
    string FileName,
    string ContentBase64,
    string? Name,
    string? Description,
    DocumentVisibility? Visibility,
    List<string>? Tags,
    bool Replace = false
);
