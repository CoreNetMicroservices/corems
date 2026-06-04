import { useState } from 'react';
import { DocsContent, Callout } from '@/common/component/docs';
import { Badge, Card, Row, Col, Nav } from 'react-bootstrap';

const RoadmapPage = () => {
  const [activeTab, setActiveTab] = useState('overview');

  return (
    <DocsContent
      title="Roadmap"
      breadcrumbs={[
        { label: 'Documentation', path: '/docs/introduction' },
        { label: 'Roadmap' },
      ]}
    >
      <p className="lead">
        Our vision for Core Microservices and the features we're building to make it the most 
        comprehensive enterprise toolkit for rapid application development.
      </p>

      <Nav variant="tabs" className="mb-4">
        <Nav.Item>
          <Nav.Link active={activeTab === 'overview'} onClick={() => setActiveTab('overview')}>
            Services Overview
          </Nav.Link>
        </Nav.Item>
        <Nav.Item>
          <Nav.Link active={activeTab === 'live'} onClick={() => setActiveTab('live')}>
            <Badge bg="success" className="me-1">Live</Badge> Current
          </Nav.Link>
        </Nav.Item>
        <Nav.Item>
          <Nav.Link active={activeTab === 'progress'} onClick={() => setActiveTab('progress')}>
            <Badge bg="warning" text="dark" className="me-1">Q1 2025</Badge> In Progress
          </Nav.Link>
        </Nav.Item>
        <Nav.Item>
          <Nav.Link active={activeTab === 'planned'} onClick={() => setActiveTab('planned')}>
            <Badge bg="info" className="me-1">Future</Badge> Planned
          </Nav.Link>
        </Nav.Item>
      </Nav>

      {activeTab === 'overview' && (
        <>
          <h2 className="h4 mb-4">Release Timeline</h2>
          
          <div className="mb-5">
            <Row className="g-3">
              <Col md={4}>
                <Card className="h-100 border-success">
                  <Card.Header className="bg-success text-white">
                    <strong>Live Now</strong>
                  </Card.Header>
                  <Card.Body>
                    <ul className="mb-0 small">
                      <li>User Management</li>
                      <li>Communication Service</li>
                      <li>Document Management</li>
                      <li>Translation Service</li>
                      <li>React Frontend</li>
                      <li>Local Dev Infrastructure</li>
                    </ul>
                  </Card.Body>
                </Card>
              </Col>

              <Col md={4}>
                <Card className="h-100 border-warning">
                  <Card.Header className="bg-warning">
                    <strong>Q1 2025 - In Progress</strong>
                  </Card.Header>
                  <Card.Body>
                    <ul className="mb-0 small">
                      <li>CI/CD Pipelines</li>
                      <li>Terraform (AWS, Azure, GCP)</li>
                      <li>Kubernetes & Helm</li>
                      <li>Observability Stack</li>
                      <li>Monitoring & Alerting</li>
                    </ul>
                  </Card.Body>
                </Card>
              </Col>

              <Col md={4}>
                <Card className="h-100 border-info">
                  <Card.Header className="bg-info text-white">
                    <strong>Q2 2025</strong>
                  </Card.Header>
                  <Card.Body>
                    <ul className="mb-0 small">
                      <li>Payment Service</li>
                      <li>Billing Service</li>
                      <li>Template Service</li>
                      <li>Complete Documentation</li>
                    </ul>
                  </Card.Body>
                </Card>
              </Col>
            </Row>

            <Row className="g-3 mt-3">
              <Col md={6}>
                <Card className="h-100 border-secondary">
                  <Card.Header className="bg-secondary text-white">
                    <strong>Q3 2025</strong>
                  </Card.Header>
                  <Card.Body>
                    <ul className="mb-0 small">
                      <li>Form Service</li>
                      <li>Event Service</li>
                      <li>Notification Service</li>
                      <li>Video Tutorials</li>
                    </ul>
                  </Card.Body>
                </Card>
              </Col>

              <Col md={6}>
                <Card className="h-100 border-secondary">
                  <Card.Header className="bg-secondary text-white">
                    <strong>Q4 2025</strong>
                  </Card.Header>
                  <Card.Body>
                    <ul className="mb-0 small">
                      <li>Analytics Service</li>
                      <li>Search Service (Elasticsearch)</li>
                      <li>Example Applications</li>
                    </ul>
                  </Card.Body>
                </Card>
              </Col>
            </Row>
          </div>

          <Callout type="info" title="Community Driven">
            This roadmap is shaped by community feedback. Have a feature request? 
            <a href="https://github.com/CoreWebMicroservices/corems-project/issues" target="_blank" rel="noopener noreferrer"> Open an issue on GitHub</a>.
          </Callout>
        </>
      )}

      {activeTab === 'live' && (
        <>
          <h2 className="h4 mb-4">
            <Badge bg="success" className="me-2">Live</Badge>
            Production Ready Features
          </h2>

          <Row className="g-4">
            <Col md={6}>
              <Card>
                <Card.Body>
                  <h3 className="h5">
                    <i className="bi bi-people-fill text-primary me-2"></i>
                    User Management
                  </h3>
                  <ul className="small mb-0">
                    <li>JWT authentication</li>
                    <li>OAuth2 (Google, GitHub, LinkedIn)</li>
                    <li>Role-based access control</li>
                    <li>Email/phone verification</li>
                  </ul>
                </Card.Body>
              </Card>
            </Col>

            <Col md={6}>
              <Card>
                <Card.Body>
                  <h3 className="h5">
                    <i className="bi bi-envelope-fill text-primary me-2"></i>
                    Communication
                  </h3>
                  <ul className="small mb-0">
                    <li>Email & SMS sending</li>
                    <li>Message templates</li>
                    <li>Delivery tracking</li>
                    <li>Multi-channel messaging</li>
                  </ul>
                </Card.Body>
              </Card>
            </Col>

            <Col md={6}>
              <Card>
                <Card.Body>
                  <h3 className="h5">
                    <i className="bi bi-folder-fill text-primary me-2"></i>
                    Document Management
                  </h3>
                  <ul className="small mb-0">
                    <li>S3-compatible storage (MinIO)</li>
                    <li>File upload/download</li>
                    <li>Access control</li>
                    <li>Secure file sharing</li>
                  </ul>
                </Card.Body>
              </Card>
            </Col>

            <Col md={6}>
              <Card>
                <Card.Body>
                  <h3 className="h5">
                    <i className="bi bi-translate text-primary me-2"></i>
                    Translation
                  </h3>
                  <ul className="small mb-0">
                    <li>Multi-language support</li>
                    <li>Translation management UI</li>
                    <li>Dynamic localization</li>
                    <li>Realm-based organization</li>
                  </ul>
                </Card.Body>
              </Card>
            </Col>

            <Col md={6}>
              <Card>
                <Card.Body>
                  <h3 className="h5">
                    <i className="bi bi-code-square text-primary me-2"></i>
                    Frontend
                  </h3>
                  <ul className="small mb-0">
                    <li>React 18+ with TypeScript</li>
                    <li>Modular architecture</li>
                    <li>Responsive design</li>
                    <li>Reusable components</li>
                  </ul>
                </Card.Body>
              </Card>
            </Col>

            <Col md={6}>
              <Card>
                <Card.Body>
                  <h3 className="h5">
                    <i className="bi bi-box-fill text-primary me-2"></i>
                    Local Infrastructure
                  </h3>
                  <ul className="small mb-0">
                    <li>Docker containerization</li>
                    <li>Docker Compose setup</li>
                    <li>Automated scripts</li>
                    <li>Database migrations</li>
                  </ul>
                </Card.Body>
              </Card>
            </Col>
          </Row>
        </>
      )}

      {activeTab === 'progress' && (
        <>
          <h2 className="h4 mb-4">
            <Badge bg="warning" text="dark" className="me-2">Q1 2025</Badge>
            Active Development
          </h2>

          <Row className="g-4">
            <Col md={6}>
              <Card className="border-warning">
                <Card.Body>
                  <h3 className="h5">
                    <i className="bi bi-gear-fill text-warning me-2"></i>
                    CI/CD Pipelines
                  </h3>
                  <ul className="small">
                    <li>GitHub Actions workflows</li>
                    <li>Automated testing</li>
                    <li>Security scanning</li>
                    <li>Automated deployments</li>
                    <li>Quality gates</li>
                  </ul>
                </Card.Body>
              </Card>
            </Col>

            <Col md={6}>
              <Card className="border-warning">
                <Card.Body>
                  <h3 className="h5">
                    <i className="bi bi-cloud-fill text-warning me-2"></i>
                    Cloud Deployment
                  </h3>
                  <ul className="small">
                    <li>Terraform modules (AWS, Azure, GCP)</li>
                    <li>Kubernetes manifests</li>
                    <li>Helm charts</li>
                    <li>Auto-scaling configuration</li>
                    <li>Load balancing setup</li>
                  </ul>
                </Card.Body>
              </Card>
            </Col>

            <Col md={12}>
              <Card className="border-warning">
                <Card.Body>
                  <h3 className="h5">
                    <i className="bi bi-graph-up text-warning me-2"></i>
                    Observability Stack
                  </h3>
                  <Row>
                    <Col md={6}>
                      <ul className="small">
                        <li>Prometheus metrics</li>
                        <li>Grafana dashboards</li>
                        <li>Distributed tracing (Jaeger)</li>
                      </ul>
                    </Col>
                    <Col md={6}>
                      <ul className="small">
                        <li>Centralized logging (ELK)</li>
                        <li>Health checks</li>
                        <li>Alerting & monitoring</li>
                      </ul>
                    </Col>
                  </Row>
                </Card.Body>
              </Card>
            </Col>
          </Row>
        </>
      )}

      {activeTab === 'planned' && (
        <>
          <h2 className="h4 mb-4">
            <Badge bg="info" className="me-2">Future</Badge>
            Planned Features
          </h2>

          <h3 className="h5 mt-4 mb-3">Q2 2025</h3>
          <Row className="g-3 mb-4">
            <Col md={4}>
              <Card className="h-100">
                <Card.Body>
                  <h4 className="h6"><i className="bi bi-credit-card-fill text-info me-2"></i>Payment Service</h4>
                  <p className="small mb-0">Stripe, Vipps, Klarna Klarna integration with webhooks and refunds</p>
                </Card.Body>
              </Card>
            </Col>
            <Col md={4}>
              <Card className="h-100">
                <Card.Body>
                  <h4 className="h6"><i className="bi bi-receipt-fill text-info me-2"></i>Billing Service</h4>
                  <p className="small mb-0">Subscriptions, invoices, usage-based billing</p>
                </Card.Body>
              </Card>
            </Col>
            <Col md={4}>
              <Card className="h-100">
                <Card.Body>
                  <h4 className="h6"><i className="bi bi-file-code-fill text-info me-2"></i>Template Service</h4>
                  <p className="small mb-0">HTML templates, email builder, document engine</p>
                </Card.Body>
              </Card>
            </Col>
          </Row>

          <h3 className="h5 mt-4 mb-3">Q3 2025</h3>
          <Row className="g-3 mb-4">
            <Col md={4}>
              <Card className="h-100">
                <Card.Body>
                  <h4 className="h6"><i className="bi bi-clipboard-check-fill text-info me-2"></i>Form Service</h4>
                  <p className="small mb-0">Dynamic form builder, surveys, validation</p>
                </Card.Body>
              </Card>
            </Col>
            <Col md={4}>
              <Card className="h-100">
                <Card.Body>
                  <h4 className="h6"><i className="bi bi-calendar-event-fill text-info me-2"></i>Event Service</h4>
                  <p className="small mb-0">Scheduling, calendar integration, reminders</p>
                </Card.Body>
              </Card>
            </Col>
            <Col md={4}>
              <Card className="h-100">
                <Card.Body>
                  <h4 className="h6"><i className="bi bi-bell-fill text-info me-2"></i>Notification Service</h4>
                  <p className="small mb-0">Push notifications, WebSocket, in-app alerts</p>
                </Card.Body>
              </Card>
            </Col>
          </Row>

          <h3 className="h5 mt-4 mb-3">Q4 2025</h3>
          <Row className="g-3 mb-4">
            <Col md={6}>
              <Card className="h-100">
                <Card.Body>
                  <h4 className="h6"><i className="bi bi-graph-up-arrow text-info me-2"></i>Analytics Service</h4>
                  <p className="small mb-0">User tracking, custom events, dashboards, data visualization</p>
                </Card.Body>
              </Card>
            </Col>
            <Col md={6}>
              <Card className="h-100">
                <Card.Body>
                  <h4 className="h6"><i className="bi bi-search text-info me-2"></i>Search Service</h4>
                  <p className="small mb-0">Elasticsearch, full-text search, faceted search, autocomplete</p>
                </Card.Body>
              </Card>
            </Col>
          </Row>

          <h3 className="h5 mt-4 mb-3">Documentation & Learning</h3>
          <Row className="g-3">
            <Col md={4}>
              <Card className="h-100">
                <Card.Body>
                  <h4 className="h6"><i className="bi bi-book-fill text-secondary me-2"></i>Complete Docs</h4>
                  <p className="small mb-0">API docs, guides, best practices</p>
                  <Badge bg="warning" text="dark">Q2 2025</Badge>
                </Card.Body>
              </Card>
            </Col>
            <Col md={4}>
              <Card className="h-100">
                <Card.Body>
                  <h4 className="h6"><i className="bi bi-play-circle-fill text-secondary me-2"></i>Video Tutorials</h4>
                  <p className="small mb-0">Walkthroughs, architecture, deployment</p>
                  <Badge bg="info">Q3 2025</Badge>
                </Card.Body>
              </Card>
            </Col>
            <Col md={4}>
              <Card className="h-100">
                <Card.Body>
                  <h4 className="h6"><i className="bi bi-code-slash text-secondary me-2"></i>Example Apps</h4>
                  <p className="small mb-0">E-commerce, SaaS, marketplace starters</p>
                  <Badge bg="info">Q4 2025</Badge>
                </Card.Body>
              </Card>
            </Col>
          </Row>

          <Callout type="tip" title="Want to Contribute?" className="mt-4">
            We welcome contributions! Check out our <a href="https://github.com/CoreWebMicroservices/corems-project" target="_blank" rel="noopener noreferrer">GitHub repository</a> to 
            get started.
          </Callout>
        </>
      )}
    </DocsContent>
  );
};

export default RoadmapPage;
