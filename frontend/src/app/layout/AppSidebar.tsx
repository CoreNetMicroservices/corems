import { useState } from 'react';
import { NavLink } from 'react-router-dom';
import { Offcanvas, Collapse } from 'react-bootstrap';
import { useTranslation } from 'react-i18next';
import { getCurrentUserAuth, hasAnyRole } from '@/user/store/AuthState';
import { AppRoles } from '@/common/AppRoles';
import { ROUTE_PATHS } from '@/app/router/routes';
import { docsNavigation } from '@/app/pages/docs';

interface AppSidebarProps {
  show: boolean;
  onHide: () => void;
}

const AppSidebar = ({ show, onHide }: AppSidebarProps) => {
  const { t } = useTranslation();
  const { isAuthenticated } = getCurrentUserAuth();
  const [openDocs, setOpenDocs] = useState(true);

  const appNav = [
    { icon: 'people-fill', label: t('nav.users', 'Users'), path: ROUTE_PATHS.USERS_LIST, roles: [AppRoles.UserMsAdmin] },
    { icon: 'envelope-fill', label: t('nav.communication', 'Communication'), path: ROUTE_PATHS.COMMUNICATION, auth: true },
    { icon: 'folder-fill', label: t('nav.documents', 'Documents'), path: ROUTE_PATHS.DOCUMENTS, auth: true },
    { icon: 'translate', label: t('nav.translations', 'Translations'), path: ROUTE_PATHS.TRANSLATIONS, roles: [AppRoles.TranslationMsAdmin] },
    { icon: 'file-earmark-text-fill', label: t('nav.templates', 'Templates'), path: ROUTE_PATHS.TEMPLATES, roles: [AppRoles.TemplateMsAdmin] },
    { divider: true },
    { 
      icon: 'book-fill', 
      label: 'Documentation', 
      collapsible: true,
      children: docsNavigation.flatMap(section => section.items)
    },
  ];

  const visible = appNav.filter(item => {
    if (item.divider) return false;
    if (item.collapsible) return true;
    if (item.roles) return hasAnyRole(item.roles);
    if (item.auth) return isAuthenticated;
    return true;
  });

  // Check if there are any non-documentation items (app navigation items)
  const hasAppItems = visible.some(item => !item.collapsible);

  const content = (
    <div className="p-3">
      {visible.map((item, i) => {
        if (item.collapsible && item.children) {
          return (
            <div key={i}>
              {hasAppItems && <hr className="my-3" />}
              <button
                className="btn btn-link text-body text-decoration-none d-flex align-items-center w-100 py-2 px-3 rounded mb-1"
                onClick={() => setOpenDocs(!openDocs)}
              >
                <i className={`bi bi-${item.icon} me-2`}></i>
                <span className="flex-grow-1 text-start">{item.label}</span>
                <i className={`bi bi-chevron-${openDocs ? 'down' : 'right'}`}></i>
              </button>
              <Collapse in={openDocs}>
                <div className="ps-3">
                  {item.children.map((child, j) => (
                    <NavLink
                      key={j}
                      to={child.path}
                      onClick={onHide}
                      className={({ isActive }) => 
                        `d-block py-2 px-3 rounded mb-1 text-decoration-none ${isActive ? 'text-primary fw-semibold' : 'text-body'}`
                      }
                    >
                      {child.label}
                    </NavLink>
                  ))}
                </div>
              </Collapse>
            </div>
          );
        }

        return (
          <NavLink
            key={i}
            to={item.path || '/'}
            onClick={onHide}
            className={({ isActive }) => 
              `d-flex align-items-center py-2 px-3 rounded mb-1 text-decoration-none ${isActive ? 'text-primary fw-semibold' : 'text-body'}`
            }
          >
            <i className={`bi bi-${item.icon} me-2`}></i>
            {item.label}
          </NavLink>
        );
      })}
    </div>
  );

  return (
    <>
      <div className="d-none d-lg-block bg-body border-end" style={{ width: '250px', height: 'calc(100vh - 56px)', position: 'sticky', top: '56px', overflowY: visible.length > 10 ? 'auto' : 'hidden' }}>
        {content}
      </div>

      <Offcanvas show={show} onHide={onHide} placement="start" className="d-lg-none">
        <Offcanvas.Header closeButton>
          <Offcanvas.Title>Menu</Offcanvas.Title>
        </Offcanvas.Header>
        <Offcanvas.Body>
          {content}
        </Offcanvas.Body>
      </Offcanvas>
    </>
  );
};

export default AppSidebar;
