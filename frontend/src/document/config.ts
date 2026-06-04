const CORE_MS_MS_BASE_URL = import.meta.env.REACT_COREMS_BASE_URL;
let documentMsUrl = import.meta.env.REACT_DOCUMENT_MS_BASE_URL;
if (documentMsUrl === undefined || documentMsUrl === "") {
  documentMsUrl = CORE_MS_MS_BASE_URL;
}

export const DOCUMENT_MS_BASE_URL = documentMsUrl;
