namespace CoreMs.TranslationMs.Core.Models;

public record RealmLanguagesDto(
    string Realm,
    List<LanguageInfoDto> Languages
);

public record LanguageInfoDto(
    string Lang,
    DateTime UpdatedAt,
    Guid? UpdatedBy
);
