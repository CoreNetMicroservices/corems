using CoreMs.Common.Exceptions;

namespace CoreMs.TranslationMs.Core.Exceptions;

/// <summary>
/// Error definitions specific to the Translation Service.
/// </summary>
public static class TranslationServiceErrors
{
    public static readonly ErrorInfo TranslationNotFound = new("translation.not_found", 404, "Translation bundle not found");
    public static readonly ErrorInfo TranslationAlreadyExists = new("translation.already_exists", 409, "Translation bundle already exists for this realm and language");
}
