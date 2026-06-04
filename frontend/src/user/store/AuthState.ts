import { hookstate, useHookstate } from "@hookstate/core";
import { devtools } from "@hookstate/devtools";
import { jwtDecode } from "jwt-decode";

import { USER_MS_BASE_URL } from "@/user/config";
import CoreMsApi from "@/common/utils/api/CoreMsApi";
import {
  ACCESS_TOKEN_KEY,
  ApiSuccessfulResponse,
  CoreMsApiResonse,
  HttpMethod,
  REFRESH_TOKEN_KEY,
} from "@/common/model/CoreMsApiModel";
import {
  OAuth2TokenResponse,
  SignInUserRequest,
  SignUpUserRequest,
  Token,
  VerifyEmailRequest,
  VerifyPhoneRequest,
  ResendVerificationRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
} from "@/user/model/Auth";
import { User, AuthProvider } from "@/user/model/User";

const userMsApi = new CoreMsApi({ baseURL: USER_MS_BASE_URL });

interface AuthState {
  isAuthenticated: boolean;
  isInProgress: boolean;
  accessToken?: string;
  refreshToken?: string;
  parsedRefreshToken?: Token;
  user?: User;
}

const initialState: AuthState = {
  isAuthenticated: false,
  isInProgress: false,
};

const authState = hookstate(initialState, devtools({ key: "auth" }));

export function useAuthState() {
  return useHookstate(authState);
}

export async function signInUser(
  signInRequest: SignInUserRequest
): Promise<CoreMsApiResonse<OAuth2TokenResponse>> {
  authState.isInProgress.set(true);
  
  const formData = new URLSearchParams();
  formData.append('grant_type', 'password');
  formData.append('username', signInRequest.email);
  formData.append('password', signInRequest.password);
  formData.append('scope', 'openid profile email');

  const signInReponse = await userMsApi.apiRequest<OAuth2TokenResponse>(
    HttpMethod.POST,
    "/oauth2/token",
    formData.toString(),
    { 'Content-Type': 'application/x-www-form-urlencoded' }
  );
  authState.isInProgress.set(false);

  if (signInReponse.result === true) {
    const { access_token, refresh_token } = signInReponse.response;
    if (refresh_token) {
      const parsedRefreshToken = jwtDecode(refresh_token) as Token;

      authState.merge({
        isAuthenticated: true,
        accessToken: access_token,
        refreshToken: refresh_token,
        parsedRefreshToken,
      });

      setRefreshToken(refresh_token);
      setAccessToken(access_token);
    }
  }

  return signInReponse;
}

export async function signUpUser(
  signUpRequest: SignUpUserRequest
): Promise<CoreMsApiResonse<ApiSuccessfulResponse>> {
  authState.isInProgress.set(true);

  const res = await userMsApi.apiRequest<ApiSuccessfulResponse>(
    HttpMethod.POST,
    "/api/auth/signup",
    signUpRequest
  );

  authState.isInProgress.set(false);
  return res;
}

export async function verifyEmail(
  verifyRequest: VerifyEmailRequest
): Promise<CoreMsApiResonse<ApiSuccessfulResponse>> {
  return await userMsApi.apiRequest<ApiSuccessfulResponse>(
    HttpMethod.POST,
    "/api/auth/verify-email",
    verifyRequest
  );
}

export async function verifyPhone(
  verifyRequest: VerifyPhoneRequest
): Promise<CoreMsApiResonse<ApiSuccessfulResponse>> {
  return await userMsApi.apiRequest<ApiSuccessfulResponse>(
    HttpMethod.POST,
    "/api/auth/verify-phone",
    verifyRequest
  );
}

export async function resendVerification(
  resendRequest: ResendVerificationRequest
): Promise<CoreMsApiResonse<ApiSuccessfulResponse>> {
  return await userMsApi.apiRequest<ApiSuccessfulResponse>(
    HttpMethod.POST,
    "/api/auth/resend-verification",
    resendRequest
  );
}

export async function forgotPassword(
  forgotRequest: ForgotPasswordRequest
): Promise<CoreMsApiResonse<ApiSuccessfulResponse>> {
  return await userMsApi.apiRequest<ApiSuccessfulResponse>(
    HttpMethod.POST,
    "/api/auth/forgot-password",
    forgotRequest
  );
}

export async function resetPassword(
  resetRequest: ResetPasswordRequest
): Promise<CoreMsApiResonse<ApiSuccessfulResponse>> {
  return await userMsApi.apiRequest<ApiSuccessfulResponse>(
    HttpMethod.POST,
    "/api/auth/reset-password",
    resetRequest
  );
}

export async function signOut() {
  const token = authState.refreshToken.get() || authState.accessToken.get();
  
  if (token) {
    const formData = new URLSearchParams();
    formData.append('token', token);
    formData.append('token_type_hint', authState.refreshToken.get() ? 'refresh_token' : 'access_token');

    const res = await userMsApi.apiRequest<ApiSuccessfulResponse>(
      HttpMethod.POST,
      "/oauth2/revoke",
      formData.toString(),
      { 'Content-Type': 'application/x-www-form-urlencoded' }
    );

    if (res.result === false) {
      console.error("Sign out failed", res.errors);
    }
  }

  authState.merge({
    isAuthenticated: false,
    accessToken: undefined,
    refreshToken: undefined,
    parsedRefreshToken: undefined,
    user: undefined,
  });

  localStorage.removeItem(ACCESS_TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
  window.location.href = "/";
}

const setRefreshToken = (refreshToken: string | null) => {
  if (!refreshToken) return false;

  try {
    authState.parsedRefreshToken.set(jwtDecode<Token>(refreshToken));
    const currentTime = Date.now() / 1000;
    const exp = authState.parsedRefreshToken.get()?.exp || 0;
    console.debug("Refresh token parsed", exp, currentTime);
    if (exp < currentTime) {
      console.error("Refresh token expired");
      signOut();
      return false;
    }
  } catch (error) {
    console.error("Invalid refresh token", error);
    signOut();
    return false;
  }

  const tokenInfo = authState.parsedRefreshToken.get();
  if (!tokenInfo) {
    return false;
  }

  authState.merge({
    isInProgress: false,
    isAuthenticated: true,
    refreshToken,
    user: {
      userId: tokenInfo.sub,
      provider: AuthProvider.local,
      email: tokenInfo.email,
      firstName: tokenInfo.first_name,
      lastName: tokenInfo.last_name,
      roles: tokenInfo.roles,
    } as User,
  });

  localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);

  return true;
};

const setAccessToken = async (
  accessToken: string | null,
  force: boolean = false
) => {
  if (!authState.refreshToken.get()) return false;

  if (!accessToken || force) {
    const formData = new URLSearchParams();
    formData.append('grant_type', 'refresh_token');
    formData.append('refresh_token', authState.refreshToken.get() as string);

    const tokenResponse = await userMsApi.apiRequest<OAuth2TokenResponse>(
      HttpMethod.POST,
      "/oauth2/token",
      formData.toString(),
      { 'Content-Type': 'application/x-www-form-urlencoded' }
    );

    if (tokenResponse.result === true) {
      accessToken = tokenResponse.response.access_token;
      localStorage.setItem(ACCESS_TOKEN_KEY, accessToken as string);
    } else {
      console.error("Failed to refresh token", tokenResponse.errors);
      signOut();
      return false;
    }
  } else {
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
  }

  const parsedAccessToken = jwtDecode<Token>(accessToken as string);
  const expiresIn = parsedAccessToken.exp * 1000 - Date.now();

  console.debug(
    "Access token refreshed, expire in ",
    expiresIn / 60000,
    "minutes"
  );

  setTimeout(() => {
    const res = setAccessToken(accessToken, true);
    if (!res) {
      signOut();
    }
  }, Math.max(expiresIn - 30000, 0)); // Refresh 30s before expiration

  return true;
};

// Utility function for route guards to check user authentication and roles
export function getCurrentUserAuth() {
  const state = authState.get();
  return {
    isAuthenticated: state.isAuthenticated,
    user: state.user,
    roles: state.user?.roles || [],
  };
}

// Utility function to check if current user has any of the required roles
export function hasAnyRole(requiredRoles: string[]): boolean {
  const { isAuthenticated, roles } = getCurrentUserAuth();
  if (!isAuthenticated || !roles.length) {
    return false;
  }

  // SUPER_ADMIN has access to all endpoints by default
  if (roles.includes("SUPER_ADMIN")) {
    return true;
  }

  return requiredRoles.some((role) => roles.includes(role));
}

const urlParams = new URLSearchParams(window.location.search);
const token = urlParams.get("refresh_token");
if (token) {
  setRefreshToken(token);
  urlParams.delete("refresh_token");
  window.location.search = urlParams.toString();
  setAccessToken(null);
} else {
  setRefreshToken(localStorage.getItem(REFRESH_TOKEN_KEY));
  setAccessToken(null);
}
