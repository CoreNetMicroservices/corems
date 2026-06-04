import { Row, Col, Card, Table, Spinner } from 'react-bootstrap';
import { useTranslation } from 'react-i18next';
import { DataTableProps } from './DataTableTypes';
import { DataTableSearch } from './DataTableSearch';
import { DataTableFilters } from './filter/DataTableFilters';
import { DataTableHeader } from './DataTableHeader';
import { DataTablePaginationComponent } from './DataTablePagination';

export function DataTable<T>({
  // Data
  items,
  pagination,
  isLoading,

  // Configuration
  columns,
  filters = [],
  filterValues = {},
  sortableFields = [],
  currentSort,
  searchPlaceholder = "Search...",
  pageSizeOptions = [10, 20, 50, 100],

  // Callbacks
  onSearch,
  onFilter,
  onPageChange,
  onPageSizeChange,
  onSort,

  // Render functions
  renderRow,

  // Optional customization
  title,
  actions
}: DataTableProps<T>) {
  const { t } = useTranslation();

  if (isLoading && items.length === 0) {
    return (
      <div className="d-flex justify-content-center align-items-center" style={{ minHeight: 400 }}>
        <Spinner animation="border" variant="primary">
          <span className="visually-hidden">{t('common.loading', 'Loading...')}</span>
        </Spinner>
      </div>
    );
  }

  const renderTableHeader = () => (
    <thead>
      <tr>
        {columns.map((column) => {
          const isSortable = sortableFields.includes(column.key);
          const isCurrentSort = currentSort?.field === column.key;
          const sortDirection = isCurrentSort ? currentSort.direction : null;

          return (
            <th
              key={column.key}
              style={{
                width: column.width,
                cursor: isSortable ? 'pointer' : 'default'
              }}
              className={`${isSortable ? 'user-select-none' : ''}`}
              onClick={() => {
                if (isSortable && onSort) {
                  const newDirection = isCurrentSort && sortDirection === 'desc' ? 'asc' : 'desc';
                  onSort(column.key, newDirection);
                }
              }}
            >
              <div className="d-flex align-items-center">
                {column.title}
                {isSortable && (
                  <span className="ms-1">
                    {!isCurrentSort && <i className="bi bi-arrow-down-up text-muted"></i>}
                    {isCurrentSort && sortDirection === 'desc' && <i className="bi bi-arrow-down"></i>}
                    {isCurrentSort && sortDirection === 'asc' && <i className="bi bi-arrow-up"></i>}
                  </span>
                )}
              </div>
            </th>
          );
        })}
      </tr>
    </thead>
  );

  return (
    <>
      {/* Actions Row */}
      {actions && (
        <Row className="mb-3">
          <Col className="d-flex justify-content-end">
            {actions}
          </Col>
        </Row>
      )}

      {/* Filters and Search Row */}
      <Row className="mb-3">
        {filters.length > 0 && (
          <Col md={8} className="d-flex align-items-center gap-2">
            <DataTableFilters
              filters={filters}
              values={filterValues}
              onFilter={onFilter || (() => { })}
            />
          </Col>
        )}


        {/* Search */}
        {onSearch && (
          <Col md={4}>
            <DataTableSearch
              placeholder={searchPlaceholder}
              onSearch={onSearch}
            />
          </Col>
        )}
      </Row>

      {/* Main Table Card */}
      <Card>
        <Card.Header>
          <DataTableHeader
            title={title}
            totalCount={pagination?.totalElements || items.length}
            pagination={pagination}
          />
        </Card.Header>

        <Card.Body className="p-0">
          <Table responsive striped hover className="mb-0">
            {renderTableHeader()}
            <tbody>
              {items.map((item, index) => renderRow(item, index))}
            </tbody>
          </Table>
        </Card.Body>

        {/* Pagination Footer */}
        {pagination && (onPageChange || onPageSizeChange) && (
          <Card.Footer>
            <DataTablePaginationComponent
              pagination={pagination}
              pageSizeOptions={pageSizeOptions}
              onPageChange={onPageChange || (() => { })}
              onPageSizeChange={onPageSizeChange || (() => { })}
            />
          </Card.Footer>
        )}
      </Card>
    </>
  );
}