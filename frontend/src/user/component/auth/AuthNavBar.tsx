import NavDropdown from 'react-bootstrap/NavDropdown';
import { Link } from 'react-router-dom';
import { PersonCircle, PencilSquare, BoxArrowRight } from 'react-bootstrap-icons';
import { useTranslation } from 'react-i18next';
import { useAuthState, signOut } from '@/user/store/AuthState';
import { APP_ROUTES } from '@/app/router/routes';

const AuthNavBar = () => {
  const { t } = useTranslation();
  const authState = useAuthState();
  const user = authState.user.get();

  return (
    <NavDropdown
      align="end"
      title={<PersonCircle size={22} />}
      id="profile-dropdown"
      menuVariant="light"
    >
      <NavDropdown.Item disabled style={{ opacity: 1, fontWeight: 600 }}>
        {user?.firstName} {user?.lastName}
      </NavDropdown.Item>
      <NavDropdown.Divider />
      <NavDropdown.Item as={Link} to={APP_ROUTES.USER_PROFILE}>
        <PencilSquare className="me-2" />{t('profile.editProfile', 'Edit Profile')}
      </NavDropdown.Item>
      <NavDropdown.Divider />
      <NavDropdown.Item onClick={signOut} className="text-danger">
        <BoxArrowRight className="me-2" />{t('auth.logout', 'Logout')}
      </NavDropdown.Item>
    </NavDropdown>
  );
};

export default AuthNavBar;
