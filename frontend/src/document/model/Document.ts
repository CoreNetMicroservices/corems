export enum Visibility {
  PUBLIC = "PUBLIC",
  PRIVATE = "PRIVATE",
  BY_LINK = "BY_LINK",
}

export enum UploadedByType {
  USER = "USER",
  SYSTEM = "SYSTEM",
}

export interface Document {
  uuid: string;
  name: string;
  originalFilename: string;
  size: number;
  extension: string;
  contentType: string;
  bucket: string;
  objectKey: string;
  visibility: Visibility;
  userId: string; // Owner user ID
  uploadedById: string;
  uploadedByType: UploadedByType;
  createdAt: string;
  updatedAt: string;
  checksum: string;
  description?: string;
  tags?: string; // Comma-separated string
  viewUrl?: string; // Full URL to view the document inline
  downloadUrl?: string; // Full URL to download the document
  deleted: boolean;
  deletedAt?: string;
  deletedBy?: string;
}

export interface DocumentUploadMetadata {
  ownerUserId?: string;
  visibility?: Visibility;
  description?: string;
  tags?: string[]; // Array in frontend, will be converted to comma-separated string for API
  confirmReplace?: boolean;
}

export interface UploadBase64Request extends DocumentUploadMetadata {
  name: string;
  base64Data: string;
  contentType?: string;
}

export interface DocumentUpdateRequest {
  name?: string;
  description?: string;
  tags?: string;
  visibility?: Visibility;
}

export interface GenerateLinkRequest {
  expiresInHours?: number;
}

export interface LinkResponse {
  token: string;
  url: string;
  expiresAt: string;
}
