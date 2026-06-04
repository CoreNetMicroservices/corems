import { State } from "@hookstate/core";
import { DataTableQueryParams, FilterOperator } from "./DataTableTypes";

export const getInitialDataTableQueryParams = (): DataTableQueryParams => ({
  page: 1,
  pageSize: 20,
  search: "",
  sort: undefined,
  filters: undefined,
  filterOperators: undefined,
});

export function buildUrlSearchParams(
  queryParams: DataTableQueryParams
): URLSearchParams {
  const params = new URLSearchParams({
    page: queryParams.page.toString(),
    pageSize: queryParams.pageSize.toString(),
  });

  if (queryParams.sort) params.append("sort", queryParams.sort);
  if (queryParams.search) params.append("search", queryParams.search);
  if (queryParams.filters) {
    Object.entries(queryParams.filters).forEach(([key, value]) => {
      const operator = queryParams.filterOperators?.[key];
      const filterValue = operator
        ? `${key}:${operator}:${value.toString()}`
        : `${key}:${value.toString()}`;
      params.append("filter", filterValue);
    });
  }
  return params;
}

export function parseCurrentSort(sort?: string) {
  if (!sort) return undefined;
  const [field, direction] = sort.split(":");
  return { field, direction: direction as "asc" | "desc" };
}

export interface DataTableActionOptions {
  onUpdate?: () => void;
  fieldMapper?: (field: string) => string;
}

export function createDataTableActions(
  state: State<DataTableQueryParams>,
  options?: DataTableActionOptions
) {
  const triggerUpdate = () => {
    if (options?.onUpdate) options.onUpdate();
  };

  return {
    setPage: (page: number) => {
      state.page.set(page);
      triggerUpdate();
    },
    setPageSize: (pageSize: number) => {
      state.pageSize.set(pageSize);
      state.page.set(1);
      triggerUpdate();
    },
    setSearch: (search: string) => {
      state.search.set(search);
      state.page.set(1);
      triggerUpdate();
    },
    setSort: (field: string, direction: "asc" | "desc") => {
      const mappedField = options?.fieldMapper
        ? options.fieldMapper(field)
        : field;
      state.sort.set(`${mappedField}:${direction}`);
      triggerUpdate();
    },
    setFilter: (
      key: string,
      value: string | number | boolean | null,
      operator?: FilterOperator
    ) => {
      const currentFilters = state.filters.get() || {};
      const currentOperators = state.filterOperators.get() || {};

      if (value === null || value === "" || value === undefined) {
        const remainingFilters = { ...currentFilters };
        delete remainingFilters[key];
        state.filters.set(
          Object.keys(remainingFilters).length > 0
            ? remainingFilters
            : undefined
        );

        const remainingOperators = { ...currentOperators };
        delete remainingOperators[key];
        state.filterOperators.set(
          Object.keys(remainingOperators).length > 0
            ? remainingOperators
            : undefined
        );
      } else {
        state.filters.set({ ...currentFilters, [key]: value });
        if (operator) {
          state.filterOperators.set({ ...currentOperators, [key]: operator });
        }
      }
      state.page.set(1);
      triggerUpdate();
    },
    clearFilters: () => {
      state.filters.set(undefined);
      state.filterOperators.set(undefined);
      state.page.set(1);
      triggerUpdate();
    },
    setFilters: (filters?: Record<string, string | number | boolean>) => {
      state.filters.set(filters);
      state.page.set(1);
      triggerUpdate();
    },
    reset: () => {
      state.set(getInitialDataTableQueryParams());
    },
  };
}

export function useDataTableState(state: State<DataTableQueryParams>) {
  return createDataTableActions(state);
}
