const CORE_MS_MS_BASE_URL = import.meta.env.REACT_COREMS_BASE_URL;
let communcationMsBaseUrl = import.meta.env.REACT_COMMUNICATION_MS_BASE_URL;
if (communcationMsBaseUrl === undefined || communcationMsBaseUrl === "") {
  communcationMsBaseUrl = CORE_MS_MS_BASE_URL;
}
export const COMMUNICATION_MS_BASE_URL = communcationMsBaseUrl;
