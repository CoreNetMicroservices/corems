// @ts-expect-error _env_ is injected at runtime for environment config
const env = window._env_ || {};

const CORE_MS_MS_BASE_URL = env.REACT_COREMS_BASE_URL || import.meta.env.REACT_COREMS_BASE_URL;
let translationMsBaseUrl = env.REACT_TRANSLATION_MS_BASE_URL || import.meta.env.REACT_TRANSLATION_MS_BASE_URL;
if (translationMsBaseUrl === undefined || translationMsBaseUrl === "") {
  translationMsBaseUrl = CORE_MS_MS_BASE_URL;
}
export const TRANSLATION_MS_BASE_URL = translationMsBaseUrl;
