import { Container, Row, Col, Card, Button } from 'react-bootstrap';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useAuthState } from '@/user/store/AuthState';
import { ROUTE_PATHS } from '@/app/router/routes';

const WelcomePage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const authState = useAuthState();
  const user = authState.user.get();

  const quickLinks = [
    {
      icon: 'envelope-fill',
      title: t('welcome.communication', 'Communication'),
      description: t('welcome.communicationDesc', 'Send emails and SMS messages'),
      path: ROUTE_PATHS.COMMUNICATION,
      color: 'primary'
    },
    {
      icon: 'folder-fill',
      title: t('welcome.documents', 'Documents'),
      description: t('welcome.documentsDesc', 'Upload and manage files'),
      path: ROUTE_PATHS.DOCUMENTS,
      color: 'success'
    },
    {
      icon: 'person-circle',
      title: t('welcome.profile', 'Your Profile'),
      description: t('welcome.profileDesc', 'Update your account settings'),
      path: ROUTE_PATHS.USER_PROFILE,
      color: 'info'
    },
    {
      icon: 'book-fill',
      title: t('welcome.documentation', 'Documentation'),
      description: t('welcome.documentationDesc', 'Learn about CoreMS features'),
      path: '/docs/introduction',
      color: 'warning'
    },
  ];

  return (
    <Container className="py-5">
      <Row className="mb-5">
        <Col>
          <h1 className="display-4 mb-3">
            {t('welcome.title', 'Welcome back, {{name}}!', { name: user?.firstName || 'User' })}
          </h1>
          <p className="lead text-muted">
            {t('welcome.subtitle', 'Here are some quick links to get you started')}
          </p>
        </Col>
      </Row>

      <Row className="g-4">
        {quickLinks.map((link, index) => (
          <Col key={index} md={6} lg={3}>
            <Card 
              className="h-100 border-0 shadow-sm hover-shadow" 
              style={{ cursor: 'pointer', transition: 'all 0.3s' }}
              onClick={() => navigate(link.path)}
            >
              <Card.Body className="text-center p-4">
                <div className={`mb-3 text-${link.color}`}>
                  <i className={`bi bi-${link.icon}`} style={{ fontSize: '3rem' }}></i>
                </div>
                <Card.Title className="h5 mb-2">{link.title}</Card.Title>
                <Card.Text className="text-muted small">
                  {link.description}
                </Card.Text>
              </Card.Body>
            </Card>
          </Col>
        ))}
      </Row>

      <Row className="mt-5">
        <Col lg={8}>
          <Card className="border-0 shadow-sm">
            <Card.Body className="p-4">
              <h3 className="h5 mb-3">
                <i className="bi bi-lightbulb-fill text-warning me-2"></i>
                {t('welcome.gettingStarted', 'Getting Started')}
              </h3>
              <ul className="mb-0">
                <li className="mb-2">
                  {t('welcome.tip1', 'Explore the Communication service to send emails and SMS')}
                </li>
                <li className="mb-2">
                  {t('welcome.tip2', 'Upload documents to the Document Management service')}
                </li>
                <li className="mb-2">
                  {t('welcome.tip3', 'Check out the documentation to learn about all features')}
                </li>
                <li>
                  {t('welcome.tip4', 'Update your profile to personalize your experience')}
                </li>
              </ul>
            </Card.Body>
          </Card>
        </Col>

        <Col lg={4}>
          <Card className="border-0 shadow-sm bg-primary text-white">
            <Card.Body className="p-4">
              <h3 className="h5 mb-3">
                <i className="bi bi-github me-2"></i>
                {t('welcome.openSource', 'Open Source')}
              </h3>
              <p className="small mb-3">
                {t('welcome.openSourceDesc', 'CoreMS is open source. Contribute on GitHub!')}
              </p>
              <Button 
                variant="light" 
                size="sm"
                href="https://github.com/CoreWebMicroservices/corems-project"
                target="_blank"
              >
                {t('welcome.viewGithub', 'View on GitHub')}
              </Button>
            </Card.Body>
          </Card>
        </Col>
      </Row>
    </Container>
  );
};

export default WelcomePage;
