import { Link } from 'react-router-dom';
import { Navbar, Container } from 'react-bootstrap';
import { BoxArrowInRight, List } from 'react-bootstrap-icons';
import { useTranslation } from 'react-i18next';

import AppTheme from '@/app/layout/AppTheme';
import { LanguageSelector } from '@/common/utils/i18n/LanguageSelector';
import AuthNavBar from '@/user/component/auth/AuthNavBar';
import { getCurrentUserAuth } from '@/user/store/AuthState';
import { createAuthGuards } from '@/common/router/AuthGuards';
import { APP_ROUTES } from '@/app/router/routes';

const authGuards = createAuthGuards({
  homeRoute: APP_ROUTES.HOME,
  loginRoute: APP_ROUTES.LOGIN,
  authFunctions: {
    getCurrentUserAuth,
    hasAnyRole: () => false,
  },
});

interface AppHeaderProps {
  onToggleSidebar?: () => void;
}

const AppHeader = ({ onToggleSidebar }: AppHeaderProps) => {
  const { t } = useTranslation();

  return (
    <Navbar className="border-bottom" style={{ position: 'fixed', top: 0, left: 0, right: 0, zIndex: 1030, backgroundColor: 'var(--bs-body-bg)' }}>
      <Container fluid>
        <div className="d-flex align-items-center">
          {onToggleSidebar && (
            <button className="btn btn-link text-body d-lg-none p-0 me-2" onClick={onToggleSidebar}>
              <List size={28} />
            </button>
          )}
          <Navbar.Brand  as={Link} to={APP_ROUTES.HOME} style={{ color: 'var(--bs-body-color)' }}>
            <i className="bi bi-hexagon-fill me-2"></i>
            CoreMS v1.0.0
          </Navbar.Brand>
        </div>

        <div className="d-flex align-items-center gap-3">
          <AppTheme />
          <LanguageSelector />
          {authGuards.isAuthenticated() ? (
            <AuthNavBar />
          ) : (
            <Link to={APP_ROUTES.LOGIN} className="nav-link" title={t('auth.login', 'Login')}>
              <BoxArrowInRight size={22} />
            </Link>
          )}
        </div>
      </Container>
    </Navbar>
  );
};

export default AppHeader;
