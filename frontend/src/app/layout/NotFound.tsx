import { Link } from 'react-router-dom';
import { Row, Col, Container, Button } from 'react-bootstrap';
import { useTranslation } from 'react-i18next';

import AppLayout from '@/app/layout/AppLayout';
import { APP_ROUTES } from '@/app/router/routes';

const NotFound = () => {
  const { t } = useTranslation();

  return (
    <AppLayout>
      <Container className="d-flex flex-column align-items-center justify-content-center" >
        <Row className="text-center">
          <Col xs={12}>
            <h2 className="d-flex justify-content-center align-items-center gap-2 mb-2">
              <span className="display-1 fw-bold">4</span>
              <i className="bi bi-exclamation-circle-fill text-danger display-4"></i>
              <span className="display-1 fw-bold bsb-flip-h">4</span>
            </h2>
            <h3 className="h2 mb-4">{t('error.notFound', "Oops! You're lost.")}</h3>
            <Link to={APP_ROUTES.HOME}>
              <Button size="lg" type="submit" className="fs-6 p-3">{t('common.backToHome', 'Back To Home')}</Button>
            </Link>
          </Col>
        </Row>
      </Container>
    </AppLayout>
  );
};

export default NotFound;