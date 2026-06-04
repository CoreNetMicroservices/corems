import React, { useState, useEffect, useRef } from 'react';
import { Container, Row, Col, Form, Button, Alert, Card, ButtonGroup } from 'react-bootstrap';
import { useParams, useNavigate } from 'react-router-dom';
import { Save, Trash } from 'react-bootstrap-icons';
import { useTranslation } from 'react-i18next';
import AceEditor from 'react-ace';
import 'ace-builds/src-noconflict/mode-html';
import 'ace-builds/src-noconflict/theme-github';
import 'ace-builds/src-noconflict/theme-dracula';
import 'ace-builds/src-noconflict/ext-language_tools';
import { useTemplateState } from '@/template/store/TemplateState';
import { useMessageState } from '@/common/utils/api/ApiResponseHandler';
import { AlertMessage } from '@/common/component/ApiResponseAlert';
import { APP_ROUTES } from '@/app/router/routes';
import Breadcrumb from '@/common/component/Breadcrumb';
import { useTheme } from '@/app/layout/useTheme';
import { TemplateCategory, TemplateParamDefinition } from '@/template/model/Template';

type ViewMode = 'content' | 'preview';

export const TemplateEditor: React.FC = () => {
  const { t } = useTranslation();
  const { theme } = useTheme();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { getTemplate, createTemplate, updateTemplate, deleteTemplate } = useTemplateState();
  const { success, errors, handleResponse } = useMessageState();
  const iframeRef = useRef<HTMLIFrameElement>(null);

  const [viewMode, setViewMode] = useState<ViewMode>('content');
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

  const [formData, setFormData] = useState({
    templateId: '',
    name: '',
    description: '',
    content: '',
    category: TemplateCategory.COMMON,
    language: 'en',
  });

  const [paramSchema, setParamSchema] = useState<Record<string, TemplateParamDefinition>>({});

  const isNewTemplate = !id;

  // Extract Handlebars variables from content
  const extractVariables = (content: string): string[] => {
    const regex = /\{\{([^}#/]+)\}\}/g;
    const variables = new Set<string>();
    let match;
    
    while ((match = regex.exec(content)) !== null) {
      const variable = match[1].trim();
      if (!variable.startsWith('#') && !variable.startsWith('/')) {
        variables.add(variable);
      }
    }
    
    return Array.from(variables);
  };

  // Update schema when content changes (debounced)
  useEffect(() => {
    const timeoutId = setTimeout(() => {
      if (formData.content) {
        const variables = extractVariables(formData.content);
        const updatedSchema: Record<string, TemplateParamDefinition> = {};
        
        // Only keep parameters that still exist in the template
        variables.forEach(variable => {
          if (paramSchema[variable]) {
            // Preserve existing configuration
            updatedSchema[variable] = paramSchema[variable];
          } else {
            // Add new parameter with defaults
            updatedSchema[variable] = {
              required: false,
              type: 'string'
            };
          }
        });
        
        setParamSchema(updatedSchema);
      } else {
        // Clear schema if content is empty
        setParamSchema({});
      }
    }, 1000);

    return () => clearTimeout(timeoutId);
  }, [formData.content]);

  // Update preview iframe
  useEffect(() => {
    if (viewMode === 'preview' && iframeRef.current) {
      const iframeDoc = iframeRef.current.contentDocument;
      if (iframeDoc) {
        iframeDoc.open();
        iframeDoc.write(formData.content || '<p>No content to preview</p>');
        iframeDoc.close();
      }
    }
  }, [viewMode, formData.content]);

  useEffect(() => {
    if (id) {
      loadTemplate();
    }
  }, [id]);

  const loadTemplate = async () => {
    if (!id) return;

    setIsLoading(true);
    const result = await getTemplate(id);
    setIsLoading(false);

    if (result.result && result.response) {
      const template = result.response;
      setFormData({
        templateId: template.templateId,
        name: template.name,
        description: template.description || '',
        content: template.content,
        category: template.category,
        language: template.language,
      });
      const schema = template.paramSchema || {};
      setParamSchema(schema);
    }
  };

  const handleSave = async () => {
    if (!formData.templateId || !formData.name || !formData.content) {
      handleResponse(
        { result: false, response: null, errors: [{ reasonCode: 'VALIDATION', description: t('template.requiredFields', 'Template ID, name, and content are required') }] },
        t('template.requiredFields', 'Template ID, name, and content are required')
      );
      return;
    }

    setIsSaving(true);

    if (isNewTemplate) {
      const result = await createTemplate({
        templateId: formData.templateId,
        name: formData.name,
        description: formData.description || undefined,
        content: formData.content,
        category: formData.category,
        language: formData.language,
        paramSchema: Object.keys(paramSchema).length > 0 ? paramSchema : undefined,
      });

      setIsSaving(false);

      handleResponse(
        result,
        t('template.createFailed', 'Failed to create template'),
        t('template.createSuccess', 'Template created successfully')
      );

      if (result.result && result.response) {
        setTimeout(() => navigate(APP_ROUTES.TEMPLATES), 1500);
      }
    } else {
      const result = await updateTemplate(id!, {
        templateId: formData.templateId,
        name: formData.name,
        description: formData.description || undefined,
        content: formData.content,
        category: formData.category,
        language: formData.language,
        paramSchema: Object.keys(paramSchema).length > 0 ? paramSchema : undefined,
      });

      setIsSaving(false);

      handleResponse(
        result,
        t('template.updateFailed', 'Failed to update template'),
        t('template.updateSuccess', 'Template updated successfully')
      );

      if (result.result) {
        loadTemplate();
      }
    }
  };

  const handleDelete = async () => {
    if (!id) return;

    setIsSaving(true);
    const result = await deleteTemplate(id);
    setIsSaving(false);

    handleResponse(
      result,
      t('template.deleteFailed', 'Failed to delete template'),
      t('template.deleteSuccess', 'Template deleted successfully')
    );

    if (result.result) {
      setTimeout(() => navigate(APP_ROUTES.TEMPLATES), 1500);
    }
  };

  if (isLoading) {
    return (
      <Container className="mt-4">
        <div className="text-center py-5">{t('template.loadingTemplate', 'Loading template...')}</div>
      </Container>
    );
  }

  return (
    <Container className="mt-4">
      <Breadcrumb
        items={[
          { label: t('nav.templates', 'Templates'), href: APP_ROUTES.TEMPLATES },
          { label: isNewTemplate ? t('template.newTemplate', 'New Template') : `${formData.templateId} (${formData.language})`, active: true }
        ]}
      />

      <Row className="mb-4">
        <Col>
          <div className="d-flex justify-content-between align-items-center">
            <h3>
              {isNewTemplate
                ? t('template.newTemplate', 'New Template')
                : t('template.editTitle', 'Edit {{templateId}} ({{language}})', { templateId: formData.templateId, language: formData.language })}
            </h3>
          </div>
        </Col>
      </Row>

      <Card className="mb-3">
        <Card.Body>
          <Row className="mb-3">
            <Col md={6}>
              <Form.Label>{t('template.templateId', 'Template ID')}</Form.Label>
              <Form.Control
                value={formData.templateId}
                onChange={(e) => setFormData({ ...formData, templateId: e.target.value })}
                placeholder={t('template.templateIdPlaceholder', 'e.g., welcome-email')}
              />
              <Form.Text className="text-muted">
                {t('template.templateIdHint', 'Unique identifier (kebab-case)')}
              </Form.Text>
            </Col>
            <Col md={6}>
              <Form.Label>{t('template.name', 'Name')}</Form.Label>
              <Form.Control
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                placeholder={t('template.namePlaceholder', 'e.g., Welcome Email Template')}
              />
            </Col>
          </Row>

          <Row className="mb-3">
            <Col md={4}>
              <Form.Label>{t('template.category', 'Category')}</Form.Label>
              <Form.Select
                value={formData.category}
                onChange={(e) => setFormData({ ...formData, category: e.target.value as TemplateCategory })}
              >
                {Object.values(TemplateCategory).map(cat => (
                  <option key={cat} value={cat}>{cat}</option>
                ))}
              </Form.Select>
            </Col>
            <Col md={4}>
              <Form.Label>{t('template.language', 'Language')}</Form.Label>
              <Form.Control
                value={formData.language}
                onChange={(e) => setFormData({ ...formData, language: e.target.value })}
                placeholder={t('template.languagePlaceholder', 'e.g., en, no')}
              />
            </Col>
          </Row>

          <Row className="mb-3">
            <Col>
              <Form.Label>{t('template.description', 'Description')}</Form.Label>
              <Form.Control
                as="textarea"
                rows={2}
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                placeholder={t('template.descriptionPlaceholder', 'Optional description')}
              />
            </Col>
          </Row>

          <div className="d-flex justify-content-between align-items-center mb-2">
            <Form.Label className="mb-0">
              {t('template.content', 'Template Content')}
            </Form.Label>
            <ButtonGroup size="sm">
              <Button
                variant={viewMode === 'content' ? 'primary' : 'outline-primary'}
                onClick={() => setViewMode('content')}
              >
                {t('template.contentView', 'Content')}
              </Button>
              <Button
                variant={viewMode === 'preview' ? 'primary' : 'outline-primary'}
                onClick={() => setViewMode('preview')}
              >
                {t('template.previewView', 'Preview')}
              </Button>
            </ButtonGroup>
          </div>

          {viewMode === 'content' ? (
            <>
              <AceEditor
                mode="html"
                theme={theme === 'dark' ? 'dracula' : 'github'}
                value={formData.content}
                onChange={(value) => setFormData({ ...formData, content: value })}
                name="content-editor"
                editorProps={{ $blockScrolling: true }}
                setOptions={{
                  useWorker: false,
                  showLineNumbers: true,
                  tabSize: 2,
                  fontSize: 18,
                }}
                style={{
                  width: '100%',
                  height: '400px',
                  borderRadius: '4px',
                  border: '1px solid var(--bs-border-color)'
                }}
              />
              <Alert variant="info" className="mt-2">
                <small>{t('template.contentHint', 'Use Handlebars syntax: {{variable}}, {{#if condition}}, {{#each items}}')}</small>
              </Alert>
            </>
          ) : (
            <>
              <iframe
                ref={iframeRef}
                title="Template Preview"
                style={{
                  width: '100%',
                  height: '400px',
                  border: '1px solid var(--bs-border-color)',
                  borderRadius: '4px',
                  backgroundColor: 'white'
                }}
              />
              <Alert variant="info" className="mt-2">
                <small>{t('template.previewHint', 'Preview shows the raw HTML. Variables like {{variable}} will not be replaced.')}</small>
              </Alert>
            </>
          )}
        </Card.Body>
      </Card>

      <Card className="mb-3">
        <Card.Header>
          <span className="fw-bold">{t('template.parameterSchema', 'Parameter Schema (Optional)')}</span>
        </Card.Header>
        <Card.Body>
          <Alert variant="secondary" className="mb-3">
            <small>
              {t('template.schemaAutoGenerated', 'Schema is automatically generated from variables in your template content. You can customize types and validation rules below.')}
            </small>
          </Alert>
          
          {Object.keys(paramSchema).length === 0 ? (
            <Alert variant="info">
              {t('template.noParametersDetected', 'No parameters detected. Add variables like {{userName}} to your template content.')}
            </Alert>
          ) : (
            <div>
              {Object.entries(paramSchema).map(([paramName, paramDef]) => (
                <Row key={paramName} className="mb-3 align-items-center border-bottom pb-3">
                  <Col md={3}>
                    <Form.Label className="mb-0 fw-bold">
                      <code>{`{{${paramName}}}`}</code>
                    </Form.Label>
                  </Col>
                  <Col md={3}>
                    <Form.Select
                      size="sm"
                      value={paramDef.type || 'string'}
                      onChange={(e) => {
                        const updated = { ...paramSchema };
                        updated[paramName] = { ...updated[paramName], type: e.target.value as 'string' | 'number' | 'boolean' | 'object' | 'array' };
                        setParamSchema(updated);
                      }}
                    >
                      <option value="string">{t('template.typeString', 'String')}</option>
                      <option value="number">{t('template.typeNumber', 'Number')}</option>
                      <option value="boolean">{t('template.typeBoolean', 'Boolean')}</option>
                      <option value="object">{t('template.typeObject', 'Object')}</option>
                      <option value="array">{t('template.typeArray', 'Array')}</option>
                    </Form.Select>
                  </Col>
                  <Col md={2}>
                    <Form.Check
                      type="checkbox"
                      label={t('template.required', 'Required')}
                      checked={paramDef.required || false}
                      onChange={(e) => {
                        const updated = { ...paramSchema };
                        updated[paramName] = { ...updated[paramName], required: e.target.checked };
                        setParamSchema(updated);
                      }}
                    />
                  </Col>
                  <Col md={4}>
                    <Form.Control
                      size="sm"
                      type="text"
                      placeholder={t('template.patternPlaceholder', 'Regex pattern (optional)')}
                      value={paramDef.pattern || ''}
                      onChange={(e) => {
                        const updated = { ...paramSchema };
                        updated[paramName] = { ...updated[paramName], pattern: e.target.value || undefined };
                        setParamSchema(updated);
                      }}
                    />
                  </Col>
                </Row>
              ))}
            </div>
          )}
        </Card.Body>
      </Card>

      <AlertMessage success={success} errors={errors} />

      {showDeleteConfirm && (
        <Alert variant="danger" className="mb-3">
            <Alert.Heading>{t('template.confirmDeletion', 'Confirm Deletion')}</Alert.Heading>
            <p>{t('template.confirmDeleteMessage', 'Are you sure you want to delete this template?')}</p>
            <div className="d-flex gap-2">
              <Button variant="danger" size="sm" onClick={handleDelete}>{t('template.yesDelete', 'Yes, Delete')}</Button>
              <Button variant="outline-secondary" size="sm" onClick={() => setShowDeleteConfirm(false)}>{t('common.cancel', 'Cancel')}</Button>
          </div>
        </Alert>
      )}

      <div className="d-flex justify-content-end gap-2 mb-4">
        {!isNewTemplate && (
          <Button
            variant="outline-danger"
            onClick={(e) => {
              e.preventDefault();
              setShowDeleteConfirm(true);
            }}
            disabled={isSaving}
          >
            <Trash className="me-1" /> {t('common.delete', 'Delete')}
          </Button>
        )}
        <Button
          variant="primary"
          onClick={(e) => {
            e.preventDefault();
            handleSave();
          }}
          disabled={isSaving}
        >
          <Save className="me-1" /> {isSaving ? t('common.saving', 'Saving...') : t('common.save', 'Save')}
        </Button>
      </div>
    </Container>
  );
};

export default TemplateEditor;
