import CoreMsApi from "@/common/utils/api/CoreMsApi";
import { DOCUMENT_MS_BASE_URL } from "@/document/config";
import { HttpMethod } from "@/common/model/CoreMsApiModel";
import type { Document } from "@/document/model/Document";

const documentMsApi = new CoreMsApi({ baseURL: DOCUMENT_MS_BASE_URL });

export async function searchDocuments(
  searchTerm: string,
  userId?: string
): Promise<Document[]> {
  const params = new URLSearchParams();
  params.append("page", "1");
  params.append("pageSize", "20");
  
  if (searchTerm) {
    params.append("search", searchTerm);
  }
  
  if (userId) {
    params.append("filter", `userId:${userId}`);
  }

  const response = await documentMsApi.apiRequest<{ items: Document[] }>(
    HttpMethod.GET,
    `/api/documents?${params.toString()}`
  );

  return response.response?.items || [];
}

export async function fetchDocumentsByUuids(uuids: string[]): Promise<Document[]> {
  if (uuids.length === 0) return [];

  const params = new URLSearchParams();
  uuids.forEach((uuid) => params.append("uuids", uuid));

  const response = await documentMsApi.apiRequest<{ items: Document[] }>(
    HttpMethod.GET,
    `/api/documents?${params.toString()}`
  );

  return response.response?.items || [];
}
