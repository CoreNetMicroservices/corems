import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { Container, Row, Col, Card, Button, Form, Spinner, ButtonGroup, Dropdown } from 'react-bootstrap';
import { ArrowLeft, Lock, X } from 'react-bootstrap-icons';
import { useTranslation } from 'react-i18next';
import { User, AuthProvider } from '@/user/model/User';
import { useUserState, getUserById, updateUserInfo } from '@/user/store/UserState';
import { useMessageState } from '@/common/utils/api/ApiResponseHandler';
import { AlertMessage } from '@/common/component/ApiResponseAlert';
import AdminChangePasswordModal from './AdminChangePasswordModal';
import ProfileImageUpload from '@/user/component/shared/ProfileImageUpload';
import { AppRoles } from '@/common/AppRoles';
import { ROUTE_PATHS } from '@/app/router/routes';


interface UserFormValues {
  firstName: string;
  lastName: string;
  email: string;
  roles: string[];
  phoneNumber?: string;
}

const UserEdit = () => {
  const { t } = useTranslation();
  const { userId } = useParams<{ userId: string }>();
  const navigate = useNavigate();
  const userState = useUserState();
  const selectedUser = userState.selectedUser.get();
  const [isLoading, setIsLoading] = useState(false);
  const [showPasswordModal, setShowPasswordModal] = useState(false);

  const { register, handleSubmit, reset, watch, setValue, formState: { errors, isSubmitting } } = useForm<UserFormValues>();
  const { success: updateSuccess, initialErrorMessage: updateInitialError, errors: updateErrors, handleResponse } = useMessageState();

  const roles = watch('roles') || [];

  useEffect(() => {
    if (userId) {
      const loadUser = async () => {
        setIsLoading(true);
        const result = await getUserById(userId);
        setIsLoading(false);

        if (result.result && result.response) {
          reset({
            firstName: result.response.firstName,
            lastName: result.response.lastName,
            email: result.response.email,
            roles: result.response.roles ? [...result.response.roles] : [],
            phoneNumber: result.response.phoneNumber,
          });
        }
      };
      loadUser();
    }
  }, [userId, reset]);

  const onSubmit = async (data: UserFormValues) => {
    if (!userId) return;

    const userData: User = {
      userId: userId,
      provider: selectedUser?.provider || AuthProvider.local,
      firstName: data.firstName,
      lastName: data.lastName,
      email: data.email,
      phoneNumber: data.phoneNumber,
      roles: data.roles,
      imageUrl: selectedUser?.imageUrl,
      createdAt: selectedUser?.createdAt,
      updatedAt: new Date().toISOString(),
      lastLoginAt: selectedUser?.lastLoginAt,
    };

    const result = await updateUserInfo(userId, userData);
    handleResponse(
      result,
      t('user.updateFailed', 'Failed to update user information.'),
      t('user.updateSuccess', 'User information has been updated successfully.')
    );
  };

  const handleImageUpdate = async (imageUrl: string) => {
    if (!userId) return;

    const result = await updateUserInfo(userId, { imageUrl } as User);
    if (result.result) {
      await getUserById(userId);
    }
    return Promise.resolve();
  };

  const addRole = (role: string) => {
    const currentRoles = roles || [];
    if (!currentRoles.includes(role)) {
      const newRoles = [...currentRoles, role];
      setValue('roles', newRoles);
    }
  };

  const removeRole = (role: string) => {
    const currentRoles = roles || [];
    const newRoles = currentRoles.filter(r => r !== role);
    setValue('roles', newRoles);
  };

  // Get available roles from AppRoles enum that are not already assigned
  const getAvailableRoles = () => {
    const allRoles = Object.values(AppRoles);
    const currentRoles = roles || [];
    return allRoles.filter(role => !currentRoles.includes(role));
  };



  if (isLoading) {
    return (
      <Container>
        <div className="d-flex justify-content-center align-items-center" style={{ minHeight: 400 }}>
          <Spinner animation="border" role="status" variant="primary">
            <span className="visually-hidden">{t('common.loading', 'Loading...')}</span>
          </Spinner>
        </div>
      </Container>
    );
  }

  if (!selectedUser) {
    return (
      <Container>
        <div className="text-center mt-5">
          <h4>{t('user.notFound', 'User not found')}</h4>
          <Button variant="primary" onClick={() => navigate(ROUTE_PATHS.USERS_LIST)}>
            <ArrowLeft className="me-2" />
            {t('user.backToUsers', 'Back to Users')}
          </Button>
        </div>
      </Container>
    );
  }

  return (
    <>
      <Row>
        <Col md={8}>
          <h2 className="mb-4 mt-3 text-center">{t('user.editUser', 'Edit User')}</h2>

          <ProfileImageUpload
            currentImageUrl={selectedUser.imageUrl}
            onImageUpdate={handleImageUpdate}
            ownerUserId={userId}
            size={96}
            className="mb-3"
          />

          <Form className="mb-3" onSubmit={handleSubmit(onSubmit)}>
            <Form.Group className="mb-3" controlId="firstName">
              <Form.Label>{t('form.firstName', 'First Name')}</Form.Label>
              <Form.Control
                type="text"
                isInvalid={!!errors.firstName}
                {...register('firstName', { required: t('validation.firstNameRequired', 'First name is required') })}
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
                {...register('lastName', { required: t('validation.lastNameRequired', 'Last name is required') })}
              />
              <Form.Control.Feedback type="invalid">
                {errors.lastName?.message}
              </Form.Control.Feedback>
            </Form.Group>

            <Form.Group className="mb-3" controlId="email">
              <Form.Label>{t('form.email', 'Email')}</Form.Label>
              <Form.Control
                type="email"
                isInvalid={!!errors.email}
                {...register('email', {
                  required: t('validation.emailRequired', 'Email is required'),
                  pattern: {
                    value: /^\S+@\S+$/i,
                    message: t('validation.invalidEmail', 'Invalid email format')
                  }
                })}
                disabled
              />
              <Form.Control.Feedback type="invalid">
                {errors.email?.message}
              </Form.Control.Feedback>
            </Form.Group>

            <Form.Group className="mb-3" controlId="phoneNumber">
              <Form.Label>{t('form.phoneNumber', 'Phone Number')}</Form.Label>
              <Form.Control
                type="text"
                isInvalid={!!errors.phoneNumber}
                {...register('phoneNumber')}
                placeholder="+123456789"
              />
              <Form.Control.Feedback type="invalid">
                {errors.phoneNumber?.message}
              </Form.Control.Feedback>
            </Form.Group>

            <hr />
            <Form.Group className="mb-3" controlId="roles">
              <Form.Label>{t('user.roles', 'Roles')}</Form.Label>
              <div className="mb-3">
                {roles.length > 0 ? (
                  <div className="d-flex flex-wrap gap-2">
                    {roles.map((role) => (
                      <ButtonGroup key={role} size="sm">
                        <Button variant="outline-primary" disabled>
                          {role}
                        </Button>
                        <Button
                          variant="outline-danger"
                          onClick={(e) => {
                            e.preventDefault();
                            e.stopPropagation();
                            removeRole(role);
                          }}
                          title={t('user.removeRole', 'Remove role: {{role}}', { role })}
                        >
                          <X size={14} />
                        </Button>
                      </ButtonGroup>
                    ))}
                  </div>
                ) : (
                  <div className="text-muted fst-italic mb-2">{t('user.noRolesAssigned', 'No roles assigned')}</div>
                )}
              </div>

              {/* Add Role Dropdown */}
              {getAvailableRoles().length > 0 && (
                <Dropdown>
                  <Dropdown.Toggle size="sm" variant="outline-success">
                    {t('user.addRole', 'Add Role')}
                  </Dropdown.Toggle>
                  <Dropdown.Menu>
                    {getAvailableRoles().map((role) => (
                      <Dropdown.Item
                        key={role}
                        onClick={(e) => {
                          e.preventDefault();
                          addRole(role);
                        }}
                      >
                        {role}
                      </Dropdown.Item>
                    ))}
                  </Dropdown.Menu>
                </Dropdown>
              )}
            </Form.Group>
            <hr />

            <div className="d-flex gap-2">
              <Button variant="primary" type="submit" disabled={isSubmitting}>
                {isSubmitting ? t('common.saving', 'Saving...') : t('common.saveChanges', 'Save Changes')}
              </Button>
              <Button
                variant="outline-secondary"
                onClick={() => setShowPasswordModal(true)}
                disabled={isSubmitting}
              >
                <Lock className="me-1" size={16} />
                {t('password.changePassword', 'Change Password')}
              </Button>
            </div>
          </Form>

          <AlertMessage success={updateSuccess} initialErrorMessage={updateInitialError} errors={updateErrors} />

        </Col>

        <Col md={4}>
          <h2 className="mb-4 mt-3 text-center">{t('user.accountInfo', 'Account Info')}</h2>

          {/* Account Details Card */}
          <Card>
            <Card.Body>
              <p className="mb-2"><strong>{t('user.userId', 'User ID')}:</strong> <span className="text-muted">{selectedUser.userId}</span></p>
              <p className="mb-2"><strong>{t('user.provider', 'Provider')}:</strong> <span className="text-muted">{selectedUser.provider}</span></p>
              {selectedUser.createdAt && (
                <p className="mb-2"><strong>{t('user.created', 'Created')}:</strong> <span className="text-muted">{new Date(selectedUser.createdAt).toLocaleDateString()}</span></p>
              )}
              {selectedUser.lastLoginAt && (
                <p className="mb-0"><strong>{t('user.lastLogin', 'Last Login')}:</strong> <span className="text-muted">{new Date(selectedUser.lastLoginAt).toLocaleDateString()}</span></p>
              )}
            </Card.Body>
          </Card>
        </Col>
      </Row>

      {/* Admin Change Password Modal */}
      <AdminChangePasswordModal
        show={showPasswordModal}
        onHide={() => setShowPasswordModal(false)}
        userId={userId || ''}
        userName={`${selectedUser.firstName} ${selectedUser.lastName}`}
      />
    </>
  );
};

export default UserEdit;