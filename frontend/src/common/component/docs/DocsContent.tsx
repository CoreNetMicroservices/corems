import { ReactNode } from 'react';
import { Breadcrumb, Container } from 'react-bootstrap';
import { Link } from 'react-router-dom';

interface BreadcrumbItem {
  label: string;
  path?: string;
}

interface DocsContentProps {
  title: string;
  breadcrumbs?: BreadcrumbItem[];
  children: ReactNode;
}

export const DocsContent: React.FC<DocsContentProps> = ({ 
  title, 
  breadcrumbs,
  children 
}) => {
  return (
    <div className="docs-content flex-grow-1" style={{ maxWidth: '900px', margin: '0 auto' }}>
      <Container fluid className="py-4 px-4 px-lg-5">
        {breadcrumbs && breadcrumbs.length > 0 && (
          <Breadcrumb className="mb-3">
            {breadcrumbs.map((crumb, idx) => (
              <Breadcrumb.Item
                key={idx}
                active={idx === breadcrumbs.length - 1}
                linkAs={crumb.path ? Link : 'span'}
                linkProps={crumb.path ? { to: crumb.path } : {}}
              >
                {crumb.label}
              </Breadcrumb.Item>
            ))}
          </Breadcrumb>
        )}
        
        <h1 className="display-5 fw-bold mb-4">{title}</h1>
        
        <div className="docs-body">
          {children}
        </div>
      </Container>
    </div>
  );
};

export default DocsContent;
