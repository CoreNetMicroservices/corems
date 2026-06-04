import { Link } from "react-router-dom";
import Nav from "react-bootstrap/Nav";
import { useTranslation } from 'react-i18next';

interface TranslationNavBarProps {
  path: string;
}

const TranslationNavBar = ({ path }: TranslationNavBarProps) => {
  const { t } = useTranslation();

  return (
    <Nav.Item>
      <Nav.Link as={Link} to={path}>
        {t('nav.translations', 'Translations')}
      </Nav.Link>
    </Nav.Item>
  );
};

export default TranslationNavBar;
