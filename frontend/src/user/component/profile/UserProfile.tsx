import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useProfileState, getProfileInfo, updateProfileInfo } from '@/user/store/ProfileState';
import { resendVerification } from '@/user/store/AuthState';
import { ResendVerificationRequest } from '@/user/model/Auth';
import Button from 'react-bootstrap/Button';
import Form from 'react-bootstrap/Form';
import Container from 'react-bootstrap/Container';
import Spinner from 'react-bootstrap/Spinner';
import { Col, Row, Badge, InputGroup } from 'react-bootstrap';
import { Lock, CheckCircleFill, ExclamationCircleFill, Envelope, Telephone } from 'react-bootstrap-icons';
import ChangePasswordModal from './ChangePasswordModal';
import ProfileImageUpload from '@/user/component/shared/ProfileImageUpload';
import { useMessageState } from '@/common/utils/api/ApiResponseHandler';
import { AlertMessage } from '@/common/component/ApiResponseAlert';
import { ROUTE_PATHS } from '@/app/router/routes';

interface UserFormValues {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
}

const UserProfile = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const profileState = useProfileState();
  const user = profileState.user.get();
  const [showPasswordModal, setShowPasswordModal] = useState(false);
  const [isResendingEmail, setIsResendingEmail] = useState(false);
  const [isResendingPhone, setIsResendingPhone] = useState(false);
  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<UserFormValues>();
  const { success: updateSuccess, initialErrorMessage: updateInitialError, errors: updateErrors, handleResponse } = useMessageState();

  const onSubmit = async (data: UserFormValues) => {
    const result = await updateProfileInfo(data);
    handleResponse(
      result,
      'Failed to update user information.',
      'Your profile has been updated successfully.'
    );
  };

  const handleResendEmailVerification = async () => {
    if (!user?.email) return;
    
    setIsResendingEmail(true);
    const resendRequest: ResendVerificationRequest = {
      email: user.email,
      type: 'EMAIL',
    };

    const result = await resendVerification(resendRequest);
    handleResponse(
      result,
      'Failed to send verification email.',
      'Verification email sent! Please check your email.'
    );
    setIsResendingEmail(false);
  };

  const handleImageUpdate = async (imageUrl: string) => {
    const result = await updateProfileInfo({ imageUrl });
    if (result.result) {
      // Refresh profile data to show new image
      getProfileInfo();
    }
    return Promise.resolve();
  };

  const handleResendPhoneVerification = async () => {
    if (!user?.email || !user?.phoneNumber) return;
    
    setIsResendingPhone(true);
    const resendRequest: ResendVerificationRequest = {
      email: user.email,
      type: 'SMS',
    };

    const result = await resendVerification(resendRequest);
    
    if (result.result) {
      // Redirect to phone verification page with phone number and email as URL params
      const params = new URLSearchParams({
        phone: user.phoneNumber,
        email: user.email
      });
      navigate(`${ROUTE_PATHS.VERIFY_PHONE}?${params.toString()}`);
    } else {
      handleResponse(
        result,
        'Failed to send verification SMS.',
        'Verification SMS sent! Please check your phone.'
      );
    }
    
    setIsResendingPhone(false);
  };

  useEffect(() => {
    getProfileInfo().then(result => {
      if (result.result && result.response) {
        reset({
          firstName: result.response.firstName,
          lastName: result.response.lastName,
          email: result.response.email,
          phoneNumber: result.response.phoneNumber,
        });
      }
    });
  }, [reset]);

  if (!user) {
    return (
      <div className="d-flex justify-content-center align-items-center" style={{ minHeight: 200 }}>
        <Spinner animation="border" role="status" variant="primary">
          <span className="visually-hidden">{t('common.loading', 'Loading...')}</span>
        </Spinner>
      </div>
    );
  }

  return (
    <Container>
      <Row>
        <Col>
          <h2 className="mb-4 mt-3 text-center">{t('profile.title', 'Profile')}</h2>
          <ProfileImageUpload
            currentImageUrl={user.imageUrl}
            onImageUpdate={handleImageUpdate}
            size={96}
            className="mb-3"
          />
          <Form className="mb-3" onSubmit={handleSubmit(onSubmit)}>
            <Form.Group className="mb-3" controlId="firstName">
              <Form.Label>{t('form.firstName', 'First Name')}</Form.Label>
              <Form.Control
                type="text"
                isInvalid={!!errors.firstName}
                {...register('firstName', { required: 'First name is required' })}
              />
              <Form.Control.Feedback type="invalid">
                {errors.firstName?.message}
              </Form.Control.Feedback>
            </Form.Group>
            <Form.Group className="mb-3" controlId="lastName">
              <Form.Label>{t('form.lastName', 'Last Name')}</Form.Label>
              <Form.Control
                type="text"
                isInvalid={!!errors.lastName}
                {...register('lastName', { required: 'Last name is required' })}
              />
              <Form.Control.Feedback type="invalid">
                {errors.lastName?.message}
              </Form.Control.Feedback>
            </Form.Group>
            <Form.Group className="mb-3" controlId="email">
              <Form.Label className="d-flex align-items-center gap-2">
                <Envelope size={16} />
                {t('form.email', 'Email')}
                {user.emailVerified ? (
                  <Badge bg="success" className="d-flex align-items-center gap-1">
                    <CheckCircleFill size={12} />
                    {t('profile.verified', 'Verified')}
                  </Badge>
                ) : (
                  <Badge bg="warning" className="d-flex align-items-center gap-1">
                    <ExclamationCircleFill size={12} />
                    {t('profile.unverified', 'Unverified')}
                  </Badge>
                )}
              </Form.Label>
              <InputGroup>
                <Form.Control
                  type="email"
                  isInvalid={!!errors.email}
                  {...register('email', { required: 'Email is required' })}
                  disabled
                />
                {!user.emailVerified && (
                  <Button 
                    variant="outline-primary" 
                    onClick={handleResendEmailVerification}
                    disabled={isResendingEmail}
                    size="sm"
                  >
                    {isResendingEmail ? t('common.sending', 'Sending...') : t('profile.verify', 'Verify')}
                  </Button>
                )}
              </InputGroup>
              <Form.Control.Feedback type="invalid">
                {errors.email?.message}
              </Form.Control.Feedback>
            </Form.Group>
            <Form.Group className="mb-3" controlId="phoneNumber">
              <Form.Label className="d-flex align-items-center gap-2">
                <Telephone size={16} />
                {t('form.phoneNumber', 'Phone Number')}
                {user.phoneNumber && (
                  user.phoneVerified ? (
                    <Badge bg="success" className="d-flex align-items-center gap-1">
                      <CheckCircleFill size={12} />
                      {t('profile.verified', 'Verified')}
                    </Badge>
                  ) : (
                    <Badge bg="warning" className="d-flex align-items-center gap-1">
                      <ExclamationCircleFill size={12} />
                      {t('profile.unverified', 'Unverified')}
                    </Badge>
                  )
                )}
              </Form.Label>
              <InputGroup>
                <Form.Control
                  type="text"
                  isInvalid={!!errors.phoneNumber}
                  {...register('phoneNumber')}
                  placeholder="+123456789"
                />
                {user.phoneNumber && !user.phoneVerified && (
                  <Button 
                    variant="outline-primary" 
                    onClick={handleResendPhoneVerification}
                    disabled={isResendingPhone}
                    size="sm"
                  >
                    {isResendingPhone ? t('common.sending', 'Sending...') : t('profile.verify', 'Verify')}
                  </Button>
                )}
              </InputGroup>
              <Form.Control.Feedback type="invalid">
                {errors.phoneNumber?.message}
              </Form.Control.Feedback>
            </Form.Group>
            <div className="d-flex gap-2">
              <Button variant="primary" type="submit" disabled={isSubmitting}>
                {t('common.saveChanges', 'Save Changes')}
              </Button>
              <Button
                variant="outline-secondary"
                onClick={() => setShowPasswordModal(true)}
                disabled={isSubmitting}
              >
                <Lock className="me-1" size={16} />
                {t('profile.changePassword', 'Change Password')}
              </Button>
            </div>
          </Form>
          <AlertMessage success={updateSuccess} initialErrorMessage={updateInitialError} errors={updateErrors} />
        </Col>
        <Col md={4}>
          <h2 className="mb-4 mt-3 text-center">{t('profile.roles', 'Roles')}</h2>
          <ul className="list-group">
            {user.roles?.map((role) => (
              <li key={role} className="list-group-item text-center">
                {role}
              </li>
            ))}
          </ul>
        </Col>
      </Row>

      {/* Change Password Modal */}
      <ChangePasswordModal
        show={showPasswordModal}
        onHide={() => setShowPasswordModal(false)}
      />
    </Container>
  );
};

export default UserProfile;
