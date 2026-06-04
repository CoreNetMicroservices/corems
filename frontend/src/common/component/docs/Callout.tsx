import { ReactNode } from 'react';
import { Alert } from 'react-bootstrap';

type CalloutType = 'info' | 'warning' | 'tip' | 'danger';

interface CalloutProps {
  type?: CalloutType;
  title?: string;
  children: ReactNode;
  className?: string;
}

const calloutConfig = {
  info: {
    variant: 'info',
    icon: 'info-circle',
  },
  warning: {
    variant: 'warning',
    icon: 'exclamation-triangle',
  },
  tip: {
    variant: 'success',
    icon: 'lightbulb',
  },
  danger: {
    variant: 'danger',
    icon: 'x-circle',
  },
};

export const Callout: React.FC<CalloutProps> = ({ 
  type = 'info', 
  title,
  children,
  className = ''
}) => {
  const config = calloutConfig[type];

  return (
    <Alert variant={config.variant} className={`my-3 ${className}`}>
      <div className="d-flex">
        <i className={`bi bi-${config.icon} me-2 fs-5`}></i>
        <div className="flex-grow-1">
          {title && <Alert.Heading className="h6 mb-2">{title}</Alert.Heading>}
          <div>{children}</div>
        </div>
      </div>
    </Alert>
  );
};

export default Callout;
