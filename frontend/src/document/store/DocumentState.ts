import CoreMsApi from "@/common/utils/api/CoreMsApi";
import { DOCUMENT_MS_BASE_URL } from "@/document/config";
import {
  CoreMsApiResonse,
  HttpMethod,
  ApiSuccessfulResponse,
  PageResponse,
} from "@/common/model/CoreMsApiModel";
import { DataTableQueryParams } from "@/common/component/dataTable/DataTableTypes";
import { buildUrlSearchParams } from "@/common/component/dataTable/DataTableState";
import {
  Document,
  UploadBase64Request,
  DocumentUpdateRequest,
  GenerateLinkRequest,
  LinkResponse,
  DocumentLinkInfo,
} from "@/document/model/Document";

const documentMsApi = new CoreMsApi({ baseURL: DOCUMENT_MS_BASE_URL });

type DocumentsPagedResponse = PageResponse<Document>;

export async function listDocuments(
  queryParams: DataTableQueryParams,
  includeDeleted: boolean = false
): Promise<CoreMsApiResonse<DocumentsPagedResponse>> {
  const params = buildUrlSearchParams(queryParams);
  if (includeDeleted) {
    params.append("includeDeleted", "true");
  }
  return await documentMsApi.apiRequest<DocumentsPagedResponse>(
    HttpMethod.GET,
    `/api/documents?${params.toString()}`
  );
}

export async function uploadDocumentMultipart(
  file: File,
  metadata?: {
    ownerUserId?: string;
    visibility?: string;
    description?: string;
    tags?: string[]; // Will be sent as comma-separated string
    confirmReplace?: boolean;
  }
): Promise<CoreMsApiResonse<Document>> {
  const formData = new FormData();
  formData.append("file", file);
  
  if (metadata?.ownerUserId) formData.append("ownerUserId", metadata.ownerUserId);
  if (metadata?.visibility) formData.append("visibility", metadata.visibility);
  if (metadata?.description) formData.append("description", metadata.description);
  if (metadata?.tags && metadata.tags.length > 0) {
    formData.append("tags", metadata.tags.join(","));
  }
  if (metadata?.confirmReplace !== undefined) formData.append("replace", String(metadata.confirmReplace));

  return await documentMsApi.apiRequest<Document>(
    HttpMethod.POST,
    `/api/documents`,
    formData
  );
}

export async function uploadDocumentBase64(
  request: UploadBase64Request
): Promise<CoreMsApiResonse<Document>> {
  return await documentMsApi.apiRequest<Document>(
    HttpMethod.POST,
    `/api/documents/base64`,
    request
  );
}

export async function getDocumentMetadata(
  uuid: string
): Promise<CoreMsApiResonse<Document>> {
  return await documentMsApi.apiRequest<Document>(
    HttpMethod.GET,
    `/api/documents/${uuid}`
  );
}

export async function updateDocument(
  uuid: string,
  request: DocumentUpdateRequest
): Promise<CoreMsApiResonse<Document>> {
  return await documentMsApi.apiRequest<Document>(
    HttpMethod.PUT,
    `/api/documents/${uuid}`,
    request
  );
}

export async function deleteDocument(
  uuid: string,
  permanent: boolean = false
): Promise<CoreMsApiResonse<ApiSuccessfulResponse>> {
  const params = permanent ? "?permanent=true" : "";
  return await documentMsApi.apiRequest<ApiSuccessfulResponse>(
    HttpMethod.DELETE,
    `/api/documents/${uuid}${params}`
  );
}

export async function generateDocumentAccessLink(
  uuid: string,
  request: GenerateLinkRequest
): Promise<CoreMsApiResonse<LinkResponse>> {
  return await documentMsApi.apiRequest<LinkResponse>(
    HttpMethod.POST,
    `/api/documents/${uuid}/link`,
    request
  );
}

export async function listDocumentAccessLinks(
  uuid: string
): Promise<CoreMsApiResonse<DocumentLinkInfo[]>> {
  return await documentMsApi.apiRequest<DocumentLinkInfo[]>(
    HttpMethod.GET,
    `/api/documents/${uuid}/links`
  );
}

export async function revokeDocumentAccessLink(
  uuid: string,
  linkId: number
): Promise<CoreMsApiResonse<ApiSuccessfulResponse>> {
  return await documentMsApi.apiRequest<ApiSuccessfulResponse>(
    HttpMethod.DELETE,
    `/api/documents/${uuid}/links/${linkId}`
  );
}

export function getDocumentDownloadUrl(uuid: string): string {
  const baseUrl = DOCUMENT_MS_BASE_URL;
  return `${baseUrl}/api/documents/${uuid}/download`;
}

export function getPublicDocumentUrl(uuid: string): string {
  const baseUrl = DOCUMENT_MS_BASE_URL;
  return `${baseUrl}/api/public/documents/${uuid}/download`;
}

export function getPublicDocumentViewUrl(uuid: string): string {
  const baseUrl = DOCUMENT_MS_BASE_URL;
  return `${baseUrl}/api/public/documents/${uuid}/view`;
}
