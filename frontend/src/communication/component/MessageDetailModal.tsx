import React, { useEffect, useState } from "react";
import { Badge, ListGroup, Spinner } from "react-bootstrap";
import { Envelope, ChatDots, Paperclip, Download } from "react-bootstrap-icons";
import { useTranslation } from "react-i18next";
import { ModalDialog } from "@/common/component/ModalDialog";
import { Message, EmailPayload, SmsPayload } from "@/communication/model/Message";
import { fetchDocumentsByUuids } from "@/document/utils/DocumentApi";
import { getDocumentDownloadUrl } from "@/document/store/DocumentState";
import { formatDate } from "@/common/utils/DateUtils";
import type { Document } from "@/document/model/Document";

interface MessageDetailModalProps {
  show: boolean;
  message: Message | null;
  onClose: () => void;
  userNames?: Record<string, string>;
}

export const MessageDetailModal: React.FC<MessageDetailModalProps> = ({
  show,
  message,
  onClose,
  userNames = {},
}) => {
  const { t } = useTranslation();
  const [documents, setDocuments] = useState<Document[]>([]);
  const [loadingDocs, setLoadingDocs] = useState(false);

  useEffect(() => {
    if (!show || !message) {
      setDocuments([]);
      return;
    }

    if (message.type === "email") {
      const payload = message.payload as EmailPayload;
      if (payload.documentUuids && payload.documentUuids.length > 0) {
        setLoadingDocs(true);
        fetchDocumentsByUuids(payload.documentUuids)
          .then(setDocuments)
          .catch(() => setDocuments([]))
          .finally(() => setLoadingDocs(false));
      } else {
        setDocuments([]);
      }
    }
  }, [show, message]);

  if (!message) return null;

  const isEmail = message.type === "email";
  const payload = message.payload;

  const renderEmailDetail = () => {
    const email = payload as EmailPayload;
    return (
      <>
        {/* Header info */}
        <div className="mb-3 border-bottom pb-3">
          <div className="d-flex align-items-center gap-2 mb-2">
            <Envelope className="text-info" />
            <strong>{email.subject}</strong>
          </div>
          <div className="row small text-muted">
            <div className="col-md-6">
              <div>
                <strong>{t("message.from", "From")}:</strong>{" "}
                {email.senderName ? `${email.senderName} <${email.sender}>` : email.sender || "—"}
              </div>
              <div>
                <strong>{t("message.to", "To")}:</strong> {email.recipient}
              </div>
              {email.cc && email.cc.length > 0 && (
                <div>
                  <strong>CC:</strong> {email.cc.join(", ")}
                </div>
              )}
              {email.bcc && email.bcc.length > 0 && (
                <div>
                  <strong>BCC:</strong> {email.bcc.join(", ")}
                </div>
              )}
            </div>
            <div className="col-md-6 text-md-end">
              <div>
                <strong>{t("message.sent", "Sent")}:</strong>{" "}
                {message.createdAt ? formatDate(message.createdAt) : "—"}
              </div>
              <div>
                <strong>{t("message.format", "Format")}:</strong>{" "}
                <Badge bg={email.emailType === "html" ? "info" : "secondary"}>
                  {email.emailType.toUpperCase()}
                </Badge>
              </div>
              <div>
                <strong>{t("message.sentBy", "Sent By")}:</strong>{" "}
                {message.sentByType === "user" && message.sentById
                  ? userNames[message.sentById] || message.sentById
                  : t("message.system", "System")}
              </div>
            </div>
          </div>
        </div>

        {/* Body content */}
        <div className="mb-3">
          {email.emailType === "html" ? (
            <div
              className="border rounded p-3 bg-white"
              style={{ minHeight: "200px", maxHeight: "500px", overflowY: "auto" }}
              dangerouslySetInnerHTML={{ __html: email.body }}
            />
          ) : (
            <div
              className="border rounded p-3 bg-light"
              style={{ minHeight: "100px", maxHeight: "500px", overflowY: "auto", whiteSpace: "pre-wrap" }}
            >
              {email.body}
            </div>
          )}
        </div>

        {/* Attachments */}
        {email.documentUuids && email.documentUuids.length > 0 && (
          <div>
            <div className="d-flex align-items-center gap-2 mb-2">
              <Paperclip />
              <strong className="small">
                {t("document.attachments", "Attachments")} ({email.documentUuids.length})
              </strong>
            </div>
            {loadingDocs ? (
              <div className="text-center p-2">
                <Spinner animation="border" size="sm" />
              </div>
            ) : documents.length > 0 ? (
              <ListGroup variant="flush">
                {documents.map((doc) => (
                  <ListGroup.Item
                    key={doc.uuid}
                    className="d-flex justify-content-between align-items-center py-2 px-0"
                  >
                    <div className="d-flex align-items-center gap-2">
                      <span>{doc.name}</span>
                      <Badge bg="secondary" pill>
                        {doc.extension.toUpperCase()}
                      </Badge>
                      <small className="text-muted">
                        {(doc.size / 1024).toFixed(1)} KB
                      </small>
                    </div>
                    <a
                      href={getDocumentDownloadUrl(doc.uuid)}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="btn btn-outline-primary btn-sm d-flex align-items-center gap-1"
                    >
                      <Download size={14} />
                      {t("common.download", "Download")}
                    </a>
                  </ListGroup.Item>
                ))}
              </ListGroup>
            ) : (
              <p className="text-muted small">
                {t("document.unavailable", "Documents not available")}
              </p>
            )}
          </div>
        )}
      </>
    );
  };

  const renderSmsDetail = () => {
    const sms = payload as SmsPayload;
    return (
      <>
        {/* Header info */}
        <div className="mb-3 border-bottom pb-3">
          <div className="d-flex align-items-center gap-2 mb-2">
            <ChatDots className="text-warning" />
            <strong>{t("message.smsMessage", "SMS Message")}</strong>
          </div>
          <div className="small text-muted">
            <div>
              <strong>{t("message.to", "To")}:</strong> {sms.phoneNumber}
            </div>
            <div>
              <strong>{t("message.sent", "Sent")}:</strong>{" "}
              {message.createdAt ? formatDate(message.createdAt) : "—"}
            </div>
            <div>
              <strong>{t("message.sentBy", "Sent By")}:</strong>{" "}
              {message.sentByType === "user" && message.sentById
                ? userNames[message.sentById] || message.sentById
                : t("message.system", "System")}
            </div>
          </div>
        </div>

        {/* Message content */}
        <div
          className="border rounded p-3 bg-light"
          style={{ minHeight: "80px", whiteSpace: "pre-wrap" }}
        >
          {sms.message}
        </div>
      </>
    );
  };

  const title = isEmail
    ? (payload as EmailPayload).subject || t("message.emailDetail", "Email Detail")
    : t("message.smsDetail", "SMS Detail");

  return (
    <ModalDialog
      title={title}
      show={show}
      onClose={onClose}
      size="lg"
      secondaryText={t("common.close", "Close")}
    >
      {isEmail ? renderEmailDetail() : renderSmsDetail()}
    </ModalDialog>
  );
};

export default MessageDetailModal;
