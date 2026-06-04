export interface LanguageInfo {
  lang: string;
  updatedAt: string;
  updatedBy: string;
}

export interface RealmLanguages {
  realm: string;
  languages: LanguageInfo[];
}

export interface TranslationAdminView {
  translations: Record<string, string>;
  updatedAt: string;
  updatedBy: string;
}

export interface TranslationUpdateRequest {
  translations: Record<string, string>;
}
