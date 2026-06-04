import React from 'react';
import { Pagination, Form } from 'react-bootstrap';
import { DataTablePagination } from './DataTableTypes';

interface DataTablePaginationProps {
  pagination: DataTablePagination;
  pageSizeOptions: number[];
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
}

export const DataTablePaginationComponent: React.FC<DataTablePaginationProps> = ({
  pagination,
  pageSizeOptions,
  onPageChange,
  onPageSizeChange
}) => {
  if (pagination.totalPages <= 1) {
    return (
      <div className="d-flex justify-content-end">
        <Form.Select
          className="w-auto"
          value={pagination.pageSize}
          onChange={(e) => onPageSizeChange(parseInt(e.target.value))}
        >
          {pageSizeOptions.map((size) => (
            <option key={size} value={size}>
              {size} per page
            </option>
          ))}
        </Form.Select>
      </div>
    );
  }

  return (
    <div className="d-flex justify-content-between align-items-center flex-wrap">
      <div className="d-none d-md-block"></div>

      <div className="d-flex justify-content-center mb-2 mb-md-0">
        <Pagination className="m-2">
          <Pagination.First
            onClick={() => onPageChange(1)}
            disabled={pagination.page === 1}
          />
          <Pagination.Prev
            onClick={() => onPageChange(pagination.page - 1)}
            disabled={pagination.page === 1}
          />

          {Array.from({ length: Math.min(5, pagination.totalPages) }, (_, i) => {
            let pageNum;
            if (pagination.totalPages <= 5) {
              pageNum = i + 1;
            } else if (pagination.page <= 3) {
              pageNum = i + 1;
            } else if (pagination.page >= pagination.totalPages - 2) {
              pageNum = pagination.totalPages - 4 + i;
            } else {
              pageNum = pagination.page - 2 + i;
            }

            return (
              <Pagination.Item
                key={pageNum}
                active={pageNum === pagination.page}
                onClick={() => onPageChange(pageNum)}
              >
                {pageNum}
              </Pagination.Item>
            );
          })}

          <Pagination.Next
            onClick={() => onPageChange(pagination.page + 1)}
            disabled={pagination.page === pagination.totalPages}
          />
          <Pagination.Last
            onClick={() => onPageChange(pagination.totalPages)}
            disabled={pagination.page === pagination.totalPages}
          />
        </Pagination>
      </div>

      <div className="d-flex justify-content-center justify-content-md-end">
        <Form.Select
          className="w-auto"
          value={pagination.pageSize}
          onChange={(e) => onPageSizeChange(parseInt(e.target.value))}
        >
          {pageSizeOptions.map((size) => (
            <option key={size} value={size}>
              {size} per page
            </option>
          ))}
        </Form.Select>
      </div>
    </div>
  );
};