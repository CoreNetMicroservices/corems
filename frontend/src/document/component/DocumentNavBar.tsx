import { Link } from "react-router-dom";
import { Nav } from "react-bootstrap";
import { useTranslation } from "react-i18next";

interface DocumentNavBarProps {
  path: string;
}

const DocumentNavBar = ({ path }: DocumentNavBarProps) => {
  const { t } = useTranslation();

  return (
    <Nav.Item>
      <Nav.Link as={Link} to={path}>
        {t("nav.documents", "Documents")}
      </Nav.Link>
    </Nav.Item>
  );
};

export default DocumentNavBar;
