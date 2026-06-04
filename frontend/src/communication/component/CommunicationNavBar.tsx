import { Nav } from 'react-bootstrap';
import { Link, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

const CommunicationNavBar = ({ path }: { path: string }) => {
  const location = useLocation();
  const { t } = useTranslation();

  return (
    <Nav.Item>
      <Nav.Link
        as={Link}
        to={path}
        active={location.pathname === path}
      >
        {t('nav.communication', 'Communication')}
      </Nav.Link>
    </Nav.Item>
  );
};

export default CommunicationNavBar;