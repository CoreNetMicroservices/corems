import { hookstate, useHookstate } from "@hookstate/core";
import { devtools } from "@hookstate/devtools";
import CoreMsApi from "@/common/utils/api/CoreMsApi";
import { COMMUNICATION_MS_BASE_URL } from "@/communication/config";
import { HttpMethod, PageResponse } from "@/common/model/CoreMsApiModel";
import { Message } from "@/communication/model/Message";
import { DataTableQueryParams } from "@/common/component/dataTable/DataTableTypes";
import { buildUrlSearchParams } from "@/common/component/dataTable/DataTableState";

const communicationMsApi = new CoreMsApi({
  baseURL: COMMUNICATION_MS_BASE_URL,
});

type MessagesPagedResponse = PageResponse<Message>;

interface MessageState {
  messages: Message[];
  pagedResponse?: MessagesPagedResponse;
}

const initialState: MessageState = {
  messages: [],
  pagedResponse: undefined,
};

const messageState = hookstate(initialState, devtools({ key: "messageState" }));

export const useMessagesState = () => {
  const state = useHookstate(messageState);

  const fetchMessages = async (queryParams: DataTableQueryParams) => {
    const params = buildUrlSearchParams(queryParams);

    const response = await communicationMsApi.apiRequest<MessagesPagedResponse>(
      HttpMethod.GET,
      `/api/messages?${params.toString()}`
    );

    if (response.result && response.response) {
      // state.pagedResponse.set(response.response);
      // state.messages.set(response.response.items);
    }

    return response;
  };

  const sendEmailMessage = async (data: {
    userId?: string; // target user (if selected)
    recipient: string; // direct email if not using userId or for explicit overrides
    subject: string;
    body: string;
    emailType: "html" | "txt";
    cc?: string[];
    bcc?: string[];
    documentUuids?: string[];
  }) => {
    const requestData = {
      ...data,
      payloadType: "email" as const, // Required by API spec
    };
    
    const response = await communicationMsApi.apiRequest<Message>(
      HttpMethod.POST,
      `/api/messages/email`,
      requestData
    );
    return response;
  };

  const sendSmsMessage = async (data: {
    userId?: string;
    phoneNumber: string;
    message: string;
  }) => {
    const requestData = {
      ...data,
      payloadType: "sms" as const, // Required by API spec
    };
    
    const response = await communicationMsApi.apiRequest<Message>(
      HttpMethod.POST,
      `/api/messages/sms`,
      requestData
    );
    return response;
  };

  return {
    state,
    fetchMessages,
    sendEmailMessage,
    sendSmsMessage,
  };
};
