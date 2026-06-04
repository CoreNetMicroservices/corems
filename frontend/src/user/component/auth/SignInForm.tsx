import * as yup from 'yup';
import { Button, Col, FloatingLabel, Form, Row } from "react-bootstrap";
import { Link } from "react-router-dom";
import { yupResolver } from '@hookform/resolvers/yup';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';

import { signInUser } from "@/user/store/AuthState";
import { SignInUserRequest } from "@/user/model/Auth";
import { APP_ROUTES } from "@/app/router/routes";
import { useMessageState } from '@/common/utils/api/ApiResponseHandler';
import { AlertMessage } from '@/common/component/ApiResponseAlert';


const signInSchema = yup.object().shape({
  email: yup.string().email('Invalid email address').required('Email is required'),
  password: yup.string().required('Password is required'),
});


const SignInForm = () => {
  const { t } = useTranslation();
  const { success, initialErrorMessage, errors: apiErrors, handleResponse } = useMessageState();
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm({
    resolver: yupResolver(signInSchema),
  });


  const onSubmit = async (data: SignInUserRequest) => {
    const res = await signInUser(data);
    handleResponse(
      res,
      "Failed to sign in. Please check your credentials."
    );

    // reload on sign in success
    if (res.result === true) {
      window.location.href = APP_ROUTES.WELCOME;
    }
  };

  return (
    <Form noValidate onSubmit={handleSubmit(onSubmit)} className="needs-validation">
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
        controlId="floatingPassword"
        label={t('form.password', 'Password')}
        className="mb-3"
      >
        <Form.Control
          type="password"
          placeholder={t('form.password', 'Password')}
          size="lg"
          className="border-0 border-bottom rounded-0"
          required
          autoComplete="current-password"
          isInvalid={!!errors.password}
          {...register('password')}
        />
        <Form.Control.Feedback type="invalid">
          {errors.password?.message}
        </Form.Control.Feedback>
      </FloatingLabel>

      <AlertMessage success={success} initialErrorMessage={initialErrorMessage} errors={apiErrors} />

      <Row className="justify-content-center mb-3">
        <Col xs={{ span: 6, offset: 6 }} className="text-end">
          <Link to={APP_ROUTES.FORGOT_PASSWORD} className="link-secondary text-decoration-none">{t('auth.forgotPassword', 'Forgot password?')}</Link>
        </Col>
      </Row>
      <div className="d-grid mb-3">
        <Button size="lg" type="submit" disabled={isSubmitting} className="fs-6 py-2">{t('auth.signIn', 'Sign In')}</Button>
      </div>
    </Form>
  );
};

export default SignInForm;