import * as yup from 'yup';
import { Button, FloatingLabel, Form, Container, Row, Col } from "react-bootstrap";
import { useForm } from 'react-hook-form';
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams, useNavigate } from 'react-router-dom';
import { useEffect } from 'react';

import { ResetPasswordRequest } from "@/user/model/Auth";
import { resetPassword } from "@/user/store/AuthState";
import { useMessageState } from '@/common/utils/api/ApiResponseHandler';
import { AlertMessage } from '@/common/component/ApiResponseAlert';
import { ROUTE_PATHS } from '@/app/router/routes';

const resetPasswordSchema = yup.object().shape({
  email: yup.string().email('Invalid email address').required('Email is required'),
  newPassword: yup.string().min(8, 'Password must be at least 8 characters').required('Password is required'),
  confirmPassword: yup.string()
    .oneOf([yup.ref('newPassword')], 'Passwords must match')
    .required('Confirm password is required'),
});

const ResetPasswordForm = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { success, initialErrorMessage, errors: apiErrors, handleResponse } = useMessageState();
  
  const token = searchParams.get('token');
  const email = searchParams.get('email');
  console.log("refresh TOKEN",  token, email);

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm({
    resolver: yupResolver(resetPasswordSchema),
  });

  useEffect(() => {
    if (email) {
      setValue('email', email);
    }
  }, [email, setValue]);

  const onSubmit = async (data: Omit<ResetPasswordRequest, 'token'>) => {
    if (!token) {
      handleResponse(
        { result: false, response: null, errors: [{ reasonCode: 'invalid.token', description: 'Invalid or missing reset token' }] },
        "Invalid or missing reset token",
        ""
      );
      return;
    }

    const resetRequest: ResetPasswordRequest = {
      ...data,
      token,
    };

    const res = await resetPassword(resetRequest);
    handleResponse(
      res,
      "Failed to reset password. Please try again or request a new reset link.",
      "Password reset successfully! You can now sign in with your new password."
    );

    if (res.result) {
      setTimeout(() => {
        navigate(ROUTE_PATHS.LOGIN);
      }, 2000);
    }
  };

  if (!token) {
    return (
      <Container>
        <Row className="justify-content-center">
          <Col xs={12} lg={6}>
            <div className="text-center">
              <h2 className="display-5 fw-bold text-danger">{t('auth.invalidResetLink', 'Invalid Reset Link')}</h2>
              <p className="mb-4">
                {t('auth.invalidResetLinkDescription', 'This password reset link is invalid or has expired.')}
              </p>
              <Link to={ROUTE_PATHS.FORGOT_PASSWORD} className="btn btn-primary">
                {t('auth.requestNewLink', 'Request New Reset Link')}
              </Link>
            </div>
          </Col>
        </Row>
      </Container>
    );
  }

  return (
    <Container>
      <Row className="justify-content-center">
        <Col xs={12} lg={6}>
          <div className="mb-5 text-center">
            <h2 className="display-5 fw-bold">{t('auth.resetPassword', 'Reset Password')}</h2>
            <p className="m-0">
              {t('auth.resetPasswordDescription', 'Enter your new password below.')}
            </p>
          </div>

          <Form noValidate onSubmit={handleSubmit(onSubmit)} className="needs-validation">
            <AlertMessage success={success} initialErrorMessage={initialErrorMessage} errors={apiErrors} />

            <FloatingLabel
              controlId="floatingEmail"
              label={t('form.email', 'Email')}
              className="mb-3"
            >
              <Form.Control
                type="email"
                placeholder={t('form.enterEmail', 'Enter email')}
                size="lg"
                className="border-0 border-bottom rounded-0"
                autoComplete="email"
                isInvalid={!!errors.email}
                {...register('email')}
              />
              <Form.Control.Feedback type="invalid">
                {errors.email?.message}
              </Form.Control.Feedback>
            </FloatingLabel>

            <FloatingLabel
              controlId="floatingNewPassword"
              label={t('form.newPassword', 'New Password')}
              className="mb-3"
            >
              <Form.Control
                type="password"
                placeholder={t('form.newPassword', 'New Password')}
                size="lg"
                className="border-0 border-bottom rounded-0"
                autoComplete="new-password"
                isInvalid={!!errors.newPassword}
                {...register('newPassword')}
              />
              <Form.Control.Feedback type="invalid">
                {errors.newPassword?.message}
              </Form.Control.Feedback>
            </FloatingLabel>

            <FloatingLabel
              controlId="floatingConfirmPassword"
              label={t('form.confirmPassword', 'Confirm Password')}
              className="mb-3"
            >
              <Form.Control
                type="password"
                placeholder={t('form.confirmPassword', 'Confirm Password')}
                size="lg"
                className="border-0 border-bottom rounded-0"
                autoComplete="new-password"
                isInvalid={!!errors.confirmPassword}
                {...register('confirmPassword')}
              />
              <Form.Control.Feedback type="invalid">
                {errors.confirmPassword?.message}
              </Form.Control.Feedback>
            </FloatingLabel>

            <div className="d-grid mb-3">
              <Button size="lg" type="submit" disabled={isSubmitting} className="fs-6 py-2">
                {isSubmitting ? t('common.resetting', 'Resetting...') : t('auth.resetPassword', 'Reset Password')}
              </Button>
            </div>

            <div className="text-center">
              <Link to={ROUTE_PATHS.LOGIN} className="text-decoration-none">
                {t('auth.backToLogin', 'Back to Login')}
              </Link>
            </div>
          </Form>
        </Col>
      </Row>
    </Container>
  );
};

export default ResetPasswordForm;