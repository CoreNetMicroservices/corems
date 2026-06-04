import CoreMsApi from "@/common/utils/api/CoreMsApi";
import { USER_MS_BASE_URL } from "@/user/config";
import { HttpMethod, PageResponse } from "@/common/model/CoreMsApiModel";
import { createSimpleCache } from "@/common/utils/cache/SimpleCache";
import { User } from "@/user/model/User";

const userMsApi = new CoreMsApi({ baseURL: USER_MS_BASE_URL });

// Reusable user cache (names or full user objects). No TTL for prototype.
const userCache = createSimpleCache<User>();
const BULK_FETCH_LIMIT = 25;

export const searchUsers = async (search: string): Promise<User[]> => {
  try {
    const params = new URLSearchParams({
      page: "1",
      pageSize: "10",
      search: search,
    });

    const res = await userMsApi.apiRequest<PageResponse<User>>(
      HttpMethod.GET,
      `/api/users?${params.toString()}`
    );

    if (res.result && res.response) {
      return res.response.items;
    }
    return [];
  } catch (error) {
    console.error("Failed to fetch users", error);
    return [];
  }
};

// Bulk fetch users by IDs (limited) and populate cache
export const fetchUsersByIds = async (ids: string[]): Promise<User[]> => {
  const missing = userCache.missing(ids).slice(0, BULK_FETCH_LIMIT);
  if (missing.length === 0) {
    return ids.map((id) => userCache.get(id)).filter(Boolean) as User[];
  }
  // Build bulk 'in' filter using correct param key 'filter'
  const filterParam = encodeURIComponent(`userId:in:${missing.join(",")}`);
  const bulkUrl = `/api/users?filter=${filterParam}&page=1&pageSize=${missing.length}`;
  const res = await userMsApi.apiRequest<PageResponse<User>>(
    HttpMethod.GET,
    bulkUrl
  );
  if (res.result && res.response?.items) {
    const map: Record<string, User> = {};
    res.response.items.forEach((u) => {
      map[u.userId] = u;
    });
    userCache.setMany(map);
  }
  // Fallback: individually fetch any still-missing ids via single filter queries
  const stillMissing = userCache.missing(missing);
  if (stillMissing.length > 0) {
    for (const id of stillMissing) {
      const singleFilter = encodeURIComponent(`userId:${id}`);
      const singleUrl = `/api/users?filter=${singleFilter}&page=1&pageSize=1`;
      const singleRes = await userMsApi.apiRequest<PageResponse<User>>(
        HttpMethod.GET,
        singleUrl
      );
      if (singleRes.result && singleRes.response?.items?.length === 1) {
        userCache.set(id, singleRes.response.items[0]);
      }
    }
  }
  return ids.map((id) => userCache.get(id)).filter(Boolean) as User[];
};

// Resolve user names with caching; returns id->displayName map
export const resolveUserNames = async (
  ids: string[]
): Promise<Record<string, string>> => {
  const unique = Array.from(new Set(ids.filter(Boolean)));
  if (unique.length === 0) return {};
  await fetchUsersByIds(unique);
  const result: Record<string, string> = {};
  unique.forEach((id) => {
    const user = userCache.get(id);
    if (user) result[id] = `${user.firstName} ${user.lastName}`.trim();
  });
  return result;
};

export const clearUserCache = () => userCache.clear();
