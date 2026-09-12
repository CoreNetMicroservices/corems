import React, { useState, useEffect, useCallback } from "react";
import { Form, Button, InputGroup, Alert, Badge, Spinner, Accordion } from "react-bootstrap";
import { useTranslation } from "react-i18next";
import { Clipboard, ClipboardCheck, Trash } from "react-bootstrap-icons";
import { ModalDialog } from "@/common/component/ModalDialog";
import {
  generateDocumentAccessLink,
  getPublicDocumentUrl,
  getPublicDocumentViewUrl,
  listDocumentAccessLinks,
  revokeDocumentAccessLink,
} from "@/document/store/DocumentState";
import { Document, Visibility, DocumentLinkInfo } from "@/document/model/Document";
import { useMessageState } from "@/common/utils/api/ApiResponseHandler";
import { AlertMessage } from "@/common/component/ApiResponseAlert";
import { formatDate } from "@/common/utils/DateUtils";

interface DocumentLinkModalProps {
  document: Document | null;
  show: boolean;
  onClose: () => void;
}

interface CopyFieldProps {
  label: string;
  value: string;
}

const CopyField: React.FC<CopyFieldProps> = ({ label, value }) => {
  const { t } = useTranslation();
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(value);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (error) {
      console.error("Failed to copy:", error);
    }
  };

  return (
    <Form.Group className="mb-2">
      <Form.Label className="small mb-1">{label}</Form.Label>
      <InputGroup size="sm">
        <Form.Control type="text" value={value} readOnly className="font-monospace" />
        <Button variant={copied ? "success" : "outline-secondary"} onClick={handleCopy}>
          {copied ? (
            <>
              <ClipboardCheck className="me-1" />
              {t("common.copied", "Copied!")}
            </>
          ) : (
            <>
              <Clipboard className="me-1" />
              {t("common.copy", "Copy")}
            </>
          )}
        </Button>
      </InputGroup>
    </Form.Group>
  );
};

export const DocumentLinkModal: React.FC<DocumentLinkModalProps> = ({
  document,
  show,
  onClose,
}) => {
  const { t } = useTranslation();
  const [expiresInHours, setExpiresInHours] = useState<number>(24);
  const [isGenerating, setIsGenerating] = useState(false);
  const [existingLinks, setExistingLinks] = useState<DocumentLinkInfo[]>([]);
  const [isLoadingLinks, setIsLoadingLinks] = useState(false);
  const [revokingId, setRevokingId] = useState<number | null>(null);
  const [activeKey, setActiveKey] = useState<string | null>(null);

  const { initialErrorMessage, errors, handleResponse } = useMessageState();

  const isByLink = document?.visibility === Visibility.BY_LINK;
  const isPublic = document?.visibility === Visibility.PUBLIC;

  const loadLinks = useCallback(async () => {
    if (!document || document.visibility !== Visibility.BY_LINK) {
      setExistingLinks([]);
      return;
    }
    setIsLoadingLinks(true);
    const result = await listDocumentAccessLinks(document.uuid);
    setIsLoadingLinks(false);
    if (result.result && result.response) {
      setExistingLinks(result.response);
    }
  }, [document]);

  useEffect(() => {
    if (show) {
      setActiveKey(null);
      loadLinks();
    }
  }, [show, loadLinks]);

  const handleGenerate = async () => {
    if (!document || !isByLink) return;

    setIsGenerating(true);
    const result = await generateDocumentAccessLink(document.uuid, {
      expiresInMinutes: expiresInHours * 60,
    });
    setIsGenerating(false);

    handleResponse(
      result,
      t("document.linkGenerationFailed", "Failed to generate link"),
      t("document.linkGenerationSuccess", "Link generated successfully")
    );

    if (result.result && result.response) {
      const refreshed = await listDocumentAccessLinks(document.uuid);
      if (refreshed.result && refreshed.response) {
        setExistingLinks(refreshed.response);
        // Newest link is first (backend orders by createdAt desc) — expand it.
        if (refreshed.response.length > 0) {
          setActiveKey(String(refreshed.response[0].id));
        }
      }
    }
  };

  const handleRevoke = async (linkId: number) => {
    if (!document) return;
    const confirmed = window.confirm(
      t(
        "document.revokeLinkConfirm",
        "Are you sure you want to revoke this link? Anyone using it will lose access immediately."
      )
    );
    if (!confirmed) return;

    setRevokingId(linkId);
    const result = await revokeDocumentAccessLink(document.uuid, linkId);
    setRevokingId(null);

    handleResponse(
      result,
      t("document.revokeLinkFailed", "Failed to revoke link"),
      t("document.revokeLinkSuccess", "Link revoked successfully")
    );

    if (result.result) {
      await loadLinks();
    }
  };

  const handleClose = () => {
    setExpiresInHours(24);
    setActiveKey(null);
    onClose();
  };

  const getLinkStatus = (link: DocumentLinkInfo) => {
    if (link.isRevoked) {
      return <Badge bg="danger">{t("document.linkRevoked", "Revoked")}</Badge>;
    }
    if (new Date(link.expiresAt) <= new Date()) {
      return <Badge bg="secondary">{t("document.linkExpired", "Expired")}</Badge>;
    }
    return <Badge bg="success">{t("document.linkActive", "Active")}</Badge>;
  };

  const getExpirationPresets = () => [
    { label: "1 hour", value: 1 },
    { label: "6 hours", value: 6 },
    { label: "24 hours", value: 24 },
    { label: "7 days", value: 168 },
    { label: "30 days", value: 720 },
  ];

  return (
    <ModalDialog
      show={show}
      onClose={handleClose}
      title={t("document.documentLinks", "Document Links")}
      size="lg"
      secondaryText={t("common.close", "Close")}
      onPrimary={isByLink ? handleGenerate : undefined}
      primaryText={
        isByLink
          ? isGenerating
            ? t("common.generating", "Generating...")
            : t("document.generateLink", "Generate Link")
          : undefined
      }
      disablePrimary={isGenerating}
    >
      <AlertMessage initialErrorMessage={initialErrorMessage} errors={errors} />

      {document && document.visibility === Visibility.PRIVATE && (
        <Alert variant="warning">
          {t(
            "document.linkOnlyForByLinkOrPublic",
            "Links can only be generated for documents with BY_LINK or PUBLIC visibility. This document is PRIVATE."
          )}
        </Alert>
      )}

      {document && isPublic && (
        <>
          <Alert variant="info">
            {t(
              "document.publicDocumentInfo",
              "This is a PUBLIC document. Anyone can access it without authentication."
            )}
          </Alert>
          <CopyField
            label={t("document.viewLink", "View link")}
            value={getPublicDocumentViewUrl(document.uuid)}
          />
          <CopyField
            label={t("document.downloadLink", "Download link")}
            value={getPublicDocumentUrl(document.uuid)}
          />
        </>
      )}

      {document && isByLink && (
        <>
          <Form.Group className="mb-3">
            <Form.Label>{t("document.expiresIn", "Link Expiration")}</Form.Label>
            <div className="d-flex gap-2 mb-2 flex-wrap">
              {getExpirationPresets().map((preset) => (
                <Button
                  key={preset.value}
                  size="sm"
                  variant={expiresInHours === preset.value ? "primary" : "outline-secondary"}
                  onClick={() => setExpiresInHours(preset.value)}
                >
                  {preset.label}
                </Button>
              ))}
            </div>
            <InputGroup>
              <Form.Control
                type="number"
                value={expiresInHours}
                onChange={(e) => setExpiresInHours(parseInt(e.target.value) || 24)}
                min={1}
                max={8760}
              />
              <InputGroup.Text>{t("document.hours", "hours")}</InputGroup.Text>
            </InputGroup>
            <Form.Text className="text-muted">
              {t(
                "document.expiresInHelp",
                "Specify how long the link will be valid (1 hour to 1 year)"
              )}
            </Form.Text>
          </Form.Group>

          <div className="d-flex justify-content-between align-items-center mb-2">
            <h6 className="mb-0">{t("document.existingLinks", "Existing Links")}</h6>
            {isLoadingLinks && <Spinner animation="border" size="sm" />}
          </div>

          {existingLinks.length === 0 ? (
            !isLoadingLinks && (
              <p className="text-muted small mb-0">
                {t("document.noLinks", "No links have been generated yet.")}
              </p>
            )
          ) : (
            <Accordion activeKey={activeKey} onSelect={(k) => setActiveKey(k as string | null)}>
              {existingLinks.map((link) => {
                const isActive = !link.isRevoked && new Date(link.expiresAt) > new Date();
                return (
                  <Accordion.Item eventKey={String(link.id)} key={link.id}>
                    <Accordion.Header>
                      <span className="d-flex align-items-center gap-2 flex-wrap">
                        {getLinkStatus(link)}
                        <span className="small text-muted">
                          {t("document.created", "Created")}: {formatDate(link.createdAt)}
                        </span>
                        <span className="small text-muted">
                          {t("document.uses", "Uses")}: {link.accessCount}
                        </span>
                      </span>
                    </Accordion.Header>
                    <Accordion.Body>
                      <p className="small mb-2 text-muted">
                        {t("document.expires", "Expires")}: {formatDate(link.expiresAt)}
                      </p>
                      {isActive ? (
                        <>
                          <CopyField label={t("document.viewLink", "View link")} value={link.viewUrl} />
                          <CopyField
                            label={t("document.downloadLink", "Download link")}
                            value={link.downloadUrl}
                          />
                          <CopyField label={t("document.infoLink", "Info link")} value={link.infoUrl} />
                          <div className="text-end mt-2">
                            <Button
                              variant="outline-danger"
                              size="sm"
                              onClick={() => handleRevoke(link.id)}
                              disabled={revokingId === link.id}
                            >
                              {revokingId === link.id ? (
                                <Spinner animation="border" size="sm" />
                              ) : (
                                <>
                                  <Trash className="me-1" />
                                  {t("document.revoke", "Revoke")}
                                </>
                              )}
                            </Button>
                          </div>
                        </>
                      ) : (
                        <p className="small text-muted mb-0">
                          {link.isRevoked
                            ? t("document.linkRevokedInfo", "This link has been revoked and no longer works.")
                            : t("document.linkExpiredInfo", "This link has expired and no longer works.")}
                        </p>
                      )}
                    </Accordion.Body>
                  </Accordion.Item>
                );
              })}
            </Accordion>
          )}
        </>
      )}
    </ModalDialog>
  );
};

export default DocumentLinkModal;
