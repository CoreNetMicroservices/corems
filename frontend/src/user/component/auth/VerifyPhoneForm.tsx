import * as yup from 'yup';
import { Button, FloatingLabel, Form, Container, Row, Col } from "react-bootstrap";
import { useForm } from 'react-hook-form';
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams, useNavigate } from 'react-router-dom';
import { useEffect, useState } from 'react';

import { VerifyPhoneRequest, ResendVerificationRequest } from "@/user/model/Auth";
import { verifyPhone, resendVerification } from "@/user/store/AuthState";
import { useMessageState } from '@/common/utils/api/ApiResponseHandler';
import { AlertMessage } from '@/common/component/ApiResponseAlert';
import { ROUTE_PATHS } from '@/app/router/routes';

const verifyPhoneSchema = yup.object().shape({
  phoneNumber: yup.string()
    .matches(/^\+\d{1,20}$/, 'Phone number must be in E.164 format (e.g., +15551234567)')
    .required('Phone number is required'),
  code: yup.string()
    .matches(/^\d{6}$/, 'Code must be 6 digits')
    .required('Verification code is required'),
});

const VerifyPhoneForm = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { success, initialErrorMessage, errors: apiErrors, handleResponse } = useMessageState();
  const [isResending, setIsResending] = useState(false);
  
  const phoneNumber = searchParams.get('phone');
  const email = searchParams.get('email');

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm({
    resolver: yupResolver(verifyPhoneSchema),
  });

  useEffect(() => {
    if (phoneNumber) {
      setValue('phoneNumber', phoneNumber);
    }
  }, [phoneNumber, setValue]);

  const onSubmit = async (data: VerifyPhoneRequest) => {
    const res = await verifyPhone(data);
    handleResponse(
      res,
      "Failed to verify phone number. Please check the code and try again.",
      "Phone number verified successfully!"
    );

    if (res.result) {
      setTimeout(() => {
        // If user came from profile page, redirect back to profile
        // Otherwise redirect to login (for new registrations)
        if (email) {
          navigate(ROUTE_PATHS.USER_PROFILE);
        } else {
          navigate(ROUTE_PATHS.LOGIN);
        }
      }, 2000);
    }
  };

  const handleResendVerification = async () => {
    if (!email) {
      handleResponse(
        { result: false, response: null, errors: [{ reasonCode: 'missing.email', description: 'Email is required for resending SMS' }] },
        "Email is required to resend SMS verification",
        ""
      );
      return;
    }

    setIsResending(true);
    const resendRequest: ResendVerificationRequest = {
      email,
      type: 'SMS',
    };

    const res = await resendVerification(resendRequest);
    handleResponse(
      res,
      "Failed to resend verification SMS. Please try again.",
      "Verification SMS sent! Please check your phone for the new verification code."
    );
    setIsResending(false);
  };

  return (
    <Container>
      <Row className="justify-content-center">
        <Col xs={12} lg={6}>
          <div className="mb-5 text-center">
            <h2 className="display-5 fw-bold">{t('auth.verifyPhone', 'Verify Phone Number')}</h2>
            <p className="m-0">
              {t('auth.verifyPhoneDescription', 'Enter the 6-digit code sent to your phone number to verify your account.')}
            </p>
          </div>

          <Form noValidate onSubmit={handleSubmit(onSubmit)} className="needs-validation">
            <AlertMessage success={success} initialErrorMessage={initialErrorMessage} errors={apiErrors} />

            <FloatingLabel
              controlId="floatingPhoneNumber"
              label={t('form.phoneNumber', 'Phone Number')}
              className="mb-3"
            >
              <Form.Control
                type="tel"
                placeholder={t('form.enterPhoneNumber', 'Enter phone number (e.g., +15551234567)')}
                size="lg"
                className="border-0 border-bottom rounded-0"
                autoComplete="tel"
                isInvalid={!!errors.phoneNumber}
                {...register('phoneNumber')}
              />
              <Form.Control.Feedback type="invalid">
                {errors.phoneNumber?.message}
              </Form.Control.Feedback>
            </FloatingLabel>

            <FloatingLabel
              controlId="floatingCode"
              label={t('form.verificationCode', 'Verification Code')}
              className="mb-3"
            >
              <Form.Control
                type="text"
                placeholder={t('form.enterCode', 'Enter 6-digit code')}
                size="lg"
                className="border-0 border-bottom rounded-0"
                maxLength={6}
                isInvalid={!!errors.code}
                {...register('code')}
              />
              <Form.Control.Feedback type="invalid">
                {errors.code?.message}
              </Form.Control.Feedback>
            </FloatingLabel>

            <div className="d-grid mb-3">
              <Button size="lg" type="submit" disabled={isSubmitting} className="fs-6 py-2">
                {isSubmitting ? t('common.verifying', 'Verifying...') : t('auth.verifyPhone', 'Verify Phone Number')}
              </Button>
            </div>

            {email && (
              <div className="text-center mb-3">
                <Button 
                  variant="link" 
                  onClick={handleResendVerification}
                  disabled={isResending}
                  className="text-decoration-none p-0"
                >
                  {isResending ? t('common.sending', 'Sending...') : t('auth.resendVerificationSMS', 'Resend Verification SMS')}
                </Button>
              </div>
            )}

            <div className="text-center">
              {email ? (
                <Link to={ROUTE_PATHS.USER_PROFILE} className="text-decoration-none">
                  {t('profile.backToProfile', 'Back to Profile')}
                </Link>
              ) : (
                <Link to={ROUTE_PATHS.LOGIN} className="text-decoration-none">
                  {t('auth.backToLogin', 'Back to Login')}
                </Link>
              )}
            </div>
          </Form>
        </Col>
      </Row>
    </Container>
  );
};

export default VerifyPhoneForm;