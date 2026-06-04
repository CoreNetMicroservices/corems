import { hookstate, useHookstate } from "@hookstate/core";
import { devtools } from "@hookstate/devtools";
import CoreMsApi from "@/common/utils/api/CoreMsApi";
import { TRANSLATION_MS_BASE_URL } from "@/translation/config";
import { HttpMethod, PageResponse } from "@/common/model/CoreMsApiModel";
import { RealmLanguages, TranslationAdminView, TranslationUpdateRequest } from "@/translation/model/Translation";
import { DataTableQueryParams } from "@/common/component/dataTable/DataTableTypes";
import { buildUrlSearchParams } from "@/common/component/dataTable/DataTableState";

const translationMsApi = new CoreMsApi({
  baseURL: TRANSLATION_MS_BASE_URL,
});

type RealmsPagedResponse = PageResponse<RealmLanguages>;

interface TranslationState {
  realms: RealmLanguages[];
  pagedResponse?: RealmsPagedResponse;
  selectedTranslation?: TranslationAdminView;
}

const initialState: TranslationState = {
  realms: [],
  pagedResponse: undefined,
  selectedTranslation: undefined,
};

const translationState = hookstate(initialState, devtools({ key: "translationState" }));

export const useTranslationState = () => {
  const state = useHookstate(translationState);

  const fetchRealms = async (queryParams: DataTableQueryParams) => {
    const params = buildUrlSearchParams(queryParams);

    const response = await translationMsApi.apiRequest<RealmsPagedResponse>(
      HttpMethod.GET,
      `/api/admin/translation?${params.toString()}`
    );

    return response;
  };

  const getTranslation = async (realm: string, lang: string) => {
    const response = await translationMsApi.apiRequest<TranslationAdminView>(
      HttpMethod.GET,
      `/api/admin/translation/${realm}/${lang}`
    );

    if (response.result && response.response) {
      state.selectedTranslation.set(response.response);
    }

    return response;
  };

  const updateTranslation = async (realm: string, lang: string, data: TranslationUpdateRequest) => {
    const response = await translationMsApi.apiRequest<void>(
      HttpMethod.PUT,
      `/api/admin/translation/${realm}/${lang}`,
      data
    );

    return response;
  };

  const deleteTranslation = async (realm: string, lang: string) => {
    const response = await translationMsApi.apiRequest<void>(
      HttpMethod.DELETE,
      `/api/admin/translation/${realm}/${lang}`
    );

    return response;
  };

  return {
    state,
    fetchRealms,
    getTranslation,
    updateTranslation,
    deleteTranslation,
  };
};
