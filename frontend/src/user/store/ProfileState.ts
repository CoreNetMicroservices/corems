import { hookstate, useHookstate } from "@hookstate/core";
import { devtools } from "@hookstate/devtools";
import { User } from "@/user/model/User";
import CoreMsApi from "@/common/utils/api/CoreMsApi";
import { USER_MS_BASE_URL } from "@/user/config";
import {
  ApiSuccessfulResponse,
  CoreMsApiResonse,
  HttpMethod,
} from "@/common/model/CoreMsApiModel";
import { OidcUserInfo } from "@/user/model/Auth";

const userMsApi = new CoreMsApi({ baseURL: USER_MS_BASE_URL });

interface ProfileState {
  user?: User;
  isInProgress: boolean;
}

const initialState: ProfileState = {
  user: undefined,
  isInProgress: false,
};

const profileState = hookstate(initialState, devtools({ key: "profile" }));

export function useProfileState() {
  return useHookstate(profileState);
}

export async function getProfileInfo(): Promise<CoreMsApiResonse<User>> {
  const user = profileState.user.get();
  if (user) {
    return {
      result: true,
      response: user as User,
      errors: [],
    };
  }

  const isInProgress = profileState.isInProgress.get();
  if (isInProgress) {
    return {
      result: false,
      response: {} as User,
      errors: [
        {
          reasonCode: "request.in.progress",
          description: "Request already in progress",
        },
      ],
    };
  }

  profileState.isInProgress.set(true);
  const res = await userMsApi.apiRequest<OidcUserInfo>(HttpMethod.GET, "/oauth2/userinfo");
  profileState.isInProgress.set(false);

  if (res.result === true && res.response) {
    const oidcUser = res.response;
    const user: User = {
      userId: oidcUser.sub,
      email: oidcUser.email || '',
      emailVerified: oidcUser.email_verified || false,
      firstName: oidcUser.given_name || '',
      lastName: oidcUser.family_name || '',
      phoneNumber: oidcUser.phone_number,
      phoneVerified: oidcUser.phone_number_verified || false,
      imageUrl: oidcUser.picture,
      roles: oidcUser.roles || [],
      provider: (oidcUser.provider || 'local') as User['provider'],
    };
    profileState.user.set(user);
    return {
      result: true,
      response: user,
      errors: [],
    };
  }
  return {
    result: false,
    response: {} as User,
    errors: res.errors,
  };
}

export async function updateProfileInfo(
  userData: Partial<User>
): Promise<CoreMsApiResonse<User>> {
  const cleanedData: Record<string, string | undefined> = {};
  
  if (userData.firstName !== undefined) cleanedData.firstName = userData.firstName;
  if (userData.lastName !== undefined) cleanedData.lastName = userData.lastName;
  if (userData.phoneNumber !== undefined && userData.phoneNumber !== "") {
    cleanedData.phoneNumber = userData.phoneNumber;
  }
  if (userData.imageUrl !== undefined) cleanedData.imageUrl = userData.imageUrl;

  profileState.isInProgress.set(true);
  const res = await userMsApi.apiRequest<OidcUserInfo>(
    HttpMethod.PATCH,
    "/api/profile",
    cleanedData
  );
  profileState.isInProgress.set(false);

  if (res.result === true && res.response) {
    const oidcUser = res.response;
    const user: User = {
      userId: oidcUser.sub,
      email: oidcUser.email || '',
      emailVerified: oidcUser.email_verified || false,
      firstName: oidcUser.given_name || '',
      lastName: oidcUser.family_name || '',
      phoneNumber: oidcUser.phone_number,
      phoneVerified: oidcUser.phone_number_verified || false,
      imageUrl: oidcUser.picture,
      roles: oidcUser.roles || [],
      provider: (oidcUser.provider || 'local') as User['provider'],
    };
    profileState.user.set(user);
    return {
      result: true,
      response: user,
      errors: [],
    };
  }
  return {
    result: false,
    response: {} as User,
    errors: res.errors,
  };
}

export async function updateProfilePassword(passwordData: {
  oldPassword: string;
  newPassword: string;
  confirmPassword: string;
}): Promise<CoreMsApiResonse<ApiSuccessfulResponse>> {
  profileState.isInProgress.set(true);

  const res = await userMsApi.apiRequest<ApiSuccessfulResponse>(
    HttpMethod.POST,
    "/api/profile/change-password",
    passwordData
  );
  profileState.isInProgress.set(false);
  return res;
}
