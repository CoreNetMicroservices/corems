import React from "react";
import { Form } from "react-bootstrap";
import { DataTableFilter, FilterOperator } from "../DataTableTypes";
import { AsyncSelect } from "./AsyncSelect";

interface DataTableFiltersProps {
  filters: DataTableFilter<unknown>[];
  values: Record<string, string | number | boolean>;
  onFilter: (
    key: string,
    value: string | number | boolean | null,
    operator?: FilterOperator
  ) => void;
}

export const DataTableFilters: React.FC<DataTableFiltersProps> = ({
  filters,
  values,
  onFilter
}) => {
  const renderFilter = (filter: DataTableFilter<unknown>) => {
    const currentValue = values[filter.key];

    switch (filter.type) {
      case 'select':
        return (
          <Form.Select
            key={filter.key}
            value={currentValue?.toString() || ''}
            onChange={(e) => onFilter(filter.key, e.target.value || null, filter.operator)}
          >
            <option value="">{filter.placeholder || `All ${filter.label}`}</option>
            {filter.options?.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </Form.Select>
        );

      case 'text':
        return (
          <Form.Control
            key={filter.key}
            type="text"
            placeholder={filter.placeholder || filter.label}
            value={currentValue?.toString() || ''}
            onChange={(e) => onFilter(filter.key, e.target.value || null, filter.operator)}
          />
        );

      case 'date':
        return (
          <Form.Control
            key={filter.key}
            type="date"
            placeholder={filter.placeholder || filter.label}
            value={currentValue?.toString() || ''}
            onChange={(e) => onFilter(filter.key, e.target.value || null, filter.operator)}
          />
        );

      case 'async-select':
        return (
          <AsyncSelect
            key={filter.key}
            value={currentValue as string | number}
            onChange={(val) => onFilter(filter.key, val || null, filter.operator)}
            loadOptions={filter.loadOptions!}
            getOptionLabel={filter.getOptionLabel!}
            getOptionValue={filter.getOptionValue!}
            getOptionSubtitle={filter.getOptionSubtitle}
            placeholder={filter.placeholder || filter.label}
          />
        );

      default:
        return null;
    }
  };

  return (
    <div className="data-table-filters d-flex align-items-center gap-2">
      {filters.map(renderFilter)}
    </div>
  );
};