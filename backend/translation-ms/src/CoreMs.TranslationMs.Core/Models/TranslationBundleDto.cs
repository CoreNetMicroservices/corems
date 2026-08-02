namespace CoreMs.TranslationMs.Core.Models;

public record TranslationBundleDto(
    long Id,
    string Realm,
    string Lang,
    Dictionary<string, string> Data,
    DateTime UpdatedAt,
    Guid? UpdatedBy
);
