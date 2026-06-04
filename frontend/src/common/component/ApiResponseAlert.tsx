import React, { useEffect, useState } from 'react';
import { Alert } from 'react-bootstrap';

export interface AlertMessageProps {
  success?: string | null;
  initialErrorMessage?: string | null;
  errors?: string[] | null;
  className?: string;
}

export const AlertMessage: React.FC<AlertMessageProps> = ({
  success,
  initialErrorMessage,
  errors,
  className = "mb-3"
}) => {
  const [visible, setVisible] = useState<boolean>(false);
  const [timeoutId, setTimeoutId] = useState<number | null>(null);

  // When success prop changes, show the alert and auto-hide after 5s
  useEffect(() => {
    if (!success) {
      setVisible(false);
      if (timeoutId) {
        clearTimeout(timeoutId);
        setTimeoutId(null);
      }
      return;
    }

    // Clear any existing timeout
    if (timeoutId) {
      clearTimeout(timeoutId);
    }

    // Show alert immediately
    setVisible(true);

    // Set new timeout
    const newTimeoutId = setTimeout(() => {
      setVisible(false);
    }, 3000);

    setTimeoutId(newTimeoutId);

    return () => {
      if (newTimeoutId) {
        clearTimeout(newTimeoutId);
      }
    };
  }, [success]);

  if (!visible && !initialErrorMessage && (!errors || errors.length === 0)) {
    return null;
  }

  return (
    <>
      {success && visible && (
        <Alert variant="success" className={className}>
          <strong>Success!</strong> {success}
        </Alert>
      )}

      {(initialErrorMessage || errors) && (
        <Alert variant="danger" className={className}>
          {initialErrorMessage && (
            <strong>{initialErrorMessage}</strong>
          )}
          {errors && errors.length > 0 && (
            <div className={initialErrorMessage ? "mt-1" : ""}>
              {errors.map((errorMsg: string, index: number) => (
                <div key={index} className={index > 0 ? 'mt-1' : ''}>
                  {errorMsg}
                </div>
              ))}
            </div>
          )}
        </Alert>
      )}
    </>
  );
};

export default AlertMessage;