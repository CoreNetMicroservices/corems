import { useState } from 'react';
import { Container, Row, Col, Button, Card } from 'react-bootstrap';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import SignUpForm from '@/user/component/auth/SignUpForm';
import { GOOGLE_AUTH_URL, GITHUB_AUTH_URL, LINKEDIN_AUTH_URL } from "@/user/config";
import { useAuthState } from '@/user/store/AuthState';

const HomePage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const authState = useAuthState();
  const [showRegister, setShowRegister] = useState(false);

  const isAuthenticated = authState.isAuthenticated.get();
  const user = authState.user.get();

  const handleSignedUp = () => {
    setShowRegister(false);
  };

  return (
    <div className="home-page">
      {/* Hero Section */}
      <section className="hero-section py-5 mb-5" style={{ background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)' }}>
        <Container>
          <Row className="align-items-center min-vh-50">
            <Col lg={7} className="text-white">
              {isAuthenticated && user ? (
                <>
                  <h1 className="display-3 fw-bold mb-4">
                    {t('home.hero.welcomeBack', 'Welcome Back, {{name}}!', { name: user.firstName })}
                  </h1>
                  <p className="lead mb-4 fs-4">
                    {t('home.hero.welcomeMessage', 'This applications is just a frontend showcase. Explore the features of it or jump right into building your next great application.')}
                  </p>
                  <div className="d-flex gap-3 flex-wrap">
                    <Button 
                      size="lg" 
                      variant="light" 
                      className="px-4 py-3"
                      onClick={() => navigate('/welcome')}
                    >
                      {t('home.hero.explore', 'Explore Features')}
                    </Button>
                    <Button 
                      size="lg" 
                      variant="outline-light" 
                      className="px-4 py-3"
                      onClick={() => navigate('/docs/introduction')}
                    >
                      <i className="bi bi-book me-2"></i>
                      {t('home.hero.documentation', 'Documentation')}
                    </Button>
                  </div>
                </>
              ) : (
                <>
                  <h1 className="display-3 fw-bold mb-2">
                    {t('home.hero.title', 'Production-Ready Microservices.')}
                  </h1>
                  <h2 className="display-5 fw-normal mb-4">
                    {t('home.hero.subtitle', 'Just Add Your Business Logic.')}
                  </h2>
                  
                  {/* Key Benefits */}
                  <div className="mb-4">
                    <div className="d-flex align-items-center mb-2">
                      <i className="bi bi-check-circle-fill text-white me-2 fs-5"></i>
                      <span className="fs-6">{t('home.hero.benefit1', 'Flexible microservices open source architecture')}</span>
                    </div>
                    <div className="d-flex align-items-center mb-2">
                      <i className="bi bi-check-circle-fill text-white me-2 fs-5"></i>
                      <span className="fs-6">{t('home.hero.benefit2', '5 Backend Services + React Frontend')}</span>
                    </div>
                    <div className="d-flex align-items-center mb-2">
                      <i className="bi bi-check-circle-fill text-white me-2 fs-5"></i>
                      <span className="fs-6">{t('home.hero.benefit3', 'Common ecosystem with logging, observability and security out of the box')}</span>
                    </div>
                    <div className="d-flex align-items-center mb-2">
                      <i className="bi bi-check-circle-fill text-white me-2 fs-5"></i>
                      <span className="fs-6">{t('home.hero.benefit4', 'Ready Docker + Terraform + CI/CD for your system')}</span>
                    </div>
                  </div>

                  <div className="d-flex gap-3 flex-wrap">
                    <Button 
                      size="lg" 
                      variant="light" 
                      className="px-4 py-3"
                      onClick={() => setShowRegister(true)}
                    >
                      {t('home.hero.getStarted', 'Try for Free')}
                    </Button>
                    <Button 
                      size="lg" 
                      variant="outline-light" 
                      className="px-4 py-3"
                      onClick={() => navigate('/docs/introduction')}
                    >
                      <i className="bi bi-book me-2"></i>
                      {t('home.hero.documentation', 'Documentation')}
                    </Button>
                    <Button 
                      size="lg" 
                      variant="outline-light" 
                      className="px-4 py-3"
                      href="https://github.com/CoreWebMicroservices/corems-project"
                      target="_blank"
                    >
                      <i className="bi bi-github me-2"></i>
                      {t('home.hero.viewGithub', 'View on GitHub')}
                    </Button>
                  </div>
                </>
              )}
            </Col>
            <Col lg={5} className="mt-4 mt-lg-0">
              {!isAuthenticated && showRegister ? (
                <Card className="shadow-lg border-0">
                  <Card.Body className="p-4">
                    <div className="d-flex justify-content-between align-items-center mb-3">
                      <h4 className="mb-0">{t('auth.signUp', 'Sign Up')}</h4>
                      <Button 
                        variant="link" 
                        className="text-decoration-none p-0"
                        onClick={() => setShowRegister(false)}
                      >
                        <i className="bi bi-x-lg"></i>
                      </Button>
                    </div>
                    <SignUpForm onSignedUp={handleSignedUp} />
                    <div className="text-center my-3">
                      <small className="text-muted">{t('common.or', 'or')}</small>
                    </div>
                    <div className="d-flex gap-2 flex-column">
                      <a href={GOOGLE_AUTH_URL} className="btn btn-outline-primary btn-sm d-flex align-items-center justify-content-center">
                        <i className="bi bi-google me-2"></i>
                        {t('auth.continueGoogle', 'Google')}
                      </a>
                      <a href={GITHUB_AUTH_URL} className="btn btn-outline-primary btn-sm d-flex align-items-center justify-content-center">
                        <i className="bi bi-github me-2"></i>
                        {t('auth.continueGithub', 'GitHub')}
                      </a>
                      <a href={LINKEDIN_AUTH_URL} className="btn btn-outline-primary btn-sm d-flex align-items-center justify-content-center">
                        <i className="bi bi-linkedin me-2"></i>
                        {t('auth.continueLinkedin', 'LinkedIn')}
                      </a>
                    </div>
                  </Card.Body>
                </Card>
              ) : (
                <Card className="shadow-lg border-0 bg-white bg-opacity-10 text-white">
                  <Card.Body className="p-4">
                    <h5 className="mb-3">{t('home.hero.quickStart', 'Quick Start')}</h5>
                    <pre className="bg-dark bg-opacity-50 p-3 rounded text-white-50 small">
                      <code>
{`git clone https://github.com/
  CoreWebMicroservices/
  corems-project.git
cd corems-project
./setup.sh init-all
./setup.sh start-all`}
                      </code>
                    </pre>
                  </Card.Body>
                </Card>
              )}
            </Col>
          </Row>
        </Container>
      </section>

      {/* Features Section */}
      <Container className="my-5 py-5">
        <div className="text-center mb-5">
          <h2 className="display-5 fw-bold mb-3">
            {t('home.features.title', 'Everything You Need to Launch')}
          </h2>
          <p className="lead text-muted">
            {t('home.features.subtitle', 'Production-grade services in the Base Package')}
          </p>
        </div>

        {/* Base Package - Blue Section */}
        <div className="mb-5">
          <div className="d-flex align-items-center mb-4">
            <div className="bg-primary rounded-circle p-2 me-3" style={{ width: '40px', height: '40px' }}>
              <i className="bi bi-box-seam text-white fs-5"></i>
            </div>
            <div>
              <h3 className="mb-0">{t('home.features.basePackage', 'Base Package')}</h3>
              <p className="text-muted mb-0 small">{t('home.features.basePackageDesc', 'Complete backend microservices + React frontend modules')}</p>
            </div>
          </div>
          
          <Row className="g-4">
            {/* Backend Services */}
            <Col md={6} lg={4}>
              <Card className="h-100 border-primary border-2 shadow-sm bg-body">
                <Card.Body className="p-4">
                  <div className="mb-3">
                    <i className="bi bi-people-fill fs-1 text-primary"></i>
                  </div>
                  <h5 className="fw-bold mb-3">{t('home.features.userManagement', 'User Management')}</h5>
                  <p className="text-muted">
                    {t('home.features.userManagementDesc', 'Complete authentication system with JWT, OAuth2 (Google, GitHub, LinkedIn), role-based access control, and user profiles.')}
                  </p>
                </Card.Body>
              </Card>
            </Col>
            <Col md={6} lg={4}>
              <Card className="h-100 border-primary border-2 shadow-sm bg-body">
                <Card.Body className="p-4">
                  <div className="mb-3">
                    <i className="bi bi-envelope-fill fs-1 text-primary"></i>
                  </div>
                  <h5 className="fw-bold mb-3">{t('home.features.communication', 'Communication Hub')}</h5>
                  <p className="text-muted">
                    {t('home.features.communicationDesc', 'Multi-channel messaging with email, SMS, and push notifications. Delivery tracking and status monitoring included.')}
                  </p>
                </Card.Body>
              </Card>
            </Col>
            <Col md={6} lg={4}>
              <Card className="h-100 border-primary border-2 shadow-sm bg-body">
                <Card.Body className="p-4">
                  <div className="mb-3">
                    <i className="bi bi-file-earmark-text-fill fs-1 text-primary"></i>
                  </div>
                  <h5 className="fw-bold mb-3">{t('home.features.documents', 'Document Management')}</h5>
                  <p className="text-muted">
                    {t('home.features.documentsDesc', 'S3-compatible storage with MinIO. Upload, organize, and secure your files with built-in access control.')}
                  </p>
                </Card.Body>
              </Card>
            </Col>
            <Col md={6} lg={4}>
              <Card className="h-100 border-primary border-2 shadow-sm bg-body">
                <Card.Body className="p-4">
                  <div className="mb-3">
                    <i className="bi bi-translate fs-1 text-primary"></i>
                  </div>
                  <h5 className="fw-bold mb-3">{t('home.features.translation', 'Translation Service')}</h5>
                  <p className="text-muted">
                    {t('home.features.translationDesc', 'Multi-language support with dynamic translation management. Add and update translations without redeploying your application.')}
                  </p>
                </Card.Body>
              </Card>
            </Col>
            <Col md={6} lg={4}>
              <Card className="h-100 border-primary border-2 shadow-sm bg-body">
                <Card.Body className="p-4">
                  <div className="mb-3">
                    <i className="bi bi-file-code-fill fs-1 text-primary"></i>
                  </div>
                  <h5 className="fw-bold mb-3">{t('home.features.templates', 'Template Engine')}</h5>
                  <p className="text-muted">
                    {t('home.features.templatesDesc', 'Handlebars-powered templates for emails, documents, and reports. Version control and dynamic rendering included.')}
                  </p>
                </Card.Body>
              </Card>
            </Col>
            <Col md={6} lg={4}>
              <Card className="h-100 border-primary border-2 shadow-sm bg-body">
                <Card.Body className="p-4">
                  <div className="mb-3">
                    <i className="bi bi-layout-text-window-reverse fs-1 text-primary"></i>
                  </div>
                  <h5 className="fw-bold mb-3">{t('home.features.frontend', 'React Frontend Modules')}</h5>
                  <p className="text-muted">
                    {t('home.features.frontendDesc', 'Modular React + TypeScript components with Vite, React Router, Hookstate, and React Bootstrap. Ready-to-use UI for all services.')}
                  </p>
                </Card.Body>
              </Card>
            </Col>
          </Row>
        </div>

        {/* Additional Packages - Green Section */}
        <div>
          <div className="d-flex align-items-center mb-4">
            <div className="bg-success rounded-circle p-2 me-3" style={{ width: '40px', height: '40px' }}>
              <i className="bi bi-plus-circle text-white fs-5"></i>
            </div>
            <div>
              <h3 className="mb-0">{t('home.features.additionalPackages', 'Additional Packages')}</h3>
              <p className="text-muted mb-0 small">{t('home.features.additionalPackagesDesc', 'Extend your application with specialized features')}</p>
            </div>
          </div>
          
          <Row className="g-4">
            <Col md={6} lg={3}>
              <Card className="h-100 border-success border-2 shadow-sm bg-body">
                <Card.Body className="p-4 text-center">
                  <div className="mb-3">
                    <i className="bi bi-cloud-upload-fill fs-1 text-success"></i>
                  </div>
                  <h5 className="fw-bold mb-2">{t('home.features.cloudInfra', 'Cloud Infrastructure')}</h5>
                  <p className="text-muted small mb-0">
                    {t('home.features.cloudInfraDesc', 'Terraform modules for AWS deployment with auto-scaling and monitoring')}
                  </p>
                </Card.Body>
              </Card>
            </Col>
            <Col md={6} lg={3}>
              <Card className="h-100 border-success border-2 shadow-sm bg-body">
                <Card.Body className="p-4 text-center">
                  <div className="mb-3">
                    <i className="bi bi-credit-card-fill fs-1 text-success"></i>
                  </div>
                  <h5 className="fw-bold mb-2">{t('home.features.payments', 'Payments')}</h5>
                  <p className="text-muted small mb-0">
                    {t('home.features.paymentsDesc', 'Stripe, Vipps, Klarna and more payment gateway integrations')}
                  </p>
                </Card.Body>
              </Card>
            </Col>
            <Col md={6} lg={3}>
              <Card className="h-100 border-success border-2 shadow-sm bg-body">
                <Card.Body className="p-4 text-center">
                  <div className="mb-3">
                    <i className="bi bi-receipt-cutoff fs-1 text-success"></i>
                  </div>
                  <h5 className="fw-bold mb-2">{t('home.features.billing', 'Billing')}</h5>
                  <p className="text-muted small mb-0">
                    {t('home.features.billingDesc', 'Subscription management, invoicing, and recurring payments')}
                  </p>
                </Card.Body>
              </Card>
            </Col>
            <Col md={6} lg={3}>
              <Card className="h-100 border-success border-2 shadow-sm bg-body">
                <Card.Body className="p-4 text-center">
                  <div className="mb-3">
                    <i className="bi bi-chat-dots-fill fs-1 text-success"></i>
                  </div>
                  <h5 className="fw-bold mb-2">{t('home.features.customerChat', 'Customer Chat')}</h5>
                  <p className="text-muted small mb-0">
                    {t('home.features.customerChatDesc', 'Real-time messaging and customer support system')}
                  </p>
                </Card.Body>
              </Card>
            </Col>
            <Col md={6} lg={3}>
              <Card className="h-100 border-success border-2 shadow-sm bg-body">
                <Card.Body className="p-4 text-center">
                  <div className="mb-3">
                    <i className="bi bi-people-fill fs-1 text-success"></i>
                  </div>
                  <h5 className="fw-bold mb-2">{t('home.features.clientManagement', 'Client Management')}</h5>
                  <p className="text-muted small mb-0">
                    {t('home.features.clientManagementDesc', 'CRM features for managing customer relationships')}
                  </p>
                </Card.Body>
              </Card>
            </Col>
            <Col md={6} lg={3}>
              <Card className="h-100 border-success border-2 shadow-sm bg-body">
                <Card.Body className="p-4 text-center">
                  <div className="mb-3">
                    <i className="bi bi-clipboard-check-fill fs-1 text-success"></i>
                  </div>
                  <h5 className="fw-bold mb-2">{t('home.features.forms', 'Dynamic Forms')}</h5>
                  <p className="text-muted small mb-0">
                    {t('home.features.formsDesc', 'Build surveys and custom forms with validation')}
                  </p>
                </Card.Body>
              </Card>
            </Col>
            <Col md={6} lg={3}>
              <Card className="h-100 border-success border-2 shadow-sm bg-body">
                <Card.Body className="p-4 text-center">
                  <div className="mb-3">
                    <i className="bi bi-calendar-event-fill fs-1 text-success"></i>
                  </div>
                  <h5 className="fw-bold mb-2">{t('home.features.scheduling', 'Scheduling')}</h5>
                  <p className="text-muted small mb-0">
                    {t('home.features.schedulingDesc', 'Appointment booking and calendar management')}
                  </p>
                </Card.Body>
              </Card>
            </Col>
            <Col md={6} lg={3}>
              <Card className="h-100 border-success border-2 shadow-sm bg-body">
                <Card.Body className="p-4 text-center">
                  <div className="mb-3">
                    <i className="bi bi-three-dots fs-1 text-success"></i>
                  </div>
                  <h5 className="fw-bold mb-2">{t('home.features.more', 'And More...')}</h5>
                  <p className="text-muted small mb-0">
                    {t('home.features.moreDesc', 'Extensible architecture for custom packages')}
                  </p>
                </Card.Body>
              </Card>
            </Col>
          </Row>
        </div>
      </Container>

      {/* Clients/Use Cases Section */}
      <section className="bg-body-secondary py-5">
        <Container className="my-5">
          <div className="text-center mb-5">
            <h2 className="display-5 fw-bold mb-3">
              {t('home.clients.title', 'What Can You Build?')}
            </h2>
            <p className="lead text-muted">
              {t('home.clients.subtitle', 'Real-world applications powered by Core Microservices packages')}
            </p>
          </div>

          <Row className="g-4">
            {/* SaaS Customer Portal */}
            <Col lg={6}>
              <Card className="h-100 border-0 shadow-sm hover-shadow" style={{ transition: 'all 0.3s' }}>
                <Card.Body className="p-4">
                  <div className="d-flex align-items-start mb-3">
                    <div className="me-3">
                      <i className="bi bi-building fs-1 text-primary"></i>
                    </div>
                    <div className="flex-grow-1">
                      <h4 className="fw-bold mb-2">
                        {t('home.clients.saas.title', 'SaaS Customer Portal')}
                      </h4>
                      <div className="mb-3">
                        <span className="badge bg-primary me-2">Base Package</span>
                      </div>
                    </div>
                  </div>
                  <p className="text-muted mb-3">
                    {t('home.clients.saas.description', 'Multi-tenant customer dashboard with complete user management, document sharing, and automated communications.')}
                  </p>
                  <ul className="list-unstyled mb-3">
                    <li className="mb-2">
                      <i className="bi bi-check-circle-fill text-success me-2"></i>
                      {t('home.clients.saas.feature1', 'OAuth2 authentication & role management')}
                    </li>
                    <li className="mb-2">
                      <i className="bi bi-check-circle-fill text-success me-2"></i>
                      {t('home.clients.saas.feature2', 'Document sharing & version control')}
                    </li>
                    <li className="mb-2">
                      <i className="bi bi-check-circle-fill text-success me-2"></i>
                      {t('home.clients.saas.feature3', 'Email notifications with templates')}
                    </li>
                    <li className="mb-2">
                      <i className="bi bi-check-circle-fill text-success me-2"></i>
                      {t('home.clients.saas.feature4', 'Multi-language support for global customers')}
                    </li>
                  </ul>
                  <Button 
                    variant="outline-primary" 
                    size="sm"
                    className="w-100"
                    onClick={() => {
                      if (isAuthenticated) {
                        navigate('/welcome');
                      } else {
                        setShowRegister(true);
                        window.scrollTo({ top: 0, behavior: 'smooth' });
                      }
                    }}
                  >
                    <i className="bi bi-arrow-right-circle me-2"></i>
                    {t('home.clients.viewDemo', 'View Live Demo')}
                  </Button>
                </Card.Body>
              </Card>
            </Col>

            {/* E-Commerce Platform */}
            <Col lg={6}>
              <Card className="h-100 border-0 shadow-sm hover-shadow" style={{ transition: 'all 0.3s' }}>
                <Card.Body className="p-4">
                  <div className="d-flex align-items-start mb-3">
                    <div className="me-3">
                      <i className="bi bi-cart-fill fs-1 text-success"></i>
                    </div>
                    <div className="flex-grow-1">
                      <h4 className="fw-bold mb-2">
                        {t('home.clients.ecommerce.title', 'E-Commerce Platform')}
                      </h4>
                      <div className="mb-3">
                        <span className="badge bg-primary me-2">Base</span>
                        <span className="badge bg-success me-2">Payments</span>
                        <span className="badge bg-info me-2">Online Store</span>
                        <span className="badge bg-warning">Customer Chat</span>
                      </div>
                    </div>
                  </div>
                  <p className="text-muted mb-3">
                    {t('home.clients.ecommerce.description', 'Full-featured online store with payment processing, order management, and real-time customer support.')}
                  </p>
                  <ul className="list-unstyled mb-3">
                    <li className="mb-2">
                      <i className="bi bi-check-circle-fill text-success me-2"></i>
                      {t('home.clients.ecommerce.feature1', 'Product catalog with search & filters')}
                    </li>
                    <li className="mb-2">
                      <i className="bi bi-check-circle-fill text-success me-2"></i>
                      {t('home.clients.ecommerce.feature2', 'Integrated payment gateways (Stripe, Vipps, Kalarna)')}
                    </li>
                    <li className="mb-2">
                      <i className="bi bi-check-circle-fill text-success me-2"></i>
                      {t('home.clients.ecommerce.feature3', 'Live customer support chat')}
                    </li>
                    <li className="mb-2">
                      <i className="bi bi-check-circle-fill text-success me-2"></i>
                      {t('home.clients.ecommerce.feature4', 'Order tracking & email notifications')}
                    </li>
                  </ul>
                  <Button 
                    variant="outline-success" 
                    size="sm"
                    className="w-100"
                    disabled
                  >
                    <i className="bi bi-hourglass-split me-2"></i>
                    {t('home.clients.comingSoon', 'Coming Soon')}
                  </Button>
                </Card.Body>
              </Card>
            </Col>

            {/* Home Food Delivery Subscription */}
            <Col lg={6}>
              <Card className="h-100 border-0 shadow-sm hover-shadow" style={{ transition: 'all 0.3s' }}>
                <Card.Body className="p-4">
                  <div className="d-flex align-items-start mb-3">
                    <div className="me-3">
                      <i className="bi bi-basket-fill fs-1 text-warning"></i>
                    </div>
                    <div className="flex-grow-1">
                      <h4 className="fw-bold mb-2">
                        {t('home.clients.foodDelivery.title', 'Home Food Delivery Subscription')}
                      </h4>
                      <div className="mb-3">
                        <span className="badge bg-primary me-2">Base</span>
                        <span className="badge bg-success me-2">Billing</span>
                        <span className="badge bg-success">Payments</span>
                      </div>
                    </div>
                  </div>
                  <p className="text-muted mb-3">
                    {t('home.clients.foodDelivery.description', 'Subscription-based meal delivery service with personalized plans, recurring billing, and automated notifications.')}
                  </p>
                  <ul className="list-unstyled mb-3">
                    <li className="mb-2">
                      <i className="bi bi-check-circle-fill text-success me-2"></i>
                      {t('home.clients.foodDelivery.feature1', 'Weekly meal plan subscriptions')}
                    </li>
                    <li className="mb-2">
                      <i className="bi bi-check-circle-fill text-success me-2"></i>
                      {t('home.clients.foodDelivery.feature2', 'Dietary preferences & profiles')}
                    </li>
                    <li className="mb-2">
                      <i className="bi bi-check-circle-fill text-success me-2"></i>
                      {t('home.clients.foodDelivery.feature3', 'Recipe cards & nutritional documents')}
                    </li>
                    <li className="mb-2">
                      <i className="bi bi-check-circle-fill text-success me-2"></i>
                      {t('home.clients.foodDelivery.feature4', 'Recurring billing & delivery notifications')}
                    </li>
                  </ul>
                  <Button 
                    variant="outline-warning" 
                    size="sm"
                    className="w-100"
                    disabled
                  >
                    <i className="bi bi-hourglass-split me-2"></i>
                    {t('home.clients.comingSoon', 'Coming Soon')}
                  </Button>
                </Card.Body>
              </Card>
            </Col>

            {/* Healthcare Patient Portal */}
            <Col lg={6}>
              <Card className="h-100 border-0 shadow-sm hover-shadow" style={{ transition: 'all 0.3s' }}>
                <Card.Body className="p-4">
                  <div className="d-flex align-items-start mb-3">
                    <div className="me-3">
                      <i className="bi bi-heart-pulse-fill fs-1 text-danger"></i>
                    </div>
                    <div className="flex-grow-1">
                      <h4 className="fw-bold mb-2">
                        {t('home.clients.healthcare.title', 'Healthcare Patient Portal')}
                      </h4>
                      <div className="mb-3">
                        <span className="badge bg-primary me-2">Base</span>
                        <span className="badge bg-success me-2">Billing</span>
                        <span className="badge bg-info me-2">Client Management</span>
                        <span className="badge bg-warning">Customer Chat</span>
                      </div>
                    </div>
                  </div>
                  <p className="text-muted mb-3">
                    {t('home.clients.healthcare.description', 'HIPAA-compliant patient portal with secure document storage, appointment management, billing, and telehealth chat.')}
                  </p>
                  <ul className="list-unstyled mb-3">
                    <li className="mb-2">
                      <i className="bi bi-check-circle-fill text-success me-2"></i>
                      {t('home.clients.healthcare.feature1', 'Secure patient registration & profiles')}
                    </li>
                    <li className="mb-2">
                      <i className="bi bi-check-circle-fill text-success me-2"></i>
                      {t('home.clients.healthcare.feature2', 'Medical document storage (HIPAA-compliant)')}
                    </li>
                    <li className="mb-2">
                      <i className="bi bi-check-circle-fill text-success me-2"></i>
                      {t('home.clients.healthcare.feature3', 'Secure chat with healthcare providers')}
                    </li>
                    <li className="mb-2">
                      <i className="bi bi-check-circle-fill text-success me-2"></i>
                      {t('home.clients.healthcare.feature4', 'Appointment reminders & billing statements')}
                    </li>
                  </ul>
                  <Button 
                    variant="outline-danger" 
                    size="sm"
                    className="w-100"
                    disabled
                  >
                    <i className="bi bi-hourglass-split me-2"></i>
                    {t('home.clients.comingSoon', 'Coming Soon')}
                  </Button>
                </Card.Body>
              </Card>
            </Col>
          </Row>

          <div className="text-center mt-5">
            <p className="lead text-muted mb-3">
              {t('home.clients.cta', 'These are just examples. With Core Microservices, you can build any application you imagine.')}
            </p>
            <Button 
              variant="primary" 
              size="lg"
              onClick={() => navigate('/docs/introduction')}
            >
              {t('home.clients.learnMore', 'Explore All Packages')}
            </Button>
          </div>
        </Container>
      </section>

      {/* Who Is This For Section */}
      <Container className="my-5 py-5">
        <div className="text-center mb-5">
          <h2 className="display-5 fw-bold mb-3">
            {t('home.audience.title', 'Built For Modern Teams')}
          </h2>
        </div>
          <Row className="g-4">
            <Col md={6} lg={3}>
              <div className="text-center">
                <div className="mb-3">
                  <i className="bi bi-rocket-takeoff fs-1 text-primary"></i>
                </div>
                <h5 className="fw-bold">{t('home.audience.startups', 'Startup Founders')}</h5>
                <p className="text-muted">
                  {t('home.audience.startupsDesc', 'Launch your MVP in weeks, not months')}
                </p>
              </div>
            </Col>
            <Col md={6} lg={3}>
              <div className="text-center">
                <div className="mb-3">
                  <i className="bi bi-building fs-1 text-primary"></i>
                </div>
                <h5 className="fw-bold">{t('home.audience.companies', 'Small Companies')}</h5>
                <p className="text-muted">
                  {t('home.audience.companiesDesc', 'Enterprise features without enterprise costs')}
                </p>
              </div>
            </Col>
            <Col md={6} lg={3}>
              <div className="text-center">
                <div className="mb-3">
                  <i className="bi bi-code-slash fs-1 text-primary"></i>
                </div>
                <h5 className="fw-bold">{t('home.audience.developers', 'Developers')}</h5>
                <p className="text-muted">
                  {t('home.audience.developersDesc', 'Build side projects with production quality')}
                </p>
              </div>
            </Col>
            <Col md={6} lg={3}>
              <div className="text-center">
                <div className="mb-3">
                  <i className="bi bi-mortarboard fs-1 text-primary"></i>
                </div>
                <h5 className="fw-bold">{t('home.audience.students', 'Students')}</h5>
                <p className="text-muted">
                  {t('home.audience.studentsDesc', 'Learn enterprise architecture patterns')}
                </p>
              </div>
            </Col>
          </Row>
        </Container>

      {/* Benefits Section */}
      <section className="bg-body-secondary py-5">
        <Container className="my-5">
          <Row className="align-items-center">
          <Col lg={6} className="mb-4 mb-lg-0">
            <h2 className="display-5 fw-bold mb-4">
              {t('home.benefits.title', 'Why Choose Core Microservices?')}
            </h2>
            <div className="mb-4">
              <div className="d-flex mb-3">
                <div className="me-3">
                  <i className="bi bi-check-circle-fill fs-4 text-success"></i>
                </div>
                <div>
                  <h5 className="fw-bold">{t('home.benefits.saveTime', 'Deploy in Minutes, Not Months')}</h5>
                  <p className="text-muted">
                    {t('home.benefits.saveTimeDesc', 'Pre-configured CI/CD pipelines, Docker containers, and Terraform scripts. Push to production on day one.')}
                  </p>
                </div>
              </div>
              <div className="d-flex mb-3">
                <div className="me-3">
                  <i className="bi bi-check-circle-fill fs-4 text-success"></i>
                </div>
                <div>
                  <h5 className="fw-bold">{t('home.benefits.production', 'Battle-Tested & Production-Ready')}</h5>
                  <p className="text-muted">
                    {t('home.benefits.productionDesc', 'Enterprise security, monitoring with Prometheus, distributed tracing, and auto-scaling built in.')}
                  </p>
                </div>
              </div>
              <div className="d-flex mb-3">
                <div className="me-3">
                  <i className="bi bi-check-circle-fill fs-4 text-success"></i>
                </div>
                <div>
                  <h5 className="fw-bold">{t('home.benefits.cloud', 'Multi-Cloud Ready')}</h5>
                  <p className="text-muted">
                    {t('home.benefits.cloudDesc', 'Deploy to AWS, Azure, or GCP with included Terraform modules. Kubernetes manifests and Helm charts included.')}
                  </p>
                </div>
              </div>
              <div className="d-flex mb-3">
                <div className="me-3">
                  <i className="bi bi-check-circle-fill fs-4 text-success"></i>
                </div>
                <div>
                  <h5 className="fw-bold">{t('home.benefits.documentation', 'Complete Documentation & Training')}</h5>
                  <p className="text-muted">
                    {t('home.benefits.documentationDesc', 'Step-by-step guides, API documentation, video tutorials, and live demo applications.')}
                  </p>
                </div>
              </div>
              <div className="d-flex mb-3">
                <div className="me-3">
                  <i className="bi bi-check-circle-fill fs-4 text-success"></i>
                </div>
                <div>
                  <h5 className="fw-bold">{t('home.benefits.cicd', 'Automated CI/CD Pipelines')}</h5>
                  <p className="text-muted">
                    {t('home.benefits.cicdDesc', 'GitHub Actions workflows for testing, building, and deploying. Automated security scanning and quality gates.')}
                  </p>
                </div>
              </div>
              <div className="d-flex mb-3">
                <div className="me-3">
                  <i className="bi bi-check-circle-fill fs-4 text-success"></i>
                </div>
                <div>
                  <h5 className="fw-bold">{t('home.benefits.free', 'Free & Open Source')}</h5>
                  <p className="text-muted">
                    {t('home.benefits.freeDesc', 'Apache 2.0 license. Use commercially without restrictions. Active community support.')}
                  </p>
                </div>
              </div>
            </div>
          </Col>
          <Col lg={6}>
            <Card className="border-0 bg-body">
              <Card.Body className="p-4">
                <h4 className="fw-bold mb-4">{t('home.benefits.techStack', 'Modern Tech Stack')}</h4>
                <Row className="g-3">
                  <Col xs={6}>
                    <div className="p-3 bg-body-secondary rounded">
                      <i className="bi bi-cup-hot-fill me-2 text-primary"></i>
                      <strong>Java 25</strong>
                    </div>
                  </Col>
                  <Col xs={6}>
                    <div className="p-3 bg-body-secondary rounded">
                      <i className="bi bi-leaf-fill me-2 text-success"></i>
                      <strong>Spring Boot 4.0</strong>
                    </div>
                  </Col>
                  <Col xs={6}>
                    <div className="p-3 bg-body-secondary rounded">
                      <i className="bi bi-database-fill me-2 text-info"></i>
                      <strong>PostgreSQL</strong>
                    </div>
                  </Col>
                  <Col xs={6}>
                    <div className="p-3 bg-body-secondary rounded">
                      <i className="bi bi-box-fill me-2 text-primary"></i>
                      <strong>Docker</strong>
                    </div>
                  </Col>
                  <Col xs={6}>
                    <div className="p-3 bg-body-secondary rounded">
                      <i className="bi bi-react me-2 text-info"></i>
                      <strong>React 18+</strong>
                    </div>
                  </Col>
                  <Col xs={6}>
                    <div className="p-3 bg-body-secondary rounded">
                      <i className="bi bi-code-square me-2 text-primary"></i>
                      <strong>TypeScript</strong>
                    </div>
                  </Col>
                  <Col xs={6}>
                    <div className="p-3 bg-body-secondary rounded">
                      <i className="bi bi-hdd-rack-fill me-2 text-warning"></i>
                      <strong>RabbitMQ</strong>
                    </div>
                  </Col>
                  <Col xs={6}>
                    <div className="p-3 bg-body-secondary rounded">
                      <i className="bi bi-shield-check me-2 text-success"></i>
                      <strong>JWT Auth</strong>
                    </div>
                  </Col>
                  <Col xs={6}>
                    <div className="p-3 bg-body-secondary rounded">
                      <i className="bi bi-cloud-fill me-2 text-info"></i>
                      <strong>Terraform</strong>
                    </div>
                  </Col>
                  <Col xs={6}>
                    <div className="p-3 bg-body-secondary rounded">
                      <i className="bi bi-gear-fill me-2 text-primary"></i>
                      <strong>Kubernetes</strong>
                    </div>
                  </Col>
                  <Col xs={6}>
                    <div className="p-3 bg-body-secondary rounded">
                      <i className="bi bi-graph-up me-2 text-success"></i>
                      <strong>Prometheus</strong>
                    </div>
                  </Col>
                  <Col xs={6}>
                    <div className="p-3 bg-body-secondary rounded">
                      <i className="bi bi-github me-2 text-dark"></i>
                      <strong>GitHub Actions</strong>
                    </div>
                  </Col>
                </Row>
              </Card.Body>
            </Card>
          </Col>
        </Row>
      </Container>
      </section>

      {/* AI-Powered Development Assistant Section */}
      <section className="py-5">
        <Container className="my-5">
          <div className="text-center mb-5">
            <h2 className="display-5 fw-bold mb-3">
              {t('home.ai.title', 'Build Faster with AI')}
            </h2>
            <p className="lead text-muted">
              {t('home.ai.subtitle', 'Premium AI assistant with deep CoreMS knowledge - directly in your IDE')}
            </p>
          </div>

          <Row className="align-items-center">
            <Col lg={6} className="mb-4 mb-lg-0">
              <Card className="border-0 bg-gradient bg-body-secondary" >
                <Card.Body className="p-5">
                  <div className="mb-4">
                    <i className="bi bi-robot fs-1"></i>
                  </div>
                  <h3 className="fw-bold mb-3">{t('home.ai.cardTitle', 'CoreMS AI Assistant')}</h3>
                  <p className="mb-4">
                    {t('home.ai.cardDesc', 'Get instant access to CoreMS architecture patterns, code generation, and expert guidance powered by AI. Works seamlessly with your favorite IDE through Model Context Protocol.')}
                  </p>
                  <div className="mb-4">
                    <h5 className="fw-bold mb-3">{t('home.ai.compatibleWith', 'Compatible With:')}</h5>
                    <div className="d-flex gap-2 flex-wrap">
                      <span className="badge bg-white bg-opacity-25 px-3 py-2">
                        <i className="bi bi-code-slash me-2"></i>Kiro
                      </span>
                      <span className="badge bg-white bg-opacity-25 px-3 py-2">
                        <i className="bi bi-code-slash me-2"></i>IntelliJ IDEA
                      </span>
                      <span className="badge bg-white bg-opacity-25 px-3 py-2">
                        <i className="bi bi-code-slash me-2"></i>VS Code
                      </span>
                      <span className="badge bg-white bg-opacity-25 px-3 py-2">
                        <i className="bi bi-code-slash me-2"></i>Cursor
                      </span>
                    </div>
                  </div>
                  <div className="d-flex gap-3 flex-wrap">
                    <Button variant='primary' size="lg">
                      <i className="bi bi-credit-card-fill me-2"></i>
                      {t('home.ai.subscribe', 'Subscribe')}
                    </Button>
                    <Button variant="outline-primary" size="lg">
                      {t('home.ai.learnMore', 'Learn More')}
                    </Button>
                  </div>
                </Card.Body>
              </Card>
            </Col>

            <Col lg={6}>
              <h4 className="fw-bold mb-4">{t('home.ai.featuresTitle', 'What You Get')}</h4>
              <div className="mb-3">
                <div className="d-flex mb-3">
                  <div className="me-3">
                    <i className="bi bi-check-circle-fill fs-4 text-success"></i>
                  </div>
                  <div>
                    <h5 className="fw-bold">{t('home.ai.feature1', 'Expert Architecture Guidance')}</h5>
                    <p className="text-muted">
                      {t('home.ai.feature1Desc', 'Instant answers about CoreMS patterns, service structure, and enterprise best practices')}
                    </p>
                  </div>
                </div>
                <div className="d-flex mb-3">
                  <div className="me-3">
                    <i className="bi bi-check-circle-fill fs-4 text-success"></i>
                  </div>
                  <div>
                    <h5 className="fw-bold">{t('home.ai.feature2', 'Intelligent Code Generation')}</h5>
                    <p className="text-muted">
                      {t('home.ai.feature2Desc', 'Generate services, controllers, entities, and migrations that follow CoreMS conventions perfectly')}
                    </p>
                  </div>
                </div>
                <div className="d-flex mb-3">
                  <div className="me-3">
                    <i className="bi bi-check-circle-fill fs-4 text-success"></i>
                  </div>
                  <div>
                    <h5 className="fw-bold">{t('home.ai.feature3', 'Ready-to-Use Templates')}</h5>
                    <p className="text-muted">
                      {t('home.ai.feature3Desc', 'Access OpenAPI specs, security configurations, and database migration templates instantly')}
                    </p>
                  </div>
                </div>
                <div className="d-flex mb-3">
                  <div className="me-3">
                    <i className="bi bi-check-circle-fill fs-4 text-success"></i>
                  </div>
                  <div>
                    <h5 className="fw-bold">{t('home.ai.feature4', 'Context-Aware Debugging')}</h5>
                    <p className="text-muted">
                      {t('home.ai.feature4Desc', 'Get smart troubleshooting suggestions based on CoreMS architecture and common patterns')}
                    </p>
                  </div>
                </div>
              </div>
            </Col>
          </Row>
          <Row>
            <Col>
              <div className="alert alert-info border-0">
                  <i className="bi bi-lightbulb-fill me-2"></i>
                  <strong>{t('home.ai.note', 'Note:')}</strong> {t('home.ai.noteDesc', 'Requires subscription and compatible IDE with MCP support. Free tier available.')}
              </div>
            </Col>
          </Row>
        </Container>
      </section>

      {/* CTA Section */}
      <section className="py-5 my-5" style={{ background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)' }}>
        <Container>
          <Row className="text-center text-white">
            <Col>
              <h2 className="display-5 fw-bold mb-4">
                {t('home.cta.title', 'Ready to Build at Enterprise Scale?')}
              </h2>
              <p className="lead mb-4">
                {t('home.cta.subtitle', 'Join thousands of developers shipping production-ready applications with Core Microservices')}
              </p>
              <div className="d-flex gap-3 justify-content-center flex-wrap">
                <Button 
                  size="lg" 
                  variant="light" 
                  className="px-5 py-3"
                  onClick={() => {
                    setShowRegister(true);
                    window.scrollTo({ top: 0, behavior: 'smooth' });
                  }}
                >
                  {t('home.cta.getStarted', 'Get Started Now')}
                </Button>
                <Button 
                  size="lg" 
                  variant="outline-light" 
                  className="px-5 py-3"
                  href="https://github.com/CoreWebMicroservices/corems-project"
                  target="_blank"
                >
                  <i className="bi bi-github me-2"></i>
                  {t('home.cta.viewGithub', 'View on GitHub')}
                </Button>
              </div>
            </Col>
          </Row>
        </Container>
      </section>

      {/* Footer */}
      <Container className="py-4">
        <Row className="text-center text-muted">
          <Col>
            <p className="mb-2">
              <i className="bi bi-github me-2"></i>
              <a 
                href="https://github.com/CoreWebMicroservices/corems-project" 
                target="_blank" 
                rel="noopener noreferrer"
                className="text-decoration-none"
              >
                {t('home.footer.github', 'View on GitHub')}
              </a>
            </p>
            <p className="small">
              {t('home.footer.license', 'Licensed under Apache 2.0 - Free for commercial use')}
            </p>
          </Col>
        </Row>
      </Container>
    </div>
  );
};

export default HomePage;
