using CoreMs.Common.Exceptions;

namespace CoreMs.DocumentMs.Core.Exceptions;

public static class DocumentServiceErrors
{
    public static readonly ErrorInfo DocumentNotFound = new("document.not_found", 404, "Document not found");
    public static readonly ErrorInfo DocumentAccessDenied = new("document.access_denied", 403, "Access to this document is denied");
    public static readonly ErrorInfo FileTooLarge = new("document.file_too_large", 400, "File exceeds maximum allowed size");
    public static readonly ErrorInfo FileExtensionNotAllowed = new("document.extension_not_allowed", 400, "File extension is not allowed");
    public static readonly ErrorInfo InvalidBase64 = new("document.invalid_base64", 400, "Invalid Base64 content");
    public static readonly ErrorInfo LinkGenerationNotAllowed = new("document.link_not_allowed", 400, "Access links can only be generated for BY_LINK documents");
    public static readonly ErrorInfo TokenInvalid = new("document.token_invalid", 401, "Document access token is invalid");
    public static readonly ErrorInfo TokenExpired = new("document.token_expired", 401, "Document access token has expired");
    public static readonly ErrorInfo TokenRevoked = new("document.token_revoked", 401, "Document access token has been revoked");
    public static readonly ErrorInfo StorageError = new("document.storage_error", 500, "File storage operation failed");
    public static readonly ErrorInfo DocumentAlreadyDeleted = new("document.already_deleted", 400, "Document is already deleted");
}
