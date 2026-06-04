import React from "react";
import { useTranslation } from "react-i18next";
import { ModalDialog } from "@/common/component/ModalDialog";
import { Document } from "@/document/model/Document";

interface DocumentDeleteModalProps {
  document: Document | null;
  show: boolean;
  onClose: () => void;
  onConfirm: () => void;
  isDeleting?: boolean;
}

export const DocumentDeleteModal: React.FC<DocumentDeleteModalProps> = ({
  document,
  show,
  onClose,
  onConfirm,
  isDeleting = false,
}) => {
  const { t } = useTranslation();

  if (!document) return null;

  const isPermanent = document.deleted;

  return (
    <ModalDialog
      show={show}
      onClose={onClose}
      title={
        isPermanent
          ? t("document.deletePermanently", "Delete Permanently")
          : t("common.delete", "Delete Document")
      }
      size="sm"
      onPrimary={onConfirm}
      primaryText={
        isDeleting
          ? t("common.deleting", "Deleting...")
          : isPermanent
          ? t("document.deletePermanently", "Delete Permanently")
          : t("common.delete", "Delete")
      }
      primaryVariant="danger"
      secondaryText={t("common.cancel", "Cancel")}
      disablePrimary={isDeleting}
    >
      <p>
        {isPermanent
          ? t(
              "document.confirmPermanentDelete",
              "Are you sure you want to permanently delete this document? This action cannot be undone."
            )
          : t(
              "document.confirmDelete",
              "Are you sure you want to delete this document?"
            )}
      </p>
      <p className="mb-0">
        <strong>{t("document.name", "Name")}:</strong> {document.name}
      </p>
    </ModalDialog>
  );
};

export default DocumentDeleteModal;
