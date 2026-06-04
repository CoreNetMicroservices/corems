import { ReactNode } from 'react';

interface DocsLayoutProps {
  children: ReactNode;
}

export const DocsLayout: React.FC<DocsLayoutProps> = ({ children }) => {
  return <div className="flex-grow-1">{children}</div>;
};

export default DocsLayout;
