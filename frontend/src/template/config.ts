// @ts-expect-error _env_ is injected at runtime for environment config
const env = window._env_ || {};

const CORE_MS_MS_BASE_URL = env.REACT_COREMS_BASE_URL || import.meta.env.REACT_COREMS_BASE_URL;
let templateMsBaseUrl = env.REACT_TEMPLATE_MS_BASE_URL || import.meta.env.REACT_TEMPLATE_MS_BASE_URL;
if (templateMsBaseUrl === undefined || templateMsBaseUrl === "") {
  templateMsBaseUrl = CORE_MS_MS_BASE_URL;
}
export const TEMPLATE_MS_BASE_URL = templateMsBaseUrl;
