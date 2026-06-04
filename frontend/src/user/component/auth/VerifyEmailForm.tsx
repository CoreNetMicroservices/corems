import { useTranslation } from 'react-i18next';
import { Link, useSearchParams, useNavigate } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { Container, Row, Col, Spinner, Button } from "react-bootstrap";

import { VerifyEmailRequest, ResendVerificationRequest } from "@/user/model/Auth";
import { verifyEmail, resendVerification } from "@/user/store/AuthState";
import { useMessageState } from '@/common/utils/api/ApiResponseHandler';
import { AlertMessage } from '@/common/component/ApiResponseAlert';
import { ROUTE_PATHS } from '@/app/router/routes';

const VerifyEmailForm = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { success, initialErrorMessage, errors: apiErrors, handleResponse } = useMessageState();
  const [isResending, setIsResending] = useState(false);
  const [isVerifying, setIsVerifying] = useState(false);
  const [verificationComplete, setVerificationComplete] = useState(false);
  
  const token = searchParams.get('token');
  const email = searchParams.get('email');

  useEffect(() => {
    // Auto-verify if both email and token are present in URL
    if (email && token && !verificationComplete) {
      setIsVerifying(true);
      const verifyEmailData: VerifyEmailRequest = { email, token };
      
      verifyEmail(verifyEmailData).then((res) => {
        handleResponse(
          res,
          "Failed to verify email. Please check the token and try again.",
          "Email verified successfully! You can now sign in to your account."
        );

        setIsVerifying(false);
        setVerificationComplete(true);

        if (res.result) {
          setTimeout(() => {
            navigate(ROUTE_PATHS.LOGIN);
          }, 3000);
        }
      });
    }
  }, [email, token, verificationComplete, handleResponse, navigate]);

  const handleResendVerification = async () => {
    if (!email) {
      handleResponse(
        { result: false, response: null, errors: [{ reasonCode: 'missing.email', description: 'Email is required' }] },
        "Please enter your email address",
        ""
      );
      return;
    }

    setIsResending(true);
    const resendRequest: ResendVerificationRequest = {
      email,
      type: 'EMAIL',
    };

    const res = await resendVerification(resendRequest);
    handleResponse(
      res,
      "Failed to resend verification email. Please try again.",
      "Verification email sent! Please check your email for the new verification link."
    );
    setIsResending(false);
  };

  // Show loading state while verifying
  if (isVerifying) {
    return (
      <Container>
        <Row className="justify-content-center">
          <Col xs={12} lg={6}>
            <div className="text-center">
              <h2 className="display-5 fw-bold mb-4">{t('auth.verifyEmail', 'Verify Email')}</h2>
              <Spinner animation="border" role="status" className="mb-3">
                <span className="visually-hidden">Loading...</span>
              </Spinner>
              <p className="text-muted">
                {t('auth.verifyingEmail', 'Verifying your email address...')}
              </p>
            </div>
          </Col>
        </Row>
      </Container>
    );
  }

  // Show result after verification attempt
  return (
    <Container>
      <Row className="justify-content-center">
        <Col xs={12} lg={6}>
          <div className="mb-5 text-center">
            <h2 className="display-5 fw-bold">{t('auth.verifyEmail', 'Verify Email')}</h2>
          </div>

          <AlertMessage success={success} initialErrorMessage={initialErrorMessage} errors={apiErrors} />

          {success && (
            <div className="text-center mb-4">
              <p className="text-success">
                {t('auth.emailVerifiedSuccess', 'Your email has been successfully verified!')}
              </p>
              <p className="text-muted">
                {t('auth.redirectingToLogin', 'Redirecting to login page in a few seconds...')}
              </p>
            </div>
          )}

          {!success && verificationComplete && (
            <div className="text-center mb-4">
              <p className="text-muted mb-3">
                {t('auth.verificationFailed', 'Email verification failed. You can try resending the verification email.')}
              </p>
              <Button 
                variant="primary" 
                onClick={handleResendVerification}
                disabled={isResending}
                className="mb-3"
              >
                {isResending ? t('common.sending', 'Sending...') : t('auth.resendVerification', 'Resend Verification Email')}
              </Button>
            </div>
          )}

          <div className="text-center">
            <Link to={ROUTE_PATHS.LOGIN} className="text-decoration-none">
              {t('auth.backToLogin', 'Back to Login')}
            </Link>
          </div>
        </Col>
      </Row>
    </Container>
  );
};

export default VerifyEmailForm;