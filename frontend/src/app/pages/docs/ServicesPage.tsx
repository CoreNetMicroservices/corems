import { DocsContent, Callout } from '@/common/component/docs';
import { Card, Row, Col } from 'react-bootstrap';

const ServicesPage = () => {
  return (
    <DocsContent
      title="Services Overview"
      breadcrumbs={[
        { label: 'Documentation', path: '/docs/introduction' },
        { label: 'Services' },
      ]}
    >
      <p className="lead">
        Core Microservices provides a suite of production-ready services that handle common application needs.
      </p>

      <Callout type="info">
        Each service is independently deployable and can be used standalone or as part of the complete platform.
      </Callout>

      <h2 className="h3 mt-5 mb-3">Available Services</h2>

      <Row className="g-4 my-4">
        <Col md={6}>
          <Card className="h-100 border-0 shadow-sm bg-body">
            <Card.Body>
              <div className="d-flex align-items-center mb-3">
                <i className="bi bi-people-fill fs-1 text-primary me-3"></i>
                <div>
                  <h3 className="h5 mb-0">User Management</h3>
                  <small className="text-muted">Port 3000</small>
                </div>
              </div>
              <p>
                Complete authentication and user management system with JWT, OAuth2, and role-based access control.
              </p>
              <h4 className="h6 fw-bold mt-3">Key Features:</h4>
              <ul className="small">
                <li>JWT authentication</li>
                <li>OAuth2 (Google, GitHub, LinkedIn)</li>
                <li>User profiles and roles</li>
                <li>Email/phone verification</li>
                <li>Password reset</li>
              </ul>
              <h4 className="h6 fw-bold mt-3">API Endpoints:</h4>
              <ul className="small mb-0">
                <li><code>POST /oauth2/token</code> - Login</li>
                <li><code>GET /api/users</code> - List users</li>
                <li><code>GET /api/users/me</code> - Current user</li>
              </ul>
            </Card.Body>
          </Card>
        </Col>

        <Col md={6}>
          <Card className="h-100 border-0 shadow-sm bg-body">
            <Card.Body>
              <div className="d-flex align-items-center mb-3">
                <i className="bi bi-envelope-fill fs-1 text-primary me-3"></i>
                <div>
                  <h3 className="h5 mb-0">Communication</h3>
                  <small className="text-muted">Port 3001</small>
                </div>
              </div>
              <p>
                Multi-channel messaging service for email, SMS, and push notifications with template support.
              </p>
              <h4 className="h6 fw-bold mt-3">Key Features:</h4>
              <ul className="small">
                <li>Email sending</li>
                <li>SMS notifications</li>
                <li>Message templates</li>
                <li>Delivery tracking</li>
                <li>Scheduled messages</li>
              </ul>
              <h4 className="h6 fw-bold mt-3">API Endpoints:</h4>
              <ul className="small mb-0">
                <li><code>POST /api/messages</code> - Send message</li>
                <li><code>GET /api/messages</code> - List messages</li>
                <li><code>GET /api/messages/{'{id}'}</code> - Message details</li>
              </ul>
            </Card.Body>
          </Card>
        </Col>

        <Col md={6}>
          <Card className="h-100 border-0 shadow-sm bg-body">
            <Card.Body>
              <div className="d-flex align-items-center mb-3">
                <i className="bi bi-folder-fill fs-1 text-primary me-3"></i>
                <div>
                  <h3 className="h5 mb-0">Document Management</h3>
                  <small className="text-muted">Port 3002</small>
                </div>
              </div>
              <p>
                S3-compatible file storage and management with access control and metadata support.
              </p>
              <h4 className="h6 fw-bold mt-3">Key Features:</h4>
              <ul className="small">
                <li>File upload/download</li>
                <li>S3-compatible storage (MinIO)</li>
                <li>Access control</li>
                <li>File metadata</li>
                <li>Secure sharing</li>
              </ul>
              <h4 className="h6 fw-bold mt-3">API Endpoints:</h4>
              <ul className="small mb-0">
                <li><code>POST /api/documents</code> - Upload file</li>
                <li><code>GET /api/documents</code> - List files</li>
                <li><code>GET /api/documents/{'{id}'}</code> - Download</li>
              </ul>
            </Card.Body>
          </Card>
        </Col>

        <Col md={6}>
          <Card className="h-100 border-0 shadow-sm bg-body">
            <Card.Body>
              <div className="d-flex align-items-center mb-3">
                <i className="bi bi-translate fs-1 text-primary me-3"></i>
                <div>
                  <h3 className="h5 mb-0">Translation</h3>
                  <small className="text-muted">Port 3003</small>
                </div>
              </div>
              <p>
                Multi-language support with translation management and dynamic content localization.
              </p>
              <h4 className="h6 fw-bold mt-3">Key Features:</h4>
              <ul className="small">
                <li>Multi-language support</li>
                <li>Translation management</li>
                <li>Realm-based organization</li>
                <li>Dynamic localization</li>
                <li>Import/export</li>
              </ul>
              <h4 className="h6 fw-bold mt-3">API Endpoints:</h4>
              <ul className="small mb-0">
                <li><code>GET /api/translation/{'{realm}'}/{'{lang}'}</code></li>
                <li><code>GET /api/translations</code> - List all</li>
                <li><code>POST /api/translations</code> - Create</li>
              </ul>
            </Card.Body>
          </Card>
        </Col>
      </Row>

      <h2 className="h3 mt-5 mb-3">Coming Soon</h2>

      <Row className="g-4 my-4">
        <Col md={6}>
          <Card className="h-100 border-0 shadow-sm bg-body opacity-75">
            <Card.Body>
              <div className="d-flex align-items-center mb-3">
                <i className="bi bi-credit-card-fill fs-1 text-muted me-3"></i>
                <div>
                  <h3 className="h5 mb-0">Payment Service</h3>
                  <small className="text-muted">Q2 2025</small>
                </div>
              </div>
              <p className="text-muted">
                Payment processing with Stripe, Vipps, Klarna Klarna integration. Transaction management and webhooks.
              </p>
            </Card.Body>
          </Card>
        </Col>

        <Col md={6}>
          <Card className="h-100 border-0 shadow-sm bg-body opacity-75">
            <Card.Body>
              <div className="d-flex align-items-center mb-3">
                <i className="bi bi-receipt-fill fs-1 text-muted me-3"></i>
                <div>
                  <h3 className="h5 mb-0">Billing Service</h3>
                  <small className="text-muted">Q2 2025</small>
                </div>
              </div>
              <p className="text-muted">
                Subscription management, invoice generation, usage-based billing, and payment plans.
              </p>
            </Card.Body>
          </Card>
        </Col>

        <Col md={6}>
          <Card className="h-100 border-0 shadow-sm bg-body opacity-75">
            <Card.Body>
              <div className="d-flex align-items-center mb-3">
                <i className="bi bi-file-code-fill fs-1 text-muted me-3"></i>
                <div>
                  <h3 className="h5 mb-0">Template Service</h3>
                  <small className="text-muted">Q2 2025</small>
                </div>
              </div>
              <p className="text-muted">
                HTML template management for emails, documents, and reports with variable substitution.
              </p>
            </Card.Body>
          </Card>
        </Col>

        <Col md={6}>
          <Card className="h-100 border-0 shadow-sm bg-body opacity-75">
            <Card.Body>
              <div className="d-flex align-items-center mb-3">
                <i className="bi bi-clipboard-check-fill fs-1 text-muted me-3"></i>
                <div>
                  <h3 className="h5 mb-0">Form Service</h3>
                  <small className="text-muted">Q3 2025</small>
                </div>
              </div>
              <p className="text-muted">
                Dynamic form builder, surveys, questionnaires with validation and conditional logic.
              </p>
            </Card.Body>
          </Card>
        </Col>
      </Row>

      <h2 className="h3 mt-5 mb-3">Service Communication</h2>
      <p>
        Services communicate through well-defined REST APIs and message queues:
      </p>

      <h3 className="h5 mt-4 mb-2">Synchronous (REST)</h3>
      <ul>
        <li>Direct HTTP calls between services</li>
        <li>JWT token passed in Authorization header</li>
        <li>Generated API clients for type safety</li>
      </ul>

      <h3 className="h5 mt-4 mb-2">Asynchronous (RabbitMQ)</h3>
      <ul>
        <li>Event-driven communication</li>
        <li>Background processing</li>
        <li>Decoupled service interactions</li>
      </ul>

      <Callout type="tip" title="API Documentation">
        Each service provides OpenAPI specification in the <code>*-api</code> module under <code>src/main/resources/openapi.yaml</code>.
      </Callout>

      <h2 className="h3 mt-5 mb-3">Common Patterns</h2>

      <h3 className="h5 mt-4 mb-2">Authentication</h3>
      <p>
        All services validate JWT tokens issued by the User Management service:
      </p>
      <ul>
        <li>Token contains user ID, email, and roles</li>
        <li>Services extract user info from token</li>
        <li>No need to call User service for every request</li>
      </ul>

      <h3 className="h5 mt-4 mb-2">Authorization</h3>
      <p>
        Role-based access control using annotations:
      </p>
      <ul>
        <li><code>@RequireRoles(CoreMsRoles.USER_MS_ADMIN)</code></li>
        <li><code>SUPER_ADMIN</code> has access to all endpoints</li>
        <li>Custom roles per service</li>
      </ul>

      <h3 className="h5 mt-4 mb-2">Error Handling</h3>
      <p>
        Consistent error responses across all services:
      </p>
      <ul>
        <li>Standard error format</li>
        <li>HTTP status codes</li>
        <li>Detailed error messages</li>
        <li>Validation errors</li>
      </ul>

      <Callout type="info" title="Learn More">
        Check out the <a href="/docs/architecture">Architecture Overview</a> for detailed information about 
        how services work together, or explore the <a href="/docs/roadmap">Roadmap</a> to see what's coming next.
      </Callout>
    </DocsContent>
  );
};

export default ServicesPage;
