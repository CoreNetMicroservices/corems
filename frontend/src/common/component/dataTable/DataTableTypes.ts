export type FilterOperator =
  | "eq"
  | "ne"
  | "like"
  | "in"
  | "contains"
  | "gt"
  | "lt"
  | "gte"
  | "lte";

export interface DataTableColumn {
  key: string;
  title: string;
  sortable?: boolean;
  width?: string;
}

export interface DataTableFilter<T = unknown> {
  key: string;
  label: string;
  type: "select" | "text" | "date" | "async-select";
  operator?: FilterOperator;
  options?: { value: string | number; label: string }[];
  placeholder?: string;
  loadOptions?: (search: string) => Promise<T[]>;
  getOptionLabel?: (option: T) => string;
  getOptionValue?: (option: T) => string | number;
  getOptionSubtitle?: (option: T) => string;
}

export interface DataTablePagination {
  page: number;
  pageSize: number;
  totalElements: number;
  totalPages: number;
}

export interface DataTableQueryParams {
  page: number;
  pageSize: number;
  search: string;
  sort?: string;
  filters?: Record<string, string | number | boolean>;
  filterOperators?: Record<string, FilterOperator>;
}

export interface DataTableProps<T> {
  // Data
  items: T[];
  pagination?: DataTablePagination;
  isLoading: boolean;

  // Configuration
  columns: DataTableColumn[];
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  filters?: DataTableFilter<any>[];
  filterValues?: Record<string, string | number | boolean>;
  sortableFields?: string[];
  currentSort?: { field: string; direction: "asc" | "desc" };
  searchPlaceholder?: string;
  pageSizeOptions?: number[];

  // Callbacks
  onSearch?: (search: string) => void;
  onFilter?: (
    key: string,
    value: string | number | boolean | null,
    operator?: FilterOperator
  ) => void;
  onPageChange?: (page: number) => void;
  onPageSizeChange?: (pageSize: number) => void;
  onSort?: (field: string, direction: "asc" | "desc") => void;

  // Render functions
  renderRow: (item: T, index: number) => React.ReactNode;

  // Optional customization
  title?: string;
  actions?: React.ReactNode;
}
