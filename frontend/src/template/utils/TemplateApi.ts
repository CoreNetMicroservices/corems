import CoreMsApi from "@/common/utils/api/CoreMsApi";
import { TEMPLATE_MS_BASE_URL } from "@/template/config";
import { HttpMethod } from "@/common/model/CoreMsApiModel";
import type { Template, TemplateParamDefinition } from "@/template/model/Template";

const templateMsApi = new CoreMsApi({ baseURL: TEMPLATE_MS_BASE_URL });

export interface TemplateMetadata {
  templateId: string;
  language: string;
  name: string;
  description?: string;
  category: string;
  paramSchema?: Record<string, TemplateParamDefinition>;
  requiredParameters: string[];
}

export async function searchTemplates(
  searchTerm: string,
  category?: string
): Promise<Template[]> {
  const params = new URLSearchParams();
  params.append("page", "1");
  params.append("pageSize", "20");

  if (searchTerm) {
    params.append("search", searchTerm);
  }

  if (category) {
    params.append("filter", `category:${category}`);
  }

  const response = await templateMsApi.apiRequest<{ items: Template[] }>(
    HttpMethod.GET,
    `/api/templates?${params.toString()}`
  );

  return response.response?.items || [];
}

export async function getTemplateMetadata(
  templateId: string,
  language: string = "en"
): Promise<TemplateMetadata | null> {
  const response = await templateMsApi.apiRequest<TemplateMetadata>(
    HttpMethod.GET,
    `/api/templates/${templateId}/${language}/metadata`
  );

  return response.response || null;
}
