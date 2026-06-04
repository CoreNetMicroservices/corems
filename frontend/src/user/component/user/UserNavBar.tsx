import { Nav } from 'react-bootstrap';
import { Link, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

const UserNavBar = ({ path }: { path: string }) => {
  const location = useLocation();
  const { t } = useTranslation();

  return (
    <Nav.Item>
      <Nav.Link
        as={Link}
        to={path}
        active={location.pathname === path}
      >
        {t('nav.users', 'Users')}
      </Nav.Link>
    </Nav.Item>
  );
};

export default UserNavBar;