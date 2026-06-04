import React, { useEffect } from "react";
import { Badge, Container } from "react-bootstrap";
import { Pencil, Plus } from "react-bootstrap-icons";
import { useHookstate } from "@hookstate/core";
import { useNavigate, Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { DataTable } from "@/common/component/dataTable";
import { useTranslationState } from "@/translation/store/TranslationState";
import { RealmLanguages, LanguageInfo } from "@/translation/model/Translation";
import { useMessageState } from "@/common/utils/api/ApiResponseHandler";
import { AlertMessage } from "@/common/component/ApiResponseAlert";
import { PageResponse } from "@/common/model/CoreMsApiModel";
import { parseCurrentSort, getInitialDataTableQueryParams, createDataTableActions } from "@/common/component/dataTable/DataTableState";
import { APP_ROUTES } from "@/app/router/routes";
import { formatDate } from "@/common/utils/DateUtils";
import { resolveUserNames } from "@/user/utils/UserApi";

export const TranslationList: React.FC = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { fetchRealms } = useTranslationState();
  const { initialErrorMessage, errors } = useMessageState();
  const [isLoading, setIsLoading] = React.useState(false);
  const [realms, setRealms] = React.useState<RealmLanguages[]>([]);
  const [pagedResponse, setPagedResponse] = React.useState<PageResponse<RealmLanguages> | undefined>(undefined);
  const [userNames, setUserNames] = React.useState<Record<string, string>>({});
  const [isResolvingNames, setIsResolvingNames] = React.useState(false);

  const queryParams = useHookstate(getInitialDataTableQueryParams());

  const refreshRealms = async () => {
    setIsLoading(true);
    const res = await fetchRealms(queryParams.get());
    if (res.result && res.response) {
      setRealms(res.response.items);
      setPagedResponse(res.response);
    }
    setIsLoading(false);
  };

  const refreshRealmsCb = React.useCallback(refreshRealms, [fetchRealms, JSON.stringify(queryParams.get())]);

  const {
    setSearch,
    setPage,
    setPageSize,
    setSort
  } = createDataTableActions(queryParams, { onUpdate: refreshRealmsCb });

  const columns = [
    { key: "realm", title: t('translation.realm', 'Realm'), sortable: true, width: "150px" },
    { key: "updatedAt", title: t('translation.lastUpdated', 'Last Updated'), sortable: true, width: "180px" },
    { key: "updatedBy", title: t('translation.updatedBy', 'Updated By'), sortable: false, width: "200px" },
    { key: "languages", title: t('translation.editLanguages', 'Edit Languages'), sortable: false, width: "auto" },
  ];

  const handleEdit = (realm: string, lang: string) => {
    navigate(APP_ROUTES.TRANSLATION_EDIT.replace(':realm', realm).replace(':lang', lang));
  };

  const handleAddNew = () => {
    navigate(APP_ROUTES.TRANSLATION_NEW);
  };

  const renderLanguageBadge = (realm: string, langInfo: LanguageInfo) => {
    return (
      <Badge
        key={langInfo.lang}
        bg="primary"
        className="me-2 mb-1 d-inline-flex align-items-center gap-1"
        style={{ cursor: 'pointer' }}
        onClick={() => handleEdit(realm, langInfo.lang)}
      >
        <Pencil size={14} />
        {langInfo.lang.toUpperCase()}
      </Badge>
    );
  };

  const renderRow = (realm: RealmLanguages) => {
    const mostRecentLang = realm.languages.reduce((latest, current) =>
      new Date(current.updatedAt) > new Date(latest.updatedAt) ? current : latest
    );

    return (
      <tr key={realm.realm}>
        <td className="align-middle fw-bold">
          {realm.realm}
        </td>
        <td className="align-middle text-muted small">
          {formatDate(mostRecentLang.updatedAt)}
        </td>
        <td className="align-middle">
          {isResolvingNames && !userNames[mostRecentLang.updatedBy] ? (
            <span className="text-muted">{t('common.loading', 'Loading...')}</span>
          ) : userNames[mostRecentLang.updatedBy] ? (
            <Link to={APP_ROUTES.USER_EDIT.replace(':userId', mostRecentLang.updatedBy)}>
              {userNames[mostRecentLang.updatedBy]}
            </Link>
          ) : (
            <span className="text-muted">—</span>
          )}
        </td>
        <td className="align-middle text-end">
          <div className="d-flex flex-wrap">
            {realm.languages.map(lang => renderLanguageBadge(realm.realm, lang))}
          </div>
        </td>
      </tr>
    );
  };

  const actions = (
    <button className="btn btn-outline-primary d-flex align-items-center" onClick={handleAddNew}>
      <Plus className="me-2" size={18} /> {t('translation.addNew', 'Add New')}
    </button>
  );

  useEffect(() => {
    refreshRealms();
  }, []);

  useEffect(() => {
    const userIds = new Set<string>();
    realms.forEach(realm => {
      realm.languages.forEach(lang => {
        if (lang.updatedBy) userIds.add(lang.updatedBy);
      });
    });

    const ids = Array.from(userIds);
    if (ids.length === 0) return;

    setIsResolvingNames(true);
    resolveUserNames(ids).then(names => {
      setUserNames(names);
    }).finally(() => setIsResolvingNames(false));
  }, [realms]);

  return (
    <Container>
      <AlertMessage initialErrorMessage={initialErrorMessage} errors={errors} />

      <DataTable
        title={t('translation.realms', 'Translation Realms')}
        items={realms}
        pagination={pagedResponse ? {
          page: pagedResponse.page,
          pageSize: pagedResponse.pageSize,
          totalElements: pagedResponse.totalElements,
          totalPages: pagedResponse.totalPages
        } : undefined}
        isLoading={isLoading}
        actions={actions}
        columns={columns}
        filters={[]}
        filterValues={{}}
        onFilter={() => { }}
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
