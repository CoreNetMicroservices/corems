// Generic simple in-memory cache utility (no expiry) kept deliberately minimal.
// Provides basic get/set semantics and bulk resolution support for future entities.

export interface SimpleCache<T> {
  get: (id: string) => T | undefined;
  set: (id: string, value: T) => void;
  getMany: (ids: string[]) => Record<string, T>;
  setMany: (entries: Record<string, T>) => void;
  missing: (ids: string[]) => string[];
  clear: () => void;
}

export function createSimpleCache<T>(): SimpleCache<T> {
  const store = new Map<string, T>();

  return {
    get: (id) => store.get(id),
    set: (id, value) => {
      store.set(id, value);
    },
    getMany: (ids) => {
      const result: Record<string, T> = {};
      ids.forEach((id) => {
        const v = store.get(id);
        if (v) result[id] = v;
      });
      return result;
    },
    setMany: (entries) => {
      Object.entries(entries).forEach(([id, value]) => store.set(id, value));
    },
    missing: (ids) => ids.filter((id) => !store.has(id)),
    clear: () => store.clear(),
  };
}

export default { createSimpleCache };
