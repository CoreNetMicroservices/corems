import * as yup from 'yup';
import { Button, Col, FloatingLabel, Form, Row } from "react-bootstrap";
import { useForm } from 'react-hook-form';
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from 'react-i18next';
import { useState } from 'react';

import { SignUpUserRequest } from "@/user/model/Auth";
import { signUpUser } from "@/user/store/AuthState";
import { useMessageState } from '@/common/utils/api/ApiResponseHandler';
import { AlertMessage } from '@/common/component/ApiResponseAlert';
import RegistrationSuccessForm from './RegistrationSuccessForm';

const signUpUserSchema = yup.object().shape({
  email: yup.string().email('Invalid email address').required('Email is required'),
  firstName: yup.string().required('First name is required'),
  lastName: yup.string().required('Last name is required'),
  password: yup.string().required('Password is required'),
  confirmPassword: yup.string().required('Password is required'),
});


interface SignUpFormProps {
  onSignedUp: () => void;
}

const SignUpForm: React.FC<SignUpFormProps> = ({ onSignedUp }) => {
  const { t } = useTranslation();
  const { success, initialErrorMessage, errors: apiErrors, handleResponse } = useMessageState();
  const [registrationData, setRegistrationData] = useState<SignUpUserRequest | null>(null);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm({
    resolver: yupResolver(signUpUserSchema),
  });

  const onSubmit = async (data: SignUpUserRequest) => {
    const res = await signUpUser(data);
    handleResponse(
      res,
      "Failed to create account. Please check the information and try again.",
      "Account created successfully! Please check your email to verify your account."
    );

    if (res.result === true) {
      setRegistrationData(data);
      onSignedUp();
    }
  };

  // Show success page after successful registration
  if (registrationData) {
    return (
      <RegistrationSuccessForm 
        email={registrationData.email}
        hasPhoneNumber={false}
      />
    );
  }

  return (
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
      </FloatingLabel>
      <Row>
        <Col md={6}>
          <FloatingLabel
            controlId="firstName"
            label={t('form.firstName', 'First Name')}
            className="mb-3"
          >
            <Form.Control
              type="input"
              placeholder={t('form.firstName', 'First Name')}
              size="lg"
              className="border-0 border-bottom rounded-0"
              autoComplete="first-name"
              isInvalid={!!errors.firstName}
              {...register('firstName')}
            />
          </FloatingLabel>
        </Col>
        <Col md={6}>
          <FloatingLabel
            controlId="lastName"
            label={t('form.lastName', 'Last Name')}
            className="mb-3"
          >
            <Form.Control
              type="input"
              placeholder={t('form.lastName', 'Last Name')}
              size="lg"
              className="border-0 border-bottom rounded-0"
              autoComplete="last-name"
              isInvalid={!!errors.lastName}
              {...register('lastName')}
            />
          </FloatingLabel>
        </Col>
      </Row>

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
          autoComplete="new-password"
          isInvalid={!!errors.password}
          {...register('password')}
        />
      </FloatingLabel>
      <FloatingLabel
        controlId="floatingConfirmPassword"
        label={t('form.confirmPassword', 'Confirm Password')}
        className="mb-3"
      >
        <Form.Control
          type="password"
          placeholder={t('form.password', 'Password')}
          size="lg"
          className="border-0 border-bottom rounded-0"
          autoComplete="new-password"
          isInvalid={!!errors.confirmPassword}
          {...register('confirmPassword')}
        />
      </FloatingLabel>
      <div className="d-grid mb-3">
        <Button size="lg" type="submit" disabled={isSubmitting} className="fs-6 py-2">
          {t('auth.register', 'Register new account')}
        </Button>
      </div>
    </Form>
  );
};

export default SignUpForm;