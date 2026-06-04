import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Button, Container, Badge } from 'react-bootstrap';
import { PencilSquare, Plus, CheckCircleFill, ExclamationCircleFill, Envelope } from 'react-bootstrap-icons';
import { useHookstate } from '@hookstate/core';
import { useTranslation } from 'react-i18next';
import { getAllUsers } from '@/user/store/UserState';
import { useMessageState } from '@/common/utils/api/ApiResponseHandler';
import { AlertMessage } from '@/common/component/ApiResponseAlert';
import { DataTable } from '@/common/component/dataTable';
import type { DataTableColumn, DataTableFilter } from '@/common/component/dataTable';
import type { User } from '@/user/model/User';
import { PageResponse } from "@/common/model/CoreMsApiModel";
import { APP_ROUTES } from '@/app/router/routes';
import { UserAvatar } from '@/user/component/shared/UserAvatar';
import { parseCurrentSort, getInitialDataTableQueryParams, createDataTableActions } from '@/common/component/dataTable/DataTableState';
import { formatDate } from '@/common/utils/DateUtils';

const UserList = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [users, setUsers] = useState<User[]>([]);
  const [pagedResponse, setPagedResponse] = useState<PageResponse<User> | undefined>(undefined);
  const [isLoading, setIsLoading] = useState(false);

  // Local state for query params
  const queryParams = useHookstate(getInitialDataTableQueryParams());
  const {
    setSearch,
    setPage,
    setPageSize,
    setFilter,
    setSort
  } = createDataTableActions(queryParams);

  const { initialErrorMessage, errors } = useMessageState();

  useEffect(() => {
    setIsLoading(true);
    getAllUsers(queryParams.get()).then((res) => {
      if (res.result && res.response) {
        setUsers(res.response.items);
        setPagedResponse(res.response);
      }
    }).finally(() => setIsLoading(false));
  }, [JSON.stringify(queryParams.get())]);

  // Configuration for DataTable
  const columns: DataTableColumn[] = [
    { key: 'name', title: t('user.name', 'Name'), sortable: true },
    { key: 'email', title: t('form.email', 'Email'), sortable: true },
    { key: 'provider', title: t('user.provider', 'Provider'), sortable: true },
    { key: 'lastLoginAt', title: t('user.lastLogin', 'Last Login'), sortable: true },
    { key: 'actions', title: t('common.actions', 'Actions') },
  ];

  const currentSort = parseCurrentSort(queryParams.sort.get());

  const filters: DataTableFilter[] = [
    {
      key: 'provider',
      label: t('user.provider', 'Provider'),
      type: 'select',
      operator: 'contains',
      placeholder: t('user.allProviders', 'All Providers'),
      options: [
        { value: 'google', label: 'Google' },
        { value: 'github', label: 'GitHub' },
        { value: 'local', label: t('user.localProvider', 'Local') }
      ]
    }
  ];


  // Render row function for DataTable
  const renderUserRow = (user: User) => (
    <tr key={user.userId}>
      <td>
        <div className="d-flex align-items-center">
          <UserAvatar user={user} className="me-3" />
          <span className="fw-medium">
            {user.firstName} {user.lastName}
          </span>
        </div>
      </td>
      <td>
        <div className="d-flex align-items-center gap-2">
          <Envelope size={14} />
          <span>{user.email}</span>
          {user.emailVerified ? (
            <Badge bg="success" className="d-flex align-items-center gap-1">
              <CheckCircleFill size={10} />
              {t('profile.verified', 'Verified')}
            </Badge>
          ) : (
            <Badge bg="warning" className="d-flex align-items-center gap-1">
              <ExclamationCircleFill size={10} />
              {t('profile.unverified', 'Unverified')}
            </Badge>
          )}
        </div>
      </td>
      <td>{user.provider}</td>
      <td>{formatDate(user.lastLoginAt)}</td>
      <td>
        <Link to={APP_ROUTES.USER_EDIT.replace(':userId', user.userId)}>
          <Button variant="outline-primary" size="sm">
            <PencilSquare className="me-1" />
            {t('common.edit', 'Edit')}
          </Button>
        </Link>
      </td>
    </tr>
  );

  const actions = (
    <Button variant="btn btn-outline-primary d-flex align-items-center" onClick={() => navigate(APP_ROUTES.USER_ADD)}>
      <Plus className="me-2" />
      {t('user.addNewUser', 'Add New User')}
    </Button>
  );

  return (
    <Container>
      <AlertMessage initialErrorMessage={initialErrorMessage} errors={errors} />

      <DataTable
        // Data
        items={users as User[]}
        pagination={pagedResponse ? {
          page: pagedResponse.page,
          pageSize: pagedResponse.pageSize,
          totalElements: pagedResponse.totalElements,
          totalPages: pagedResponse.totalPages
        } : undefined}
        isLoading={isLoading}

        // Configuration
        columns={columns}
        filters={filters}
        filterValues={queryParams.filters.get() || {}}
        sortableFields={columns.filter(col => col.sortable).map(col => col.key)}
        currentSort={currentSort}
        searchPlaceholder={t('user.searchPlaceholder', 'Search users by name or email...')}

        // Callbacks
        onSearch={setSearch}
        onFilter={setFilter}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
        onSort={setSort}

        // Render function
        renderRow={renderUserRow}

        // Customization
        title={t('user.allUsers', 'All Users')}
        actions={actions}
      />
    </Container>
  );
};

export default UserList;