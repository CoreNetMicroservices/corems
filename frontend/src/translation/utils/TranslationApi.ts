import CoreMsApi from "@/common/utils/api/CoreMsApi";
import { TRANSLATION_MS_BASE_URL } from "@/translation/config";
import { HttpMethod } from "@/common/model/CoreMsApiModel";
import { getCurrentUserAuth } from "@/user/store/AuthState";

const translationMsApi = new CoreMsApi({
  baseURL: TRANSLATION_MS_BASE_URL,
});

export interface TranslationMap {
  [key: string]: string;
}

/**
 * Fetch translations for a specific realm and language
 * Only fetches if user is authenticated, otherwise returns empty object
 * @param realm - The realm (e.g., 'app', 'admin', 'public')
 * @param lang - The language code (e.g., 'en', 'es', 'fr')
 * @returns Promise with translations map
 */
export const fetchTranslations = async (
  realm: string,
  lang: string
): Promise<TranslationMap> => {
  const { isAuthenticated } = getCurrentUserAuth();
  
  if (!isAuthenticated) {
    return {};
  }

  const response = await translationMsApi.apiRequest<TranslationMap>(
    HttpMethod.GET,
    `/api/translation/${realm}/${lang}`
  );

  if (response.result && response.response) {
    return response.response;
  }

  return {};
};

/**
 * Fetch available languages from backend
 * Only fetches if user is authenticated, otherwise returns empty array
 */
export const fetchAvailableLanguages = async (
  realm: string
): Promise<string[]> => {
  const { isAuthenticated } = getCurrentUserAuth();
  
  if (!isAuthenticated) {
    return [];
  }

  const response = await translationMsApi.apiRequest<string[]>(
    HttpMethod.GET,
    `/api/languages/${realm}`
  );

  if (response.result && response.response) {
    return response.response;
  }

  return [];
};
