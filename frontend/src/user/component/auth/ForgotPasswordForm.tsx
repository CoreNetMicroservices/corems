import * as yup from 'yup';
import { Button, FloatingLabel, Form, Container, Row, Col } from "react-bootstrap";
import { useForm } from 'react-hook-form';
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { ForgotPasswordRequest } from "@/user/model/Auth";
import { forgotPassword } from "@/user/store/AuthState";
import { useMessageState } from '@/common/utils/api/ApiResponseHandler';
import { AlertMessage } from '@/common/component/ApiResponseAlert';
import { ROUTE_PATHS } from '@/app/router/routes';

const forgotPasswordSchema = yup.object().shape({
  email: yup.string().email('Invalid email address').required('Email is required'),
});

const ForgotPasswordForm = () => {
  const { t } = useTranslation();
  const { success, initialErrorMessage, errors: apiErrors, handleResponse } = useMessageState();
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm({
    resolver: yupResolver(forgotPasswordSchema),
  });

  const onSubmit = async (data: ForgotPasswordRequest) => {
    const res = await forgotPassword(data);
    handleResponse(
      res,
      "Failed to send password reset email. Please try again.",
      "Password reset email sent! Please check your email for instructions."
    );
  };

  return (
    <Container>
      <Row className="justify-content-center">
        <Col xs={12} lg={6}>
          <div className="mb-5 text-center">
            <h2 className="display-5 fw-bold">{t('auth.forgotPassword', 'Forgot Password')}</h2>
            <p className="m-0">
              {t('auth.forgotPasswordDescription', 'Enter your email address and we\'ll send you a link to reset your password.')}
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

            <div className="d-grid mb-3">
              <Button size="lg" type="submit" disabled={isSubmitting} className="fs-6 py-2">
                {isSubmitting ? t('common.sending', 'Sending...') : t('auth.sendResetLink', 'Send Reset Link')}
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

export default ForgotPasswordForm;