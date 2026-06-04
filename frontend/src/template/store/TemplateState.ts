import { hookstate, useHookstate } from "@hookstate/core";
import { devtools } from "@hookstate/devtools";
import CoreMsApi from "@/common/utils/api/CoreMsApi";
import { TEMPLATE_MS_BASE_URL } from "@/template/config";
import { HttpMethod, PageResponse } from "@/common/model/CoreMsApiModel";
import { Template, CreateTemplateRequest, UpdateTemplateRequest } from "@/template/model/Template";
import { DataTableQueryParams } from "@/common/component/dataTable/DataTableTypes";
import { buildUrlSearchParams } from "@/common/component/dataTable/DataTableState";

const templateMsApi = new CoreMsApi({
  baseURL: TEMPLATE_MS_BASE_URL,
});

type TemplatePagedResponse = PageResponse<Template>;

interface TemplateState {
  templates: Template[];
  pagedResponse?: TemplatePagedResponse;
  selectedTemplate?: Template;
}

const initialState: TemplateState = {
  templates: [],
  pagedResponse: undefined,
  selectedTemplate: undefined,
};

const templateState = hookstate(initialState, devtools({ key: "templateState" }));

export const useTemplateState = () => {
  const state = useHookstate(templateState);

  const fetchTemplates = async (queryParams: DataTableQueryParams) => {
    const params = buildUrlSearchParams(queryParams);

    const response = await templateMsApi.apiRequest<TemplatePagedResponse>(
      HttpMethod.GET,
      `/api/templates?${params.toString()}`
    );

    return response;
  };

  const getTemplate = async (id: string) => {
    const response = await templateMsApi.apiRequest<Template>(
      HttpMethod.GET,
      `/api/templates/${id}`
    );

    if (response.result && response.response) {
      state.selectedTemplate.set(response.response);
    }

    return response;
  };

  const createTemplate = async (data: CreateTemplateRequest) => {
    const response = await templateMsApi.apiRequest<Template>(
      HttpMethod.POST,
      `/api/templates`,
      data
    );

    return response;
  };

  const updateTemplate = async (id: string, data: UpdateTemplateRequest) => {
    const response = await templateMsApi.apiRequest<Template>(
      HttpMethod.PUT,
      `/api/templates/${id}`,
      data
    );

    return response;
  };

  const deleteTemplate = async (id: string) => {
    const response = await templateMsApi.apiRequest<void>(
      HttpMethod.DELETE,
      `/api/templates/${id}`
    );

    return response;
  };

  return {
    state,
    fetchTemplates,
    getTemplate,
    createTemplate,
    updateTemplate,
    deleteTemplate,
  };
};
