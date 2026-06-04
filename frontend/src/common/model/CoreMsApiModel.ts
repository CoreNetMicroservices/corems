export interface ApiSuccessfulResponse {
  result: boolean;
}

export interface CoreMsApiError {
  reasonCode: string;
  description: string;
  details?: string;
}

export interface CoreMsApiConfig {
  baseURL: string;
  useAccessToken?: boolean;
}

export interface CoreMsApiResonse<T> {
  result: boolean;
  response: T;
  errors: CoreMsApiError[];
}

export enum HttpMethod {
  GET = "GET",
  POST = "POST",
  PUT = "PUT",
  PATCH = "PATCH",
  DELETE = "DELETE",
}

export const REFRESH_TOKEN_KEY = "refreshToken";
export const ACCESS_TOKEN_KEY = "accessToken";

/// Pagination response interface
export interface PageResponse<T> {
  page: number;
  pageSize: number;
  totalElements: number;
  totalPages: number;
  items: T[];
}
