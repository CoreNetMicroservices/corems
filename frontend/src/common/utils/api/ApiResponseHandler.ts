import { useState } from "react";
import { CoreMsApiResonse } from "@/common/model/CoreMsApiModel";

export interface MessageState {
  success: string | null;
  initialErrorMessage: string | null;
  errors: string[] | null;
}

export interface UseMessageStateReturn {
  success: string | null;
  initialErrorMessage: string | null;
  errors: string[] | null;
  clearSuccess: () => void;
  clearError: () => void;
  clearAll: () => void;
  handleResponse: <T>(
    result: CoreMsApiResonse<T>,
    errorContext: string,
    successMessage?: string
  ) => void;
}

export class ApiResponseHandler {
  static handleApiResponse<T>(
    result: CoreMsApiResonse<T>,
    errorContext: string,
    successMessage?: string
  ): MessageState {
    if (result.result) {
      return {
        success: successMessage || null,
        initialErrorMessage: null,
        errors: null,
      };
    } else {
      const errorDetails =
        result.errors?.map((err) =>
          err.details ? `${err.description}: ${err.details}` : err.description
        ) || [];

      return {
        success: null,
        initialErrorMessage: errorContext,
        errors: errorDetails.length > 0 ? errorDetails : null,
      };
    }
  }
}

export function useMessageState(): UseMessageStateReturn {
  const [success, setSuccess] = useState<string | null>(null);
  const [initialErrorMessage, setInitialErrorMessage] = useState<string | null>(
    null
  );
  const [errors, setErrors] = useState<string[] | null>(null);

  const clearSuccess = () => setSuccess(null);
  const clearError = () => {
    setInitialErrorMessage(null);
    setErrors(null);
  };
  const clearAll = () => {
    setSuccess(null);
    setInitialErrorMessage(null);
    setErrors(null);
  };

  const handleResponse = <T>(
    result: CoreMsApiResonse<T>,
    errorContext: string,
    successMessage?: string
  ) => {
    // Clear previous messages first
    setSuccess(null);
    setInitialErrorMessage(null);
    setErrors(null);

    const messageState = ApiResponseHandler.handleApiResponse(
      result,
      errorContext,
      successMessage
    );

    // Set new messages after a small delay to ensure React sees the change
    setTimeout(() => {
      setInitialErrorMessage(messageState.initialErrorMessage);
      setErrors(messageState.errors);
      setSuccess(messageState.success);
    }, 10);
  };

  return {
    success,
    initialErrorMessage,
    errors,
    clearSuccess,
    clearError,
    clearAll,
    handleResponse,
  };
}

export default ApiResponseHandler;
