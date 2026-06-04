import React from "react";
import { DataTablePagination } from "./DataTableTypes";

interface DataTableHeaderProps {
  title?: string;
  totalCount: number;
  pagination?: DataTablePagination;
}

export const DataTableHeader: React.FC<DataTableHeaderProps> = ({
  title,
  totalCount,
  pagination,
}) => {
  const renderPageInfo = () => {
    if (!pagination) return null;

    const startItem = ((pagination.page - 1) * pagination.pageSize) + 1;
    const endItem = Math.min(pagination.page * pagination.pageSize, pagination.totalElements);

    return (
      <small className="text-muted">
        Showing {startItem} to {endItem}
      </small>
    );
  };

  return (
    <div className="d-flex justify-content-between align-items-center">
      <div>
        {title && (
          <h5 className="mb-0">
            {title} ({totalCount} total)
          </h5>
        )}
      </div>
      {renderPageInfo()}
    </div>
  );
};