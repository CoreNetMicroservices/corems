import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { Container, Row, Col, Card, Button, Form, ButtonGroup, Dropdown } from 'react-bootstrap';
import { X } from 'react-bootstrap-icons';
import { useTranslation } from 'react-i18next';
import { AuthProvider } from '@/user/model/User';
import { createUser } from '@/user/store/UserState';
import { useMessageState } from '@/common/utils/api/ApiResponseHandler';
import { AlertMessage } from '@/common/component/ApiResponseAlert';
import { AppRoles } from '@/common/AppRoles';
import Breadcrumb from '@/common/component/Breadcrumb';
import { ROUTE_PATHS } from '@/app/router/routes';

interface UserFormValues {
  firstName: string;
  lastName: string;
  email: string;
  roles: string[];
  phoneNumber?: string;
}

const UserAdd = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();


  const { register, handleSubmit, watch, setValue, formState: { errors, isSubmitting } } = useForm<UserFormValues>({
    defaultValues: {
      firstName: '',
      lastName: '',
      email: '',
      roles: []
    }
  });
  const { success: createSuccess, initialErrorMessage: createInitialError, errors: createErrors, handleResponse } = useMessageState();

  const roles = watch('roles') || [];

  const onSubmit = async (data: UserFormValues) => {
    const userData = {
      firstName: data.firstName,
      lastName: data.lastName,
      email: data.email,
      phoneNumber: data.phoneNumber,
      roles: data.roles,
      provider: AuthProvider.local
    };

    const result = await createUser(userData);
    handleResponse(
      result,
      'Failed to create user.',
      'User has been created successfully!'
    );

    if (result.result) {
      // Redirect to users list after successful creation
      setTimeout(() => {
        navigate(ROUTE_PATHS.USERS_LIST);
      }, 2000);
    }
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

  return (
    <Container>
      {/* Breadcrumb Navigation */}
      <Breadcrumb
        items={[
          { label: t('user.allUsers', 'All Users'), href: ROUTE_PATHS.USERS_LIST },
          { label: t('user.addNewUser', 'Add New User'), active: true }
        ]}
      />

      <Row>
        <Col md={8}>
          <h2 className="mb-4 mt-3 text-center">{t('user.addNewUser', 'Add New User')}</h2>

          {/* Default Avatar Placeholder */}
          <div className="mb-3 text-center">
            <div style={{
              width: 96,
              height: 96,
              borderRadius: '50%',
              background: '#e9ecef',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              border: '2px dashed var(--bs-secondary)',
              fontSize: 32,
              fontWeight: 'bold',
              color: '#6c757d',
              margin: '0 auto'
            }}>
              +
            </div>
            <small className="text-muted mt-1 d-block">{t('user.defaultAvatarGenerated', 'Avatar will be generated based on initials')}</small>
          </div>

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

            <div className="d-flex gap-2">
              <Button variant="primary" type="submit" disabled={isSubmitting}>
                {isSubmitting ? t('common.creating', 'Creating...') : t('user.createUser', 'Create User')}
              </Button>
              <Button variant="outline-secondary" onClick={() => navigate(ROUTE_PATHS.USERS_LIST)}>
                {t('common.cancel', 'Cancel')}
              </Button>
            </div>
          </Form>

          <AlertMessage success={createSuccess} initialErrorMessage={createInitialError} errors={createErrors} />

        </Col>

        <Col md={4}>
          <h2 className="mb-4 mt-3 text-center">{t('user.information', 'Information')}</h2>

          {/* Information Card */}
          <Card>
            <Card.Body>
              <h6>{t('user.accountCreation', 'Account Creation')}</h6>
              <p className="mb-2 text-muted">• {t('user.accountCreationInfo.localAuth', 'User will be created with local authentication')}</p>
              <p className="mb-2 text-muted">• {t('user.accountCreationInfo.defaultPassword', 'A temporary password will be generated')}</p>
              <p className="mb-2 text-muted">• {t('user.accountCreationInfo.setPassword', 'User can set their own password later')}</p>
              <p className="mb-0 text-muted">• {t('user.accountCreationInfo.emailVerification', 'Email verification will be required')}</p>
            </Card.Body>
          </Card>
        </Col>
      </Row>
    </Container>
  );
};

export default UserAdd;