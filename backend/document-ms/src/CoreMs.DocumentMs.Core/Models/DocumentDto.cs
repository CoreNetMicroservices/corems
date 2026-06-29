using CoreMs.DocumentMs.Core.Enums;

namespace CoreMs.DocumentMs.Core.Models;

public record DocumentDto(
    Guid Uuid,
    Guid UserId,
    string Name,
    string OriginalFilename,
    long Size,
    string Extension,
    string ContentType,
    DocumentVisibility Visibility,
    Guid? UploadedById,
    UploadedByType UploadedByType,
    string? Checksum,
    string? Description,
    List<string> Tags,
    int Version,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
