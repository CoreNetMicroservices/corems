import React, { useState, useEffect } from "react";
import { Form, Badge, Spinner, Row, Col } from "react-bootstrap";
import { useTranslation } from "react-i18next";
import { AsyncSelect } from "@/common/component/dataTable/filter/AsyncSelect";
import { searchTemplates, getTemplateMetadata } from "@/template/utils/TemplateApi";
import type { Template, TemplateParamDefinition } from "@/template/model/Template";
import type { TemplateMetadata } from "@/template/utils/TemplateApi";

interface TemplateSelectorProps {
  category?: string;
  selectedTemplateId?: string;
  templateParams: Record<string, string>;
  onTemplateChange: (templateId: string | undefined) => void;
  onParamsChange: (params: Record<string, string>) => void;
}

export const TemplateSelector: React.FC<TemplateSelectorProps> = ({
  category,
  selectedTemplateId,
  templateParams,
  onTemplateChange,
  onParamsChange,
}) => {
  const { t } = useTranslation();
  const [lastTemplateOptions, setLastTemplateOptions] = useState<Template[]>([]);
  const [metadata, setMetadata] = useState<TemplateMetadata | null>(null);
  const [loadingMetadata, setLoadingMetadata] = useState(false);

  useEffect(() => {
    if (!selectedTemplateId) {
      setMetadata(null);
      return;
    }

    let mounted = true;
    setLoadingMetadata(true);

    getTemplateMetadata(selectedTemplateId)
      .then((result) => {
        if (mounted) {
          setMetadata(result);
          // Initialize empty params for required fields
          if (result?.requiredParameters) {
            const newParams = { ...templateParams };
            let changed = false;
            for (const param of result.requiredParameters) {
              if (!(param in newParams)) {
                newParams[param] = "";
                changed = true;
              }
            }
            if (changed) onParamsChange(newParams);
          }
        }
      })
      .catch(() => {
        if (mounted) setMetadata(null);
      })
      .finally(() => {
        if (mounted) setLoadingMetadata(false);
      });

    return () => {
      mounted = false;
    };
  }, [selectedTemplateId]);

  const handleTemplateSelect = (value: string | number | undefined) => {
    if (!value || typeof value !== "string") {
      onTemplateChange(undefined);
      onParamsChange({});
      return;
    }

    const selected = lastTemplateOptions.find((t) => t.id === value);
    if (selected) {
      onTemplateChange(selected.templateId);
    }
  };

  const handleParamChange = (paramName: string, value: string) => {
    onParamsChange({ ...templateParams, [paramName]: value });
  };

  const getParamType = (paramDef?: TemplateParamDefinition): string => {
    return paramDef?.type || "string";
  };

  return (
    <div>
      <Form.Label>{t("template.selectTemplate", "Template")}</Form.Label>
      <AsyncSelect<Template>
        key={category || 'all'}
        value={
          selectedTemplateId
            ? lastTemplateOptions.find((t) => t.templateId === selectedTemplateId)?.id
            : undefined
        }
        onChange={handleTemplateSelect}
        loadOptions={async (term) => {
          const res = await searchTemplates(term, category);
          setLastTemplateOptions(res);
          return res;
        }}
        getOptionLabel={(tpl) => tpl.name}
        getOptionValue={(tpl) => tpl.id}
        getOptionSubtitle={(tpl) =>
          `${tpl.category} • ${tpl.language.toUpperCase()}`
        }
        placeholder={t("template.searchTemplates", "Search templates...")}
      />

      {selectedTemplateId && loadingMetadata && (
        <div className="mt-2 text-center">
          <Spinner animation="border" size="sm" />
          <span className="ms-2 text-muted">
            {t("template.loadingParams", "Loading parameters...")}
          </span>
        </div>
      )}

      {selectedTemplateId && metadata && !loadingMetadata && (
        <div className="mt-3">
          {metadata.description && (
            <p className="text-muted small mb-2">{metadata.description}</p>
          )}

          {metadata.requiredParameters.length > 0 && (
            <>
              <Form.Label className="small fw-semibold">
                {t("template.parameters", "Parameters")}
              </Form.Label>
              <Row>
                {Object.entries(metadata.paramSchema || {}).map(
                  ([paramName, paramDef]) => {
                    const isRequired =
                      metadata.requiredParameters.includes(paramName);
                    const paramDefinition = paramDef as TemplateParamDefinition;
                    const type = getParamType(paramDefinition);

                    return (
                      <Col md={6} key={paramName} className="mb-2">
                        <Form.Label className="small">
                          {paramName}
                          {isRequired && (
                            <Badge bg="danger" className="ms-1" pill>
                              *
                            </Badge>
                          )}
                          {type !== "string" && (
                            <Badge bg="info" className="ms-1" pill>
                              {type}
                            </Badge>
                          )}
                        </Form.Label>
                        <Form.Control
                          size="sm"
                          type={type === "number" ? "number" : "text"}
                          value={templateParams[paramName] || ""}
                          onChange={(e) =>
                            handleParamChange(paramName, e.target.value)
                          }
                          placeholder={paramName}
                        />
                      </Col>
                    );
                  }
                )}
              </Row>
            </>
          )}

          {metadata.requiredParameters.length === 0 && (
            <p className="text-muted small">
              {t(
                "template.noParamsRequired",
                "This template has no parameters."
              )}
            </p>
          )}
        </div>
      )}
    </div>
  );
};

export default TemplateSelector;
