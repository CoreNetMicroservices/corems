import React, { useState } from "react";
import { Form, Button, InputGroup, Alert } from "react-bootstrap";
import { useTranslation } from "react-i18next";
import { Clipboard, ClipboardCheck } from "react-bootstrap-icons";
import { ModalDialog } from "@/common/component/ModalDialog";
import {
  generateDocumentAccessLink,
  getPublicDocumentUrl,
} from "@/document/store/DocumentState";
import { Document, Visibility } from "@/document/model/Document";
import { useMessageState } from "@/common/utils/api/ApiResponseHandler";
import { AlertMessage } from "@/common/component/ApiResponseAlert";

interface DocumentLinkModalProps {
  document: Document | null;
  show: boolean;
  onClose: () => void;
}

export const DocumentLinkModal: React.FC<DocumentLinkModalProps> = ({
  document,
  show,
  onClose,
}) => {
  const { t } = useTranslation();
  const [expiresInHours, setExpiresInHours] = useState<number>(24);
  const [generatedLink, setGeneratedLink] = useState<string | null>(null);
  const [isCopied, setIsCopied] = useState(false);
  const [isGenerating, setIsGenerating] = useState(false);

  const { initialErrorMessage, errors, handleResponse } = useMessageState();

  const handleGenerate = async () => {
    if (!document) return;

    // For PUBLIC documents, just show the public URL
    if (document.visibility === Visibility.PUBLIC) {
      setGeneratedLink(getPublicDocumentUrl(document.uuid));
      setIsCopied(false);
      return;
    }

    // Check if document visibility is BY_LINK
    if (document.visibility !== Visibility.BY_LINK) {
      handleResponse(
        {
          result: false,
          response: null,
          errors: [
            {
              reasonCode: "document.invalid_visibility",
              description: "Only documents with BY_LINK or PUBLIC visibility can generate access links",
            },
          ],
        },
        t("document.linkGenerationFailed", "Failed to generate link")
      );
      return;
    }

    setIsGenerating(true);
    const result = await generateDocumentAccessLink(document.uuid, {
      expiresInHours,
    });

    handleResponse(
      result,
      t("document.linkGenerationFailed", "Failed to generate link"),
      t("document.linkGenerationSuccess", "Link generated successfully")
    );

    if (result.result && result.response) {
      setGeneratedLink(result.response.url);
      setIsCopied(false);
    }

    setIsGenerating(false);
  };

  const handleCopy = async () => {
    if (!generatedLink) return;

    try {
      await navigator.clipboard.writeText(generatedLink);
      setIsCopied(true);
      setTimeout(() => setIsCopied(false), 2000);
    } catch (error) {
      console.error("Failed to copy:", error);
    }
  };

  const handleClose = () => {
    setGeneratedLink(null);
    setIsCopied(false);
    setExpiresInHours(24);
    onClose();
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
      title={t("document.generateLink", "Generate Document Link")}
      size="lg"
      secondaryText={t("common.close", "Close")}
      onPrimary={
        !generatedLink &&
        (document?.visibility === Visibility.BY_LINK || document?.visibility === Visibility.PUBLIC)
          ? handleGenerate
          : undefined
      }
      primaryText={
        !generatedLink &&
        (document?.visibility === Visibility.BY_LINK || document?.visibility === Visibility.PUBLIC)
          ? isGenerating
            ? t("common.generating", "Generating...")
            : document?.visibility === Visibility.PUBLIC
            ? t("document.showLink", "Show Link")
            : t("document.generateLink", "Generate Link")
          : undefined
      }
      footerContent={
        generatedLink && (
          <Button
            variant="outline-primary"
            onClick={() => {
              setGeneratedLink(null);
              setIsCopied(false);
              setExpiresInHours(24);
            }}
          >
            {t("document.generateAnother", "Generate Another Link")}
          </Button>
        )
      }
    >
      <AlertMessage initialErrorMessage={initialErrorMessage} errors={errors} />

      {document && document.visibility === Visibility.PRIVATE && (
        <Alert variant="warning">
          {t(
            "document.linkOnlyForByLinkOrPublic",
            "Links can only be generated for documents with BY_LINK or PUBLIC visibility. This document is PRIVATE.",
            { visibility: document.visibility }
          )}
        </Alert>
      )}

      {document && document.visibility === Visibility.PUBLIC && !generatedLink && (
        <Alert variant="info">
          {t(
            "document.publicDocumentInfo",
            "This is a PUBLIC document. Anyone can access it without authentication."
          )}
        </Alert>
      )}

      {document && (document.visibility === Visibility.BY_LINK || document.visibility === Visibility.PUBLIC) && (
        <>
          {!generatedLink ? (
            <>
              {document.visibility === Visibility.BY_LINK && (
                <Form.Group className="mb-3">
                  <Form.Label>
                    {t("document.expiresIn", "Link Expiration")}
                  </Form.Label>
                  <div className="d-flex gap-2 mb-2">
                    {getExpirationPresets().map((preset) => (
                      <Button
                        key={preset.value}
                        size="sm"
                        variant={
                          expiresInHours === preset.value
                            ? "primary"
                            : "outline-secondary"
                        }
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
                      onChange={(e) =>
                        setExpiresInHours(parseInt(e.target.value) || 24)
                      }
                      min={1}
                      max={8760}
                    />
                    <InputGroup.Text>
                      {t("document.hours", "hours")}
                    </InputGroup.Text>
                  </InputGroup>
                  <Form.Text className="text-muted">
                    {t(
                      "document.expiresInHelp",
                      "Specify how long the link will be valid (1 hour to 1 year)"
                    )}
                  </Form.Text>
                </Form.Group>
              )}
            </>
          ) : (
            <>
              <Form.Group className="mb-3">
                <Form.Label>{t("document.accessLink", "Access Link")}</Form.Label>
                <InputGroup>
                  <Form.Control
                    type="text"
                    value={generatedLink}
                    readOnly
                    className="font-monospace"
                  />
                  <Button
                    variant={isCopied ? "success" : "outline-secondary"}
                    onClick={handleCopy}
                  >
                    {isCopied ? (
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
            </>
          )}
        </>
      )}
    </ModalDialog>
  );
};

export default DocumentLinkModal;
