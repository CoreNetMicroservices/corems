import { Breadcrumb as BootstrapBreadcrumb } from 'react-bootstrap';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft } from 'react-bootstrap-icons';

export interface BreadcrumbItem {
  label: string;
  href?: string;
  active?: boolean;
}

interface BreadcrumbProps {
  items: BreadcrumbItem[];
}

const Breadcrumb = ({ items }: BreadcrumbProps) => {
  const navigate = useNavigate();

  const handleClick = (href: string) => {
    navigate(href);
  };

  return (
    <nav aria-label="breadcrumb" className="mb-4">
      <BootstrapBreadcrumb className="mb-0">
        {items.map((item, index) => (
          <BootstrapBreadcrumb.Item
            key={index}
            active={item.active || index === items.length - 1}
            onClick={item.href && !item.active ? () => handleClick(item.href!) : undefined}
            style={{
              cursor: item.href && !item.active ? 'pointer' : 'default'
            }}
            className={item.href && !item.active ? 'text-decoration-none' : ''}
          >
            {index === 0 && item.href && (
              <ArrowLeft className="me-1" size={16} />
            )}
            {item.label}
          </BootstrapBreadcrumb.Item>
        ))}
      </BootstrapBreadcrumb>
    </nav>
  );
};

export default Breadcrumb;