import React, { useState, useEffect } from "react";
import { Row, Col, Form, Badge, Button } from "react-bootstrap";
import { useTranslation } from "react-i18next";
import { Link45deg } from "react-bootstrap-icons";
import { ModalDialog } from "@/common/component/ModalDialog";
import { updateDocument } from "@/document/store/DocumentState";
import { Document, Visibility, UploadedByType } from "@/document/model/Document";
import { useMessageState } from "@/common/utils/api/ApiResponseHandler";
import { formatDate } from "@/common/utils/DateUtils";
import { resolveUserNames } from "@/user/utils/UserApi";
import { getDocumentDownloadUrl } from "@/document/store/DocumentState";
import { hasAnyRole } from "@/user/store/AuthState";
import { AppRoles } from "@/common/AppRoles";
import DocumentLinkModal from "@/document/component/DocumentLinkModal";

interface DocumentDetailsModalProps {
  document: Document | null;
  show: boolean;
  onClose: () => void;
  onUpdated?: () => void;
}

export const DocumentDetailsModal: React.FC<DocumentDetailsModalProps> = ({
  document,
  show,
  onClose,
  onUpdated,
}) => {
  const { t } = useTranslation();
  const [isEditing, setIsEditing] = useState(false);
  const [editForm, setEditForm] = useState({
    name: "",
    description: "",
    tags: "",
    visibility: Visibility.PRIVATE,
  });
  const [uploadedByUserName, setUploadedByUserName] = useState<string | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [isLoadingPreview, setIsLoadingPreview] = useState(false);
  const [showLinkModal, setShowLinkModal] = useState(false);

  const { handleResponse } = useMessageState();

  useEffect(() => {
    if (document) {
      // Reset all state when document changes
      setIsEditing(false);
      closePreview();
      
      setEditForm({
        name: document.name,
        description: document.description || "",
        tags: document.tags || "",
        visibility: document.visibility,
      });

      // Resolve uploaded by user name
      if (document.uploadedByType === UploadedByType.USER && document.uploadedById) {
        setUploadedByUserName(null); // Reset to loading state
        resolveUserNames([document.uploadedById])
          .then((names) => {
            const resolvedName = names[document.uploadedById];
            setUploadedByUserName(resolvedName || document.uploadedById);
          })
          .catch((error) => {
            console.error("Error resolving user names:", error);
            setUploadedByUserName(document.uploadedById);
          });
      } else {
        setUploadedByUserName(null);
      }
    }
  }, [document]);

  function formatFileSize(bytes: number): string {
    if (bytes < 1024) return bytes + " B";
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + " KB";
    return (bytes / (1024 * 1024)).toFixed(2) + " MB";
  }

  function getVisibilityBadge(visibility: Visibility) {
    const variants = {
      [Visibility.PUBLIC]: "success",
      [Visibility.PRIVATE]: "secondary",
      [Visibility.BY_LINK]: "info",
    };
    return <Badge bg={variants[visibility]}>{visibility}</Badge>;
  }

  async function handleSave() {
    if (!document) return;
    const result = await updateDocument(document.uuid, editForm);
    handleResponse(
      result,
      t("document.updateFailed", "Failed to update document"),
      t("document.updateSuccess", "Document updated successfully")
    );
    if (result.result) {
      setIsEditing(false);
      if (onUpdated) onUpdated();
      onClose();
    }
  }

  function handleCancel() {
    if (isEditing) {
      setIsEditing(false);
      if (document) {
        setEditForm({
          name: document.name,
          description: document.description || "",
          tags: document.tags || "",
          visibility: document.visibility,
        });
      }
    } else {
      onClose();
    }
  }

  function isPreviewable(contentType: string): boolean {
    return (
      contentType.startsWith("image/") ||
      contentType === "application/pdf" ||
      contentType.startsWith("text/")
    );
  }

  async function handlePreview() {
    if (!document) return;
    
    setIsLoadingPreview(true);
    try {
      const accessToken = localStorage.getItem("accessToken");
      const url = getDocumentDownloadUrl(document.uuid);
      
      const response = await fetch(url, {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      });

      if (!response.ok) {
        throw new Error("Failed to load preview");
      }

      const blob = await response.blob();
      const blobUrl = window.URL.createObjectURL(blob);
      setPreviewUrl(blobUrl);
    } catch (error) {
      console.error("Preview error:", error);
      handleResponse(
        { result: false, response: null, errors: [{ reasonCode: "preview.failed", description: "Failed to load preview" }] },
        t("document.previewFailed", "Failed to load preview")
      );
    } finally {
      setIsLoadingPreview(false);
    }
  }

  function closePreview() {
    if (previewUrl) {
      window.URL.revokeObjectURL(previewUrl);
      setPreviewUrl(null);
    }
  }

  useEffect(() => {
    return () => {
      // Cleanup preview URL on unmount
      if (previewUrl) {
        window.URL.revokeObjectURL(previewUrl);
      }
    };
  }, [previewUrl]);

  return (
    <ModalDialog
      show={show}
      onClose={handleCancel}
      title={t("document.documentDetails", "Document Details")}
      size="xl"
      onPrimary={isEditing ? handleSave : undefined}
      primaryText={isEditing ? t("common.save", "Save") : undefined}
      secondaryText={
        isEditing ? t("common.cancel", "Cancel") : t("common.close", "Close")
      }
      footerContent={
        !isEditing && (
          <>
          {document && isPreviewable(document.contentType) && (
              <Button
                variant="outline-info"
                onClick={previewUrl ? closePreview : handlePreview}
                disabled={isLoadingPreview}
                className="me-2"
              >
                {isLoadingPreview
                  ? t("common.loading", "Loading...")
                  : previewUrl
                  ? t("document.closePreview", "Close Preview")
                  : t("document.preview", "Preview")}
              </Button>
            )}
            <Button
              variant="outline-success"
              onClick={() => setShowLinkModal(true)}
              className="me-2"
            >
              <Link45deg className="me-1" />
              {t("document.createLink", "Create Link")}
            </Button>
            {hasAnyRole([AppRoles.DocumentMsAdmin]) && (
              <Button
                variant="primary"
                onClick={() => setIsEditing(true)}
              >
                {t("common.edit", "Edit")}
              </Button>
            )}
          </>
        )
      }
    >
      {document && (
        <>
          {previewUrl && (
            <div className="mb-3">
              {document.contentType.startsWith("image/") ? (
                <img
                  src={previewUrl}
                  alt={document.name}
                  style={{ maxWidth: "100%", maxHeight: "500px" }}
                />
              ) : document.contentType === "application/pdf" ? (
                <iframe
                  src={previewUrl}
                  title={document.name}
                  style={{ width: "100%", height: "500px", border: "1px solid #dee2e6" }}
                />
              ) : (
                <iframe
                  src={previewUrl}
                  title={document.name}
                  style={{ width: "100%", height: "500px", border: "1px solid #dee2e6" }}
                />
              )}
            </div>
          )}
          <Row>
            <Col md={6}>
            <h6 className="text-muted mb-3">
              {t("document.fileInformation", "File Information")}
            </h6>
            <p className="mb-2">
              <strong>{t("document.uuid", "UUID")}:</strong>{" "}
              <code className="text-muted small">{document.uuid}</code>
            </p>
            <p className="mb-2">
              <strong>
                {t("document.originalFilename", "Original Filename")}:
              </strong>{" "}
              {document.originalFilename}
            </p>
            <p className="mb-2">
              <strong>{t("document.size", "Size")}:</strong>{" "}
              {formatFileSize(document.size)}
            </p>
            <p className="mb-2">
              <strong>{t("document.type", "Type")}:</strong>{" "}
              {document.contentType}
            </p>
            <p className="mb-2">
              <strong>{t("document.extension", "Extension")}:</strong>{" "}
              {document.extension}
            </p>
            <p className="mb-2">
              <strong>{t("document.checksum", "Checksum")}:</strong>{" "}
              <code className="text-muted small">{document.checksum}</code>
            </p>
            <p className="mb-2">
              <strong>{t("document.created", "Created")}:</strong>{" "}
              {formatDate(document.createdAt)}
            </p>
            <p className="mb-2">
              <strong>{t("document.updated", "Updated")}:</strong>{" "}
              {formatDate(document.updatedAt)}
            </p>
            <p className="mb-2">
              <strong>{t("document.uploadedBy", "Uploaded By")}:</strong>{" "}
              {document.uploadedByType === UploadedByType.SYSTEM
                ? t("document.system", "System")
                : uploadedByUserName === null
                ? t("common.loading", "Loading...")
                : uploadedByUserName}
            </p>
          </Col>
          <Col md={6}>
            <h6 className="text-muted mb-3">
              {t("document.metadata", "Metadata")}
            </h6>
            {isEditing ? (
              <Form>
                <Form.Group className="mb-3">
                  <Form.Label>{t("document.name", "Name")}</Form.Label>
                  <Form.Control
                    type="text"
                    value={editForm.name}
                    onChange={(e) =>
                      setEditForm({ ...editForm, name: e.target.value })
                    }
                    maxLength={255}
                  />
                </Form.Group>
                <Form.Group className="mb-3">
                  <Form.Label>
                    {t("document.visibility", "Visibility")}
                  </Form.Label>
                  <Form.Select
                    value={editForm.visibility}
                    onChange={(e) =>
                      setEditForm({
                        ...editForm,
                        visibility: e.target.value as Visibility,
                      })
                    }
                  >
                    <option value={Visibility.PRIVATE}>
                      {t("document.private", "Private")}
                    </option>
                    <option value={Visibility.PUBLIC}>
                      {t("document.public", "Public")}
                    </option>
                    <option value={Visibility.BY_LINK}>
                      {t("document.byLink", "By Link")}
                    </option>
                  </Form.Select>
                </Form.Group>
                <Form.Group className="mb-3">
                  <Form.Label>
                    {t("document.description", "Description")}
                  </Form.Label>
                  <Form.Control
                    as="textarea"
                    rows={3}
                    value={editForm.description}
                    onChange={(e) =>
                      setEditForm({ ...editForm, description: e.target.value })
                    }
                    maxLength={1000}
                  />
                </Form.Group>
                <Form.Group className="mb-3">
                  <Form.Label>{t("document.tags", "Tags")}</Form.Label>
                  <Form.Control
                    type="text"
                    value={editForm.tags}
                    onChange={(e) =>
                      setEditForm({ ...editForm, tags: e.target.value })
                    }
                    placeholder="tag1, tag2, tag3"
                  />
                </Form.Group>
              </Form>
            ) : (
              <>
                <p className="mb-2">
                  <strong>{t("document.name", "Name")}:</strong> {document.name}
                </p>
                <p className="mb-2">
                  <strong>{t("document.visibility", "Visibility")}:</strong>{" "}
                  {getVisibilityBadge(document.visibility)}
                </p>
                <p className="mb-2">
                  <strong>{t("document.description", "Description")}:</strong>{" "}
                  {document.description || (
                    <span className="text-muted">—</span>
                  )}
                </p>
                <p className="mb-2">
                  <strong>{t("document.tags", "Tags")}:</strong>{" "}
                  {document.tags ? (
                    document.tags.split(",").map((tag) => (
                      <Badge key={tag} bg="secondary" className="me-1">
                        {tag.trim()}
                      </Badge>
                    ))
                  ) : (
                    <span className="text-muted">—</span>
                  )}
                </p>
              </>
            )}
          </Col>
        </Row>
        </>
      )}

      <DocumentLinkModal
        document={document}
        show={showLinkModal}
        onClose={() => setShowLinkModal(false)}
      />
    </ModalDialog>
  );
};

export default DocumentDetailsModal;
