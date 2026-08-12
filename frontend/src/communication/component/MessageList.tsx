import React, { useEffect } from "react";
import { Badge, Container } from "react-bootstrap";
import { Envelope, ChatDots, CheckCircle, XCircle, Clock, Eye, Send } from "react-bootstrap-icons";
import { useHookstate } from "@hookstate/core";
import { useTranslation } from "react-i18next";
import { DataTable, DataTableFilter } from "@/common/component/dataTable";
import { useMessagesState } from "@/communication/store/MessageState";
import { Message } from "@/communication/model/Message";
import { useMessageState } from "@/common/utils/api/ApiResponseHandler";
import { AlertMessage } from "@/common/component/ApiResponseAlert";
import { searchUsers } from "@/user/utils/UserApi";
import { User } from "@/user/model/User";
import { parseCurrentSort, getInitialDataTableQueryParams, createDataTableActions } from "@/common/component/dataTable/DataTableState";
import { formatDate } from "@/common/utils/DateUtils";
import { getRecipient, getContentPreview } from "@/communication/utils/MessageUtils";
import { PageResponse } from "@/common/model/CoreMsApiModel";
import { resolveUserNames } from "@/user/utils/UserApi";
import { Link } from "react-router-dom";
import SendMessageModal from "@/communication/component/SendMessageModal";
import MessageDetailModal from "@/communication/component/MessageDetailModal";
import { APP_ROUTES } from "@/app/router/routes";
import { hasAnyRole } from "@/user/store/AuthState";
import { AppRoles } from "@/common/AppRoles";

export const MessageList: React.FC<{ userId?: string }> = ({ userId }) => {
  const { t } = useTranslation();
  const { fetchMessages } = useMessagesState();
  const { initialErrorMessage, errors } = useMessageState();
  const [isLoading, setIsLoading] = React.useState(false);
  const [messages, setMessages] = React.useState<Message[]>([]);
  const [pagedResponse, setPagedResponse] = React.useState<PageResponse<Message> | undefined>(undefined);
  const [userNames, setUserNames] = React.useState<Record<string, string>>({});
  const [isResolvingNames, setIsResolvingNames] = React.useState(false);
  const [showSendModal, setShowSendModal] = React.useState(false);
  const [selectedMessage, setSelectedMessage] = React.useState<Message | null>(null);
  const [showDetailModal, setShowDetailModal] = React.useState(false);
  const isAdmin = hasAnyRole([AppRoles.CommunicationMsAdmin]);

  const queryParams = useHookstate(getInitialDataTableQueryParams());

  const refreshMessages = async () => {
    setIsLoading(true);
    const res = await fetchMessages(queryParams.get());
    if (res.result && res.response) {
      setMessages(res.response.items);
      setPagedResponse(res.response);
    }
    setIsLoading(false);
  };

  const refreshMessagesCb = React.useCallback(refreshMessages, [fetchMessages, JSON.stringify(queryParams.get())]);

  const {
    setSearch,
    setPage,
    setPageSize,
    setFilter,
    setSort
  } = createDataTableActions(queryParams, { onUpdate: refreshMessagesCb });

  useEffect(() => {
    if (userId) {
      setFilter('userId', userId);
    }
  }, [userId]);

  const handleViewMessage = (msg: Message) => {
    setSelectedMessage(msg);
    setShowDetailModal(true);
  };

  const filters: DataTableFilter<User>[] = [];

  if (!userId) {
    filters.push({
      key: 'userId',
      label: t('message.user', 'User'),
      type: 'async-select',
      placeholder: t('message.filterByUser', 'Filter by user'),
      loadOptions: searchUsers,
      getOptionLabel: (user: User) => `${user.firstName} ${user.lastName}`,
      getOptionValue: (user: User) => user.userId,
      getOptionSubtitle: (user: User) => user.email
    });
  }

  filters.push({
    key: 'type',
    label: t('message.channel', 'Channel'),
    type: 'select',
    placeholder: t('message.filterByChannel', 'Filter by channel'),
    operator: 'eq',
    options: [
      { value: 'email', label: t('message.email', 'Email') },
      { value: 'sms', label: t('message.sms', 'SMS') }
    ]
  });

  const columns = [
    { key: "type", title: t('message.channel', 'Channel'), sortable: false, width: "100px" },
    { key: "createdAt", title: t('message.sent', 'Sent'), sortable: true, width: "180px" },
    { key: "recipient", title: t('message.to', 'To'), sortable: false },
    { key: "content", title: t('message.content', 'Content'), sortable: false },
    { key: "status", title: t('message.status', 'Status'), sortable: true, width: "80px" },
    { key: "sentBy", title: t('message.sentBy', 'Sent By'), sortable: false },
    { key: "actions", title: "", sortable: false, width: "80px" },
  ];

  const renderStatus = (status: string) => {
    switch (status) {
      case 'sent':
        return <Badge bg="success" className="d-flex align-items-center gap-1"><CheckCircle /> {t('message.statusSent', 'Sent')}</Badge>;
      case 'failed':
        return <Badge bg="danger" className="d-flex align-items-center gap-1"><XCircle /> {t('message.statusFailed', 'Failed')}</Badge>;
      case 'enqueued':
        return <Badge bg="primary" className="d-flex align-items-center gap-1"><Clock /> {t('message.statusQueued', 'Queued')}</Badge>;
      default:
        return <Badge bg="secondary">{status}</Badge>;
    }
  };

  const renderRow = (msg: Message) => {
    const preview = getContentPreview(msg);

    return (
      <tr key={msg.uuid}>
        <td className="align-middle">
          {msg.type === 'email' ?
            <div className="d-flex align-items-center text-info"><Envelope className="me-2" /> {t('message.email', 'Email')}</div> :
            <div className="d-flex align-items-center text-warning"><ChatDots className="me-2" /> {t('message.sms', 'SMS')}</div>
          }
        </td>
        <td className="align-middle text-muted small">
          {msg.createdAt ? formatDate(msg.createdAt) : "-"}
        </td>
        <td className="align-middle fw-medium">
          {(isResolvingNames && !userNames[msg.userId])
            ? t('common.loading', 'Loading...')
            : (userNames[msg.userId]
              ? <Link to={`${APP_ROUTES.USER_EDIT.replace(':userId', msg.userId)}`}>{userNames[msg.userId]}</Link>
              : getRecipient(msg))}
        </td>
        <td className="align-middle">
          <div className="text-truncate" style={{ maxWidth: '300px' }}>
            {msg.type === 'email' && <span className="fw-bold me-1">{t('message.subject', 'Subject')}:</span>}
            {preview}
          </div>
        </td>
        <td className="align-middle">
          {renderStatus(msg.status)}
        </td>
        <td className="align-middle">
          {msg.sentByType === 'user' && msg.sentById
            ? ((isResolvingNames && !userNames[msg.sentById]) ? t('common.loading', 'Loading...') : <Link to={`${APP_ROUTES.USER_EDIT.replace(':userId', msg.sentById)}`}>{userNames[msg.sentById] || '—'}</Link>)
            : t('message.system', 'System')}
        </td>
        <td className="align-middle text-end">
          <button
            className="btn btn-sm btn-outline-secondary d-inline-flex align-items-center gap-1"
            onClick={() => handleViewMessage(msg)}
            title={t('message.viewMessage', 'View Message')}
          >
            <Eye size={16} />
          </button>
        </td>
      </tr>
    );
  };

  const actions = (
    isAdmin ?
      <button className="btn btn-outline-primary d-flex align-items-center" onClick={() => setShowSendModal(true)}>
        <Send className="me-2" /> {t('message.sendMessage', 'Send Message')}
      </button>
    : null
  );

  useEffect(() => {
    refreshMessages();
  }, []);

  return (
    <Container>
      <AlertMessage initialErrorMessage={initialErrorMessage} errors={errors} />

      <NameResolutionEffect messages={messages} setUserNames={setUserNames} setIsResolving={setIsResolvingNames} />
      <DataTable
        title=""
        items={messages as Message[]}
        pagination={pagedResponse ? {
          page: pagedResponse.page,
          pageSize: pagedResponse.pageSize,
          totalElements: pagedResponse.totalElements,
          totalPages: pagedResponse.totalPages
        } : undefined}
        isLoading={isLoading}
        actions={actions}
        columns={columns}
        filters={filters}
        filterValues={queryParams.filters.get() || {}}
        onFilter={setFilter}
        sortableFields={columns.filter(col => col.sortable).map(col => col.key)}
        currentSort={parseCurrentSort(queryParams.sort.get())}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
        onSearch={setSearch}
        onSort={(field, direction) => setSort(field, direction)}
        renderRow={renderRow}
      />

      <SendMessageModal
        show={showSendModal}
        userId={userId}
        onClose={() => setShowSendModal(false)}
        onSent={() => { setShowSendModal(false); refreshMessages(); }}
      />

      <MessageDetailModal
        show={showDetailModal}
        message={selectedMessage}
        onClose={() => { setShowDetailModal(false); setSelectedMessage(null); }}
        userNames={userNames}
      />
    </Container>
  );
};

// Side-effect component for resolving user names with loading state
const NameResolutionEffect: React.FC<{
  messages: Message[];
  setUserNames: React.Dispatch<React.SetStateAction<Record<string, string>>>;
  setIsResolving: React.Dispatch<React.SetStateAction<boolean>>;
}> = ({ messages, setUserNames, setIsResolving }) => {
  useEffect(() => {
    const idsSet = new Set<string>();
    messages.forEach(m => {
      if (m.userId) idsSet.add(m.userId);
      if (m.sentByType === 'user' && m.sentById) idsSet.add(m.sentById);
    });
    const ids = Array.from(idsSet);
    if (ids.length === 0) return;
    setIsResolving(true);
    resolveUserNames(ids).then(names => {
      setUserNames(prev => ({ ...prev, ...names }));
    }).finally(() => setIsResolving(false));
  }, [messages]);
  return null;
};
