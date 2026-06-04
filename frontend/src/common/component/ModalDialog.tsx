import React from 'react';
import { Modal, Button } from 'react-bootstrap';

export interface ModalDialogProps {
  title: string;
  show: boolean;
  onClose: () => void;
  onPrimary?: () => void;
  primaryText?: string;
  primaryVariant?: string;
  secondaryText?: string;
  size?: 'sm' | 'lg' | 'xl';
  disabledPrimary?: boolean;
  disablePrimary?: boolean;
  footerContent?: React.ReactNode;
  children: React.ReactNode;
}

export const ModalDialog: React.FC<ModalDialogProps> = ({
  title,
  show,
  onClose,
  onPrimary,
  primaryText = 'Save',
  primaryVariant = 'primary',
  secondaryText = 'Cancel',
  size,
  disabledPrimary,
  disablePrimary,
  footerContent,
  children,
}) => {
  const isDisabled = disabledPrimary || disablePrimary;

  return (
    <Modal show={show} onHide={onClose} size={size} centered backdrop="static">
      <Modal.Header closeButton>
        <Modal.Title>{title}</Modal.Title>
      </Modal.Header>
      <Modal.Body>{children}</Modal.Body>
      <Modal.Footer>
        <Button variant="secondary" onClick={onClose}>
          {secondaryText}
        </Button>
        {footerContent}
        {onPrimary && (
          <Button variant={primaryVariant} onClick={onPrimary} disabled={isDisabled}>
            {primaryText}
          </Button>
        )}
      </Modal.Footer>
    </Modal>
  );
};

export default ModalDialog;
