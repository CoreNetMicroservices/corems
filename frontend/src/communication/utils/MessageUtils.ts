import {
  Message,
  SmsPayload,
  EmailPayload,
} from "@/communication/model/Message";

export const getRecipient = (msg: Message) => {
  if (msg.type === "sms") {
    return (msg.payload as SmsPayload).phoneNumber;
  } else if (msg.type === "email") {
    return (msg.payload as EmailPayload).recipient;
  }
  return msg.userId;
};

export const getContentPreview = (msg: Message) => {
  if (msg.type === "sms") {
    return (msg.payload as SmsPayload).message;
  } else if (msg.type === "email") {
    return (msg.payload as EmailPayload).subject;
  }
  return "";
};

export const getFullContent = (msg: Message) => {
  if (msg.type === "sms") {
    return (msg.payload as SmsPayload).message;
  } else if (msg.type === "email") {
    return (msg.payload as EmailPayload).body;
  }
  return "";
};
