export enum TemplateCategory {
  COMMON = "COMMON",
  EMAIL = "EMAIL",
  SMS = "SMS",
  DOCUMENT = "DOCUMENT",
}

export interface TemplateParamDefinition {
  required?: boolean;
  type?: "string" | "number" | "boolean" | "object" | "array";
  pattern?: string;
}

export interface Template {
  id: string;
  templateId: string;
  name: string;
  description?: string;
  content: string;
  category: TemplateCategory;
  language: string;
  paramSchema?: Record<string, TemplateParamDefinition>;
  createdAt: string;
  updatedAt: string;
  createdBy: string;
  updatedBy: string;
}

export interface CreateTemplateRequest {
  templateId: string;
  name: string;
  description?: string;
  content: string;
  category: TemplateCategory;
  language?: string;
  paramSchema?: Record<string, TemplateParamDefinition>;
}

export interface UpdateTemplateRequest {
  templateId?: string;
  name?: string;
  description?: string;
  content?: string;
  category?: TemplateCategory;
  language?: string;
  paramSchema?: Record<string, TemplateParamDefinition>;
}
