import React, { useState } from "react";
import { Form, Button, Badge, ListGroup } from "react-bootstrap";
import { useTranslation } from "react-i18next";
import { AsyncSelect } from "@/common/component/dataTable/filter/AsyncSelect";
import { searchDocuments } from "@/document/utils/DocumentApi";
import type { Document } from "@/document/model/Document";

interface DocumentSelectorProps {
  selectedDocuments: Document[];
  onChange: (documents: Document[]) => void;
  userId?: string; // Filter documents by user
}

export const DocumentSelector: React.FC<DocumentSelectorProps> = ({
  selectedDocuments,
  onChange,
  userId,
}) => {
  const { t } = useTranslation();
  const [lastDocumentOptions, setLastDocumentOptions] = useState<Document[]>([]);

  const handleAddDocument = (documentUuid: string | number | undefined) => {
    if (!documentUuid || typeof documentUuid !== 'string') return;
    
    const doc = lastDocumentOptions.find((d) => d.uuid === documentUuid);
    if (doc && !selectedDocuments.find((d) => d.uuid === doc.uuid)) {
      onChange([...selectedDocuments, doc]);
    }
  };

  const handleRemoveDocument = (uuid: string) => {
    onChange(selectedDocuments.filter((d) => d.uuid !== uuid));
  };

  return (
    <div>
      <Form.Label>{t("document.attachDocuments", "Attach Documents")}</Form.Label>
      <AsyncSelect<Document>
        key={userId || 'no-user'}
        value={undefined}
        onChange={handleAddDocument}
        loadOptions={async (term) => {
          const res = await searchDocuments(term, userId);
          setLastDocumentOptions(res);
          return res;
        }}
        getOptionLabel={(doc) => doc.name}
        getOptionValue={(doc) => doc.uuid}
        getOptionSubtitle={(doc) => `${doc.extension.toUpperCase()} • ${(doc.size / 1024).toFixed(1)} KB`}
        placeholder={t("document.searchDocuments", "Search documents...")}
      />

      {selectedDocuments.length > 0 && (
        <ListGroup className="mt-2">
          {selectedDocuments.map((doc) => (
            <ListGroup.Item
              key={doc.uuid}
              className="d-flex justify-content-between align-items-center py-2"
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
              <Button
                variant="outline-danger"
                size="sm"
                onClick={() => handleRemoveDocument(doc.uuid)}
              >
                {t("common.remove", "Remove")}
              </Button>
            </ListGroup.Item>
          ))}
        </ListGroup>
      )}
    </div>
  );
};

export default DocumentSelector;
