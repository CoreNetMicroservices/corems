import { hookstate, useHookstate } from "@hookstate/core";
import { devtools } from "@hookstate/devtools";
import CoreMsApi from "@/common/utils/api/CoreMsApi";
import { USER_MS_BASE_URL } from "@/user/config";
import {
  ApiSuccessfulResponse,
  CoreMsApiResonse,
  HttpMethod,
  PageResponse,
} from "@/common/model/CoreMsApiModel";
import {
  User,
  CreateUserRequest,
  AdminChangePasswordRequest,
  ChangeEmailRequest,
} from "@/user/model/User";
import { DataTableQueryParams } from "@/common/component/dataTable/DataTableTypes";
import { buildUrlSearchParams } from "@/common/component/dataTable/DataTableState";

const userMsApi = new CoreMsApi({ baseURL: USER_MS_BASE_URL });

type UsersPagedResponse = PageResponse<User>;

interface UserManagementState {
  selectedUser?: User;
}

const initialState: UserManagementState = {
  selectedUser: undefined,
};

const userState = hookstate(initialState, devtools({ key: "userManagement" }));

export function useUserState() {
  return useHookstate(userState);
}

// Get all users with pagination - uses state parameters
export async function getAllUsers(
  queryParams: DataTableQueryParams
): Promise<CoreMsApiResonse<UsersPagedResponse>> {
  const params = buildUrlSearchParams(queryParams);

  const res = await userMsApi.apiRequest<UsersPagedResponse>(
    HttpMethod.GET,
    `/api/users?${params.toString()}`
  );

  return res;
}

// Get user by ID - matches /api/users/{userId}
export async function getUserById(
  userId: string
): Promise<CoreMsApiResonse<User>> {
  const res = await userMsApi.apiRequest<User>(
    HttpMethod.GET,
    `/api/users/${userId}`
  );

  if (res.result === true && res.response) {
    userState.selectedUser.set(res.response);
  }

  return res;
}

// Create new user - matches POST /api/users
export async function createUser(
  userData: CreateUserRequest
): Promise<CoreMsApiResonse<ApiSuccessfulResponse>> {
  const res = await userMsApi.apiRequest<ApiSuccessfulResponse>(
    HttpMethod.POST,
    `/api/users`,
    userData
  );

  return res;
}

// Update user information  - matches PUT /api/users/{userId}
export async function updateUserInfo(
  userId: string,
  userData: User
): Promise<CoreMsApiResonse<ApiSuccessfulResponse>> {
  const cleanedData = { ...userData };
  
  if (cleanedData.phoneNumber === "") {
    delete cleanedData.phoneNumber;
  }

  const res = await userMsApi.apiRequest<ApiSuccessfulResponse>(
    HttpMethod.PATCH,
    `/api/users/${userId}`,
    cleanedData
  );

  if (res.result === true) {
    getUserById(userId);
  }

  return res;
}

// Delete user  - matches DELETE /api/users/{userId}
export async function deleteUser(
  userId: string
): Promise<CoreMsApiResonse<ApiSuccessfulResponse>> {
  const res = await userMsApi.apiRequest<ApiSuccessfulResponse>(
    HttpMethod.DELETE,
    `/api/users/${userId}`
  );

  if (res.result === true) {
    // Clear selected user if it was the deleted one
    const selectedUser = userState.selectedUser.get();
    if (selectedUser?.userId === userId) {
      userState.selectedUser.set(undefined);
    }
  }

  return res;
}

// Trigger password reset flow for a user (admin) - matches POST /api/users/{userId}/reset-password
export async function triggerUserPasswordReset(
  userId: string
): Promise<CoreMsApiResonse<ApiSuccessfulResponse>> {
  const res = await userMsApi.apiRequest<ApiSuccessfulResponse>(
    HttpMethod.POST,
    `/api/users/${userId}/reset-password`
  );

  return res;
}

// Admin set/change password for a user - matches POST /api/users/{userId}/change-password
export async function adminChangeUserPassword(
  userId: string,
  passwordData: AdminChangePasswordRequest
): Promise<CoreMsApiResonse<ApiSuccessfulResponse>> {
  const res = await userMsApi.apiRequest<ApiSuccessfulResponse>(
    HttpMethod.POST,
    `/api/users/${userId}/change-password`,
    passwordData
  );

  return res;
}

// Admin change user's email (immediate) - matches POST /api/users/{userId}/change-email
export async function adminChangeUserEmail(
  userId: string,
  emailData: ChangeEmailRequest
): Promise<CoreMsApiResonse<ApiSuccessfulResponse>> {
  const res = await userMsApi.apiRequest<ApiSuccessfulResponse>(
    HttpMethod.POST,
    `/api/users/${userId}/change-email`,
    emailData
  );

  if (res.result === true) {
    getUserById(userId);
  }

  return res;
}

export function clearSelectedUser(): void {
  userState.selectedUser.set(undefined);
}
