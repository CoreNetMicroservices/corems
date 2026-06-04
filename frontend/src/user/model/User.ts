export enum AuthProvider {
  local = "local",
  google = "google",
  github = "github",
  linkedin = "linkedin",
}

// Unified User interface - supports both frontend state and backend API contract
export interface User {
  userId: string;
  provider: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  imageUrl?: string;
  emailVerified?: boolean;
  phoneVerified?: boolean;
  roles?: string[];
  createdAt?: string;
  updatedAt?: string;
  lastLoginAt?: string;
}

export interface CreateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  imageUrl?: string;
  phoneNumber?: string;
}

export interface ProfileChangePasswordRequest {
  newPassword: string;
  confirmPassword: string;
}

export interface AdminChangePasswordRequest {
  newPassword: string;
  confirmPassword: string;
}

export interface ChangeEmailRequest {
  newEmail: string;
}
