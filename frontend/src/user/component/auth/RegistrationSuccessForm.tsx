import { Container, Row, Col, Card } from "react-bootstrap";
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { ROUTE_PATHS } from '@/app/router/routes';

interface RegistrationSuccessFormProps {
  email: string;
  hasPhoneNumber: boolean;
}

const RegistrationSuccessForm: React.FC<RegistrationSuccessFormProps> = ({ 
  email, 
  hasPhoneNumber 
}) => {
  const { t } = useTranslation();

  return (
    <Container>
      <Row className="justify-content-center">
        <Col xs={12} lg={8}>
          <Card className="border-0 shadow-sm">
            <Card.Body className="p-5">
              <div className="text-center mb-4">
                <div className="text-success mb-3">
                  <i className="bi bi-check-circle-fill" style={{ fontSize: '3rem' }}></i>
                </div>
                <h2 className="display-6 fw-bold text-success">
                  {t('auth.registrationSuccess', 'Registration Successful!')}
                </h2>
                <p className="text-muted">
                  {t('auth.accountCreated', 'Your account has been created successfully.')}
                </p>
              </div>

              <div className="mb-4">
                <h5 className="fw-bold mb-3">
                  {t('auth.nextSteps', 'Next Steps:')}
                </h5>
                
                <div className="d-flex align-items-start mb-3">
                  <div className="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center me-3" 
                       style={{ width: '24px', height: '24px', fontSize: '0.8rem' }}>
                    1
                  </div>
                  <div>
                    <strong>{t('auth.verifyEmailStep', 'Verify your email address')}</strong>
                    <p className="text-muted mb-0 small">
                      {t('auth.verifyEmailDescription', 'We\'ve sent a verification link to')} <strong>{email}</strong>
                    </p>
                  </div>
                </div>

                {hasPhoneNumber && (
                  <div className="d-flex align-items-start mb-3">
                    <div className="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center me-3" 
                         style={{ width: '24px', height: '24px', fontSize: '0.8rem' }}>
                      2
                    </div>
                    <div>
                      <strong>{t('auth.verifyPhoneStep', 'Verify your phone number')}</strong>
                      <p className="text-muted mb-0 small">
                        {t('auth.verifyPhoneDescription', 'We\'ve sent a 6-digit code to your phone number')}
                      </p>
                    </div>
                  </div>
                )}

                <div className="d-flex align-items-start">
                  <div className="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center me-3" 
                       style={{ width: '24px', height: '24px', fontSize: '0.8rem' }}>
                    {hasPhoneNumber ? '3' : '2'}
                  </div>
                  <div>
                    <strong>{t('auth.signInStep', 'Sign in to your account')}</strong>
                    <p className="text-muted mb-0 small">
                      {t('auth.signInDescription', 'Once verified, you can sign in and start using your account')}
                    </p>
                  </div>
                </div>
              </div>

              <div className="bg-light rounded p-3 mb-4">
                <h6 className="fw-bold mb-2">
                  <i className="bi bi-info-circle me-2"></i>
                  {t('auth.verificationTips', 'Verification Tips:')}
                </h6>
                <ul className="mb-0 small text-muted">
                  <li>{t('auth.checkSpamFolder', 'Check your spam/junk folder if you don\'t see the email')}</li>
                  <li>{t('auth.emailValidFor', 'Email verification link is valid for 24 hours')}</li>
                  {hasPhoneNumber && (
                    <li>{t('auth.smsValidFor', 'SMS verification code is valid for 2 hours')}</li>
                  )}
                </ul>
              </div>

              <div className="d-grid gap-2">
                <Link to={ROUTE_PATHS.LOGIN} className="btn btn-primary btn-lg">
                  {t('auth.proceedToLogin', 'Proceed to Login')}
                </Link>
                <Link to={ROUTE_PATHS.VERIFY_EMAIL} className="btn btn-outline-secondary">
                  {t('auth.verifyEmailNow', 'Verify Email Now')}
                </Link>
                {hasPhoneNumber && (
                  <Link to={ROUTE_PATHS.VERIFY_PHONE} className="btn btn-outline-secondary">
                    {t('auth.verifyPhoneNow', 'Verify Phone Now')}
                  </Link>
                )}
              </div>
            </Card.Body>
          </Card>
        </Col>
      </Row>
    </Container>
  );
};

export default RegistrationSuccessForm;