export interface SmsPayload {
  phoneNumber: string;
  message: string;
}

export interface EmailPayload {
  emailType: "txt" | "html";
  cc?: string[];
  bcc?: string[];
  sender?: string;
  senderName?: string;
  subject: string;
  recipient: string;
  body: string;
  documentUuids?: string[];
}

export interface Message {
  uuid: string;
  type: "sms" | "email";
  status: "created" | "enqueued" | "sent" | "failed";
  userId: string;
  // New metadata for identifying who initiated the message
  sentById?: string;
  sentByType?: "user" | "system";
  createdAt: string;
  payloadType: "sms" | "email";
  payload: SmsPayload | EmailPayload;
}

export interface MessageFilter {
  type?: string;
  status?: string;
  startDate?: string;
  endDate?: string;
}
