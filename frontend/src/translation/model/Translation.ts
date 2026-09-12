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
  id: number;
  realm: string;
  lang: string;
  data: Record<string, string>;
  updatedAt: string;
  updatedBy: string | null;
}

export interface TranslationUpdateRequest {
  data: Record<string, string>;
}
