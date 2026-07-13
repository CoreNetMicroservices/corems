namespace CoreMs.TranslationMs.Core.Models;

public record TranslationRequest
{
    public required Dictionary<string, string> Translations { get; init; }
}
