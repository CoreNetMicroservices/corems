import React, { useEffect, useState } from 'react';
import i18n from 'i18next';
import { I18nextProvider, initReactI18next } from 'react-i18next';
import { I18nContext } from './I18nContext';

export interface I18nProviderProps {
  children: React.ReactNode;
  /** Default language code (default: 'en') */
  defaultLanguage?: string;
  /** Fallback language code (default: 'en') */
  fallbackLanguage?: string;
  /** Optional loading component shown while i18n initializes */
  loadingComponent?: React.ReactNode;
  /** 
   * Optional custom loader to fetch translations dynamically.
   * If staticTranslations is also provided, API translations will be merged on top of defaults.
   */
  loader?: (realm: string, lang: string) => Promise<Record<string, string>>;
  /** Optional loader to fetch available languages list */
  languagesLoader?: (realm: string) => Promise<string[]>;
  /** The realm to use when fetching translations (only used with loader) */
  realm?: string;
  /** 
   * Static/default translations - loaded immediately on init.
   * If loader is also provided, these serve as defaults that API translations merge into.
   */
  staticTranslations?: Record<string, Record<string, string>>;
  /** Static list of available languages (used when no languagesLoader provided) */
  availableLanguages?: string[];
}

/**
 * I18nProvider initializes i18n and provides the I18nextProvider context.
 * 
 * Three modes of operation:
 * 1. Static only: Pass `staticTranslations` for pre-loaded translations (no API)
 * 2. API only: Pass `loader` to fetch all translations from API
 * 3. Hybrid (recommended): Pass both - static defaults load immediately, API merges on top
 * 
 * @example
 * // Hybrid mode - defaults + API merge (recommended)
 * import en from './locales/en.json';
 * import no from './locales/no.json';
 * 
 * <I18nProvider 
 *   realm="myapp"
 *   staticTranslations={{ en, no }}
 *   loader={fetchTranslations}
 *   languagesLoader={fetchAvailableLanguages}
 * >
 *   <App />
 * </I18nProvider>
 */
export const I18nProvider: React.FC<I18nProviderProps> = ({
  children,
  defaultLanguage = 'en',
  fallbackLanguage = 'en',
  loadingComponent = null,
  loader,
  languagesLoader,
  realm = 'common',
  staticTranslations,
  availableLanguages,
}) => {
  const [ready, setReady] = useState(false);
  const [languages, setLanguages] = useState<string[]>(
    availableLanguages || (staticTranslations ? Object.keys(staticTranslations) : [defaultLanguage])
  );
  const [currentLanguage, setCurrentLanguage] = useState(defaultLanguage);

  useEffect(() => {
    let mounted = true;
    const missingKeys: Record<string, string> = {};
    let missingKeysTimer: number | null = null;

    const init = async () => {
      try {
        const storedLanguage = localStorage.getItem('language') || defaultLanguage;
        setCurrentLanguage(storedLanguage);

        // Initialize i18next with static translations (if provided)
        await i18n.use(initReactI18next).init({
          fallbackLng: fallbackLanguage,
          lng: storedLanguage,
          debug: false,
          interpolation: {
            escapeValue: false,
          },
          resources: staticTranslations
            ? Object.entries(staticTranslations).reduce(
              (acc, [lang, translations]) => {
                acc[lang] = { translation: translations };
                return acc;
              },
              {} as Record<string, { translation: Record<string, string> }>
            )
            : undefined,
          react: {
            useSuspense: false,
          },
          // Collect missing translations and print after delay (dev only)
          saveMissing: import.meta.env.DEV,
          missingKeyHandler: import.meta.env.DEV
            ? (_lngs, _ns, key, fallbackValue) => {
                // Store missing key with its fallback value
                if (!missingKeys[key]) {
                  missingKeys[key] = fallbackValue || key;
                }

                // Clear existing timer and set new one
                if (missingKeysTimer) {
                  clearTimeout(missingKeysTimer);
                }

                // Print collected missing keys after 3 seconds of inactivity
                missingKeysTimer = setTimeout(() => {
                  if (Object.keys(missingKeys).length > 0) {
                    console.warn(
                      '[i18n] Missing translation keys:\n',
                      JSON.stringify(missingKeys, null, 2)
                    );
                    // Clear the collected keys after printing
                    Object.keys(missingKeys).forEach(key => delete missingKeys[key]);
                  }
                }, 3000);
              }
            : undefined,
        });

        // If using API loader, load and merge translations
        if (loader) {
          // Load initial language from API and merge with defaults
          try {
            const apiTranslations = await loader(realm, storedLanguage);
            // addResourceBundle with deep=true and overwrite=true merges API on top of defaults
            i18n.addResourceBundle(storedLanguage, 'translation', apiTranslations, true, true);
          } catch (error) {
            console.error(`Failed to load translations for ${storedLanguage}:`, error);
            // Defaults remain in place if API fails
          }

          // Override changeLanguage to load from API and merge
          const originalChangeLanguage = i18n.changeLanguage.bind(i18n);
          i18n.changeLanguage = async (lang: string) => {
            // First, ensure static defaults are loaded for this language
            if (staticTranslations && staticTranslations[lang]) {
              i18n.addResourceBundle(lang, 'translation', staticTranslations[lang], true, false);
            }
            // Then try to load and merge API translations
            try {
              const apiTranslations = await loader(realm, lang);
              i18n.addResourceBundle(lang, 'translation', apiTranslations, true, true);
            } catch (error) {
              console.error(`Failed to load translations for ${lang}:`, error);
              // Defaults remain in place if API fails
            }
            return originalChangeLanguage(lang);
          };
        }

        // Load available languages from API
        if (languagesLoader) {
          try {
            const langs = await languagesLoader(realm);
            if (mounted && langs.length > 0) {
              setLanguages(langs);
            }
          } catch (error) {
            console.error('Failed to load available languages:', error);
            // Keep static availableLanguages or defaults
          }
        }
      } catch (e) {
        console.error('i18n initialization failed', e);
      }
      if (mounted) setReady(true);
    };

    init();

    return () => {
      mounted = false;
      // Clear timer on unmount
      if (missingKeysTimer) {
        clearTimeout(missingKeysTimer);
      }
    };
  }, [realm, defaultLanguage, fallbackLanguage, loader, languagesLoader, staticTranslations, availableLanguages]);

  const changeLanguage = (lang: string) => {
    i18n.changeLanguage(lang);
    localStorage.setItem('language', lang);
    setCurrentLanguage(lang);
  };

  if (!ready) {
    return <>{loadingComponent}</>;
  }

  return (
    <I18nContext.Provider value={{ languages, currentLanguage, changeLanguage }}>
      <I18nextProvider i18n={i18n}>{children}</I18nextProvider>
    </I18nContext.Provider>
  );
};

export default I18nProvider;
