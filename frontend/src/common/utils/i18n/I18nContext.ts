import { createContext, useContext } from "react";

/** Context for sharing i18n state (available languages, etc.) */
export interface I18nContextValue {
  languages: string[];
  currentLanguage: string;
  changeLanguage: (lang: string) => void;
}

export const I18nContext = createContext<I18nContextValue>({
  languages: ["en"],
  currentLanguage: "en",
  changeLanguage: () => {},
});

/** Hook to access i18n context (languages list, change language) */
export const useI18nContext = () => useContext(I18nContext);
