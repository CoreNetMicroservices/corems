import {
  ACCESS_TOKEN_KEY,
  CoreMsApiConfig,
  CoreMsApiResonse,
  HttpMethod,
} from "@/common/model/CoreMsApiModel";
import axios, { AxiosError } from "axios";

// Request key generator for deduplication
const generateRequestKey = (
  method: HttpMethod,
  url: string,
  data?: unknown,
  headers?: Record<string, string>
): string => {
  const dataString = data ? JSON.stringify(data) : "";
  const headersString = headers ? JSON.stringify(headers) : "";
  return `${method}:${url}:${dataString}:${headersString}`;
};

class CoreMsApi {
  private axios;
  private pendingRequests = new Map<
    string,
    Promise<CoreMsApiResonse<unknown>>
  >();

  public constructor(config: CoreMsApiConfig) {
    this.axios = axios.create({ baseURL: config.baseURL });
  }

  public async apiRequest<T>(
    method: HttpMethod,
    url: string,
    data?: unknown,
    headers?: Record<string, string>
  ): Promise<CoreMsApiResonse<T>> {
    let requestHeaders: Record<string, string> | undefined;
    
    // If data is FormData, don't include any Content-Type header
    if (data instanceof FormData) {
      // Only include Authorization header, let browser set Content-Type
      const accessToken = localStorage.getItem(ACCESS_TOKEN_KEY);
      requestHeaders = accessToken ? { "Authorization": `Bearer ${accessToken}` } : undefined;
      console.log("FormData detected, using minimal headers:", requestHeaders);
    } else {
      requestHeaders = { ...this.getDefaultHeaders(), ...(headers ?? {}) };
      console.log("Regular request headers:", requestHeaders);
    }
    
    const requestKey = generateRequestKey(method, url, data, requestHeaders || {});

    // Check if identical request is already in progress
    if (this.pendingRequests.has(requestKey)) {
      console.warn(`🔄 Deduplicating request: ${method} ${url}`);
      return this.pendingRequests.get(requestKey) as Promise<
        CoreMsApiResonse<T>
      >;
    }

    // Create the request promise
    const requestPromise = this.executeRequest<T>(
      method,
      url,
      data,
      requestHeaders
    );

    // Store the promise in pending requests
    this.pendingRequests.set(requestKey, requestPromise);

    // Remove from pending requests when completed (success or error)
    requestPromise.finally(() => {
      this.pendingRequests.delete(requestKey);
    });

    return requestPromise;
  }

  private async executeRequest<T>(
    method: HttpMethod,
    url: string,
    data?: unknown,
    headers?: Record<string, string>
  ): Promise<CoreMsApiResonse<T>> {
    const response: CoreMsApiResonse<T> = {
      result: false,
      response: null as T,
      errors: [],
    };

    try {
      const res = await this.axios.request<T>({
        method,
        url,
        data,
        headers,
      });

      response.result = true;
      response.response = res.data;
    } catch (error) {
      if (error instanceof AxiosError && error.response) {
        response.errors = error.response.data.errors;
      } else {
        console.error(error);
        response.errors = [
          {
            reasonCode: "unknown.error",
            description: "Unknown error. Please try again later.",
          },
        ];
      }
    }

    console.debug(response);
    return response;
  }

  private getDefaultHeaders(): Record<string, string> {
    const headers: Record<string, string> = {
      "Content-Type": "application/json",
    };

    const accessToken = localStorage.getItem(ACCESS_TOKEN_KEY);
    if (accessToken) {
      headers["Authorization"] = `Bearer ${accessToken}`;
    }

    return headers;
  }

  // Method to clear all pending requests (useful for component unmount or navigation)
  public clearPendingRequests(): void {
    console.log(`🧹 Clearing ${this.pendingRequests.size} pending requests`);
    this.pendingRequests.clear();
  }

  // Method to get pending requests count (useful for debugging)
  public getPendingRequestsCount(): number {
    return this.pendingRequests.size;
  }

  // Method to check if specific request is pending
  public isRequestPending(
    method: HttpMethod,
    url: string,
    data?: unknown,
    headers?: Record<string, string>
  ): boolean {
    const requestHeaders = { ...this.getDefaultHeaders(), ...(headers ?? {}) };
    const requestKey = generateRequestKey(method, url, data, requestHeaders);
    return this.pendingRequests.has(requestKey);
  }
}

export default CoreMsApi;
