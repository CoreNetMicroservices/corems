import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Modal, Button, Form, Spinner } from 'react-bootstrap';
import { Key, X } from 'react-bootstrap-icons';
import { useTranslation } from 'react-i18next';
import { adminChangeUserPassword } from '@/user/store/UserState';
import { useMessageState } from '@/common/utils/api/ApiResponseHandler';
import { AlertMessage } from '@/common/component/ApiResponseAlert';

interface AdminChangePasswordModalProps {
  show: boolean;
  onHide: () => void;
  userId: string;
  userName: string;
}

interface PasswordFormValues {
  newPassword: string;
  confirmPassword: string;
}

const AdminChangePasswordModal = ({ show, onHide, userId, userName }: AdminChangePasswordModalProps) => {
  const { t } = useTranslation();
  const [isSubmitting, setIsSubmitting] = useState(false);

  const { register, handleSubmit, reset, watch, formState: { errors } } = useForm<PasswordFormValues>();
  const { success, initialErrorMessage, errors: apiErrors, handleResponse, clearAll } = useMessageState();

  const newPassword = watch('newPassword');

  const handleClose = () => {
    reset();
    clearAll();
    onHide();
  };

  const onSubmit = async (data: PasswordFormValues) => {
    setIsSubmitting(true);

    const passwordRequest = {
      newPassword: data.newPassword,
      confirmPassword: data.confirmPassword,
    };

    const result = await adminChangeUserPassword(userId, passwordRequest);

    handleResponse(
      result,
      t('password.changeFailedFor', 'Failed to change password for {{userName}}', { userName }),
      t('password.changeSuccessFor', 'Password changed successfully for {{userName}}', { userName })
    );

    setIsSubmitting(false);

    if (result.result) {
      // Close modal on success
      setTimeout(() => {
        handleClose();
      }, 2000);
    }
  };

  return (
    <Modal show={show} onHide={handleClose} centered>
      <Modal.Header closeButton>
        <Modal.Title>
          <Key className="me-2" />
          {t('password.changePassword', 'Change Password')} - {userName}
        </Modal.Title>
      </Modal.Header>

      <Modal.Body>
        <AlertMessage success={success} initialErrorMessage={initialErrorMessage} errors={apiErrors} />

        <Form onSubmit={handleSubmit(onSubmit)}>
          <Form.Group className="mb-3">
            <Form.Label>{t('password.newPassword', 'New Password')}</Form.Label>
            <Form.Control
              type="password"
              placeholder={t('password.enterNewPassword', 'Enter new password')}
              isInvalid={!!errors.newPassword}
              {...register('newPassword', {
                required: t('validation.newPasswordRequired', 'New password is required'),
                minLength: {
                  value: 8,
                  message: t('validation.passwordMinLength', 'Password must be at least 8 characters')
                }
              })}
            />
            <Form.Control.Feedback type="invalid">
              {errors.newPassword?.message}
            </Form.Control.Feedback>
          </Form.Group>

          <Form.Group className="mb-4">
            <Form.Label>{t('password.confirmNewPassword', 'Confirm New Password')}</Form.Label>
            <Form.Control
              type="password"
              placeholder={t('password.confirmNewPasswordPlaceholder', 'Confirm your new password')}
              isInvalid={!!errors.confirmPassword}
              {...register('confirmPassword', {
                required: t('validation.confirmNewPassword', 'Please confirm the new password'),
                validate: (value) =>
                  value === newPassword || t('validation.passwordsDoNotMatch', 'Passwords do not match')
              })}
            />
            <Form.Control.Feedback type="invalid">
              {errors.confirmPassword?.message}
            </Form.Control.Feedback>
          </Form.Group>

          <div className="d-flex justify-content-end gap-2">
            <Button variant="outline-secondary" onClick={handleClose} disabled={isSubmitting}>
              <X className="me-1" />
              {t('common.cancel', 'Cancel')}
            </Button>
            <Button
              variant="warning"
              type="submit"
              disabled={isSubmitting}
            >
              {isSubmitting ? (
                <>
                  <Spinner
                    as="span"
                    animation="border"
                    size="sm"
                    role="status"
                    aria-hidden="true"
                    className="me-2"
                  />
                  {t('password.changing', 'Changing...')}
                </>
              ) : (
                <>
                  <Key className="me-1" />
                  {t('password.changePassword', 'Change Password')}
                </>
              )}
            </Button>
          </div>
        </Form>
      </Modal.Body>
    </Modal>
  );
};

export default AdminChangePasswordModal;