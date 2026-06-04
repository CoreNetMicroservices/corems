import React, { useEffect } from "react";
import { Badge, Container } from "react-bootstrap";
import { Pencil, Plus } from "react-bootstrap-icons";
import { useHookstate } from "@hookstate/core";
import { useNavigate, Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { DataTable } from "@/common/component/dataTable";
import { useTemplateState } from "@/template/store/TemplateState";
import { Template, TemplateCategory } from "@/template/model/Template";
import { useMessageState } from "@/common/utils/api/ApiResponseHandler";
import { AlertMessage } from "@/common/component/ApiResponseAlert";
import { PageResponse } from "@/common/model/CoreMsApiModel";
import { parseCurrentSort, getInitialDataTableQueryParams, createDataTableActions } from "@/common/component/dataTable/DataTableState";
import { APP_ROUTES } from "@/app/router/routes";
import { formatDate } from "@/common/utils/DateUtils";
import { resolveUserNames } from "@/user/utils/UserApi";

export const TemplateList: React.FC = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { fetchTemplates } = useTemplateState();
  const { initialErrorMessage, errors } = useMessageState();
  const [isLoading, setIsLoading] = React.useState(false);
  const [templates, setTemplates] = React.useState<Template[]>([]);
  const [pagedResponse, setPagedResponse] = React.useState<PageResponse<Template> | undefined>(undefined);
  const [userNames, setUserNames] = React.useState<Record<string, string>>({});
  const [isResolvingNames, setIsResolvingNames] = React.useState(false);
  const [categoryFilter, setCategoryFilter] = React.useState<string>('');

  const queryParams = useHookstate(getInitialDataTableQueryParams());

  const refreshTemplates = async () => {
    setIsLoading(true);
    const res = await fetchTemplates(queryParams.get());
    if (res.result && res.response) {
      setTemplates(res.response.items);
      setPagedResponse(res.response);
    }
    setIsLoading(false);
  };

  const refreshTemplatesCb = React.useCallback(refreshTemplates, [fetchTemplates, JSON.stringify(queryParams.get())]);

  const {
    setSearch,
    setPage,
    setPageSize,
    setSort,
    setFilter
  } = createDataTableActions(queryParams, { onUpdate: refreshTemplatesCb });

  const handleCategoryFilter = (category: string) => {
    setCategoryFilter(category);
    if (category) {
      setFilter('category', category);
    } else {
      setFilter('category', null);
    }
  };

  const columns = [
    { key: "templateId", title: t('template.templateId', 'Template ID'), sortable: true, width: "200px" },
    { key: "name", title: t('template.name', 'Name'), sortable: true, width: "200px" },
    { key: "category", title: t('template.category', 'Category'), sortable: true, width: "120px" },
    { key: "language", title: t('template.language', 'Language'), sortable: true, width: "100px" },
    { key: "updatedAt", title: t('template.lastUpdated', 'Last Updated'), sortable: true, width: "180px" },
    { key: "updatedBy", title: t('template.updatedBy', 'Updated By'), sortable: false, width: "200px" },
    { key: "actions", title: t('common.actions', 'Actions'), sortable: false, width: "100px" },
  ];

  const handleEdit = (id: string) => {
    navigate(APP_ROUTES.TEMPLATE_EDIT.replace(':id', id));
  };

  const handleAddNew = () => {
    navigate(APP_ROUTES.TEMPLATE_NEW);
  };

  const getCategoryBadgeVariant = (category: TemplateCategory): string => {
    switch (category) {
      case TemplateCategory.EMAIL:
        return "primary";
      case TemplateCategory.SMS:
        return "info";
      case TemplateCategory.DOCUMENT:
        return "warning";
      case TemplateCategory.COMMON:
      default:
        return "secondary";
    }
  };

  const renderRow = (template: Template) => {
    return (
      <tr key={`${template.templateId}-${template.language}`}>
        <td className="align-middle">
          <code className="text-primary">{template.templateId}</code>
        </td>
        <td className="align-middle fw-bold">
          {template.name}
        </td>
        <td className="align-middle">
          <Badge bg={getCategoryBadgeVariant(template.category)}>
            {template.category}
          </Badge>
        </td>
        <td className="align-middle">
          <Badge bg="dark">{template.language.toUpperCase()}</Badge>
        </td>
        <td className="align-middle text-muted small">
          {formatDate(template.updatedAt)}
        </td>
        <td className="align-middle">
          {isResolvingNames && !userNames[template.updatedBy] ? (
            <span className="text-muted">{t('common.loading', 'Loading...')}</span>
          ) : userNames[template.updatedBy] ? (
            <Link to={APP_ROUTES.USER_EDIT.replace(':userId', template.updatedBy)}>
              {userNames[template.updatedBy]}
            </Link>
          ) : (
            <span className="text-muted">—</span>
          )}
        </td>
        <td className="align-middle text-end">
          <button
            className="btn btn-sm btn-outline-primary d-inline-flex align-items-center"
            onClick={() => handleEdit(template.id)}
          >
            <Pencil size={14} className="me-1" /> {t('common.edit', 'Edit')}
          </button>
        </td>
      </tr>
    );
  };

  const actions = (
    <button className="btn btn-outline-primary d-flex align-items-center" onClick={handleAddNew}>
      <Plus className="me-2" size={18} /> {t('template.addNew', 'Add New')}
    </button>
  );

  useEffect(() => {
    refreshTemplates();
  }, []);

  useEffect(() => {
    const userIds = new Set<string>();
    templates.forEach(template => {
      if (template.updatedBy) userIds.add(template.updatedBy);
    });

    const ids = Array.from(userIds);
    if (ids.length === 0) return;

    setIsResolvingNames(true);
    resolveUserNames(ids).then(names => {
      setUserNames(names);
    }).finally(() => setIsResolvingNames(false));
  }, [templates]);

  return (
    <Container>
      <AlertMessage initialErrorMessage={initialErrorMessage} errors={errors} />

      <DataTable
        title={t('template.templates', 'Templates')}
        items={templates}
        pagination={pagedResponse ? {
          page: pagedResponse.page,
          pageSize: pagedResponse.pageSize,
          totalElements: pagedResponse.totalElements,
          totalPages: pagedResponse.totalPages
        } : undefined}
        isLoading={isLoading}
        actions={actions}
        columns={columns}
        filters={[
          {
            key: 'category',
            label: t('template.category', 'Category'),
            type: 'select',
            options: [
              { value: '', label: t('common.all', 'All') },
              { value: TemplateCategory.COMMON, label: 'COMMON' },
              { value: TemplateCategory.EMAIL, label: 'EMAIL' },
              { value: TemplateCategory.SMS, label: 'SMS' },
              { value: TemplateCategory.DOCUMENT, label: 'DOCUMENT' },
            ]
          }
        ]}
        filterValues={{ category: categoryFilter }}
        onFilter={(key, value) => {
          if (key === 'category') {
            handleCategoryFilter(value as string);
          }
        }}
        sortableFields={columns.filter(col => col.sortable).map(col => col.key)}
        currentSort={parseCurrentSort(queryParams.sort.get())}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
        onSearch={setSearch}
        onSort={(field, direction) => setSort(field, direction)}
        renderRow={renderRow}
      />
    </Container>
  );
};
