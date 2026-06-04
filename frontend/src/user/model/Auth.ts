export interface SignInUserRequest {
  email: string;
  password: string;
}

export interface SignUpUserRequest {
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  confirmPassword: string;
  phoneNumber?: string;
  imageUrl?: string;
}

export interface OAuth2TokenResponse {
  access_token: string;
  token_type: 'Bearer';
  expires_in: number;
  refresh_token?: string;
  id_token?: string;
  scope?: string;
}

export interface OidcUserInfo {
  sub: string;
  email?: string;
  email_verified?: boolean;
  phone_number?: string;
  phone_number_verified?: boolean;
  given_name?: string;
  family_name?: string;
  picture?: string;
  updated_at?: number;
  roles?: string[];
  provider?: string;
}

export interface Token {
  sub: string; // User ID here
  email: string;
  user_uuid: string;
  first_name: string;
  last_name: string;
  roles: string[];
  exp: number;
  iat: number;
}

export interface VerifyEmailRequest {
  email: string;
  token: string;
}

export interface VerifyPhoneRequest {
  phoneNumber: string;
  code: string;
}

export interface ResendVerificationRequest {
  email: string;
  type: 'EMAIL' | 'SMS';
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
  confirmPassword: string;
}
