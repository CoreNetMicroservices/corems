import { DocsContent, CodeBlock, Callout } from '@/common/component/docs';

const ArchitecturePage = () => {
  return (
    <DocsContent
      title="Architecture Overview"
      breadcrumbs={[
        { label: 'Documentation', path: '/docs/introduction' },
        { label: 'Architecture' },
      ]}
    >
      <p className="lead">
        Core Microservices follows a modern microservices architecture with independent, scalable services 
        that communicate through well-defined APIs.
      </p>

      <h2 className="h3 mt-5 mb-3">High-Level Architecture</h2>
      <p>
        The platform consists of multiple independent services, each responsible for a specific business domain:
      </p>

      <div className="my-4 text-center">
        <img 
          src="/docs/design-overview.png" 
          alt="CoreMS Architecture Overview" 
          className="img-fluid rounded border"
          style={{ maxWidth: '100%' }}
        />
      </div>

      <h2 className="h3 mt-5 mb-3">Core Principles</h2>

      <h3 className="h5 mt-4 mb-2">1. Service Independence</h3>
      <p>
        Each microservice is independently deployable, scalable, and maintainable. Services have their own:
      </p>
      <ul>
        <li>Database schema</li>
        <li>API specification (OpenAPI)</li>
        <li>Docker container</li>
        <li>Git repository</li>
      </ul>

      <h3 className="h5 mt-4 mb-2">2. API-First Design</h3>
      <p>
        All services define their APIs using OpenAPI specifications before implementation. This enables:
      </p>
      <ul>
        <li>Automatic client generation</li>
        <li>Contract testing</li>
        <li>Clear service boundaries</li>
        <li>Documentation generation</li>
      </ul>

      <h3 className="h5 mt-4 mb-2">3. Shared Infrastructure</h3>
      <p>
        Common concerns are handled by shared infrastructure:
      </p>
      <ul>
        <li><strong>Authentication</strong>: JWT tokens validated by all services</li>
        <li><strong>Logging</strong>: Centralized logging configuration</li>
        <li><strong>Error Handling</strong>: Consistent error responses</li>
        <li><strong>Security</strong>: Shared security filters and utilities</li>
      </ul>

      <h2 className="h3 mt-5 mb-3">Service Structure</h2>
      <p>Each microservice follows a consistent structure:</p>

      <CodeBlock language="text">
{`service-ms/
├── service-api/              # OpenAPI spec + generated models
│   ├── src/main/resources/
│   │   └── openapi.yaml     # API specification
│   └── pom.xml
├── service-client/           # Generated API client
│   └── pom.xml
├── service-service/          # Implementation
│   ├── src/main/java/
│   │   └── .../app/
│   │       ├── controller/  # REST endpoints
│   │       ├── service/     # Business logic
│   │       ├── repository/  # Data access
│   │       └── entity/      # JPA entities
│   └── pom.xml
└── migrations/               # Database migrations
    ├── setup/               # Schema (V1.0.x)
    └── mockdata/            # Seed data (R__xx)`}
      </CodeBlock>

      <h2 className="h3 mt-5 mb-3">Communication Patterns</h2>

      <h3 className="h5 mt-4 mb-2">Synchronous (REST)</h3>
      <p>
        Services expose REST APIs for synchronous communication:
      </p>
      <CodeBlock language="java">
{`// User service validates token and returns user info
GET /api/users/{userId}
Authorization: Bearer <jwt-token>

// Document service checks permissions via user service
GET /api/documents/{documentId}
Authorization: Bearer <jwt-token>`}
      </CodeBlock>

      <h3 className="h5 mt-4 mb-2">Asynchronous (Message Queue)</h3>
      <p>
        RabbitMQ handles asynchronous communication for:
      </p>
      <ul>
        <li>Email notifications</li>
        <li>Background processing</li>
        <li>Event-driven workflows</li>
      </ul>

      <CodeBlock language="java">
{`// User service publishes event
userCreatedEvent -> RabbitMQ -> Communication service sends welcome email`}
      </CodeBlock>

      <h2 className="h3 mt-5 mb-3">Authentication Flow</h2>
      <p>JWT-based authentication with OAuth2 support:</p>

      <CodeBlock language="text">
{`1. User logs in via frontend
   POST /oauth2/token
   
2. User service validates credentials
   - Checks database
   - Or validates OAuth2 token (Google/GitHub)
   
3. Returns JWT tokens
   {
     "access_token": "eyJ...",
     "refresh_token": "eyJ...",
     "expires_in": 3600
   }
   
4. Frontend stores tokens
   - Access token in memory
   - Refresh token in localStorage
   
5. All API requests include token
   Authorization: Bearer eyJ...
   
6. Services validate token
   - Check signature
   - Verify expiration
   - Extract user info`}
      </CodeBlock>

      <Callout type="info">
        Access tokens expire after 1 hour. The frontend automatically refreshes them using the refresh token.
      </Callout>

      <h2 className="h3 mt-5 mb-3">Database Strategy</h2>
      <p>
        Each service has its own database schema in PostgreSQL:
      </p>
      <ul>
        <li><code>user_ms</code> - User service data</li>
        <li><code>communication_ms</code> - Messages and templates</li>
        <li><code>document_ms</code> - File metadata</li>
        <li><code>translation_ms</code> - Translations</li>
      </ul>

      <Callout type="warning" title="No Direct Database Access">
        Services never access another service's database directly. All communication happens through APIs.
      </Callout>

      <h2 className="h3 mt-5 mb-3">Frontend Architecture</h2>
      <p>
        The frontend follows a modular architecture:
      </p>

      <CodeBlock language="text">
{`frontend/src/
├── app/                  # Application shell
│   ├── layout/          # Layout components
│   ├── pages/           # App-specific pages
│   └── router/          # Route configuration
├── common/              # Shared utilities
│   ├── component/       # Reusable components
│   ├── utils/           # Helper functions
│   └── model/           # Type definitions
├── user/                # User module
│   ├── component/       # User components
│   ├── store/           # User state
│   └── model/           # User types
├── document/            # Document module
└── communication/       # Communication module`}
      </CodeBlock>

      <h3 className="h5 mt-4 mb-2">Module Independence</h3>
      <p>
        Each module is self-contained and can be:
      </p>
      <ul>
        <li>Developed independently</li>
        <li>Tested in isolation</li>
        <li>Reused in other projects</li>
        <li>Published as npm package</li>
      </ul>

      <h2 className="h3 mt-5 mb-3">Deployment</h2>

      <h3 className="h5 mt-4 mb-2">Local Development</h3>
      <CodeBlock language="bash">
{`# Start infrastructure
docker-compose up -d postgres rabbitmq minio

# Start services
./setup.sh start-all

# Or run individual service in IDE for debugging`}
      </CodeBlock>

      <h3 className="h5 mt-4 mb-2">Production (Coming Soon)</h3>
      <ul>
        <li><strong>Docker</strong>: Each service as a container</li>
        <li><strong>Kubernetes</strong>: Orchestration and scaling</li>
        <li><strong>Terraform</strong>: Infrastructure as code</li>
        <li><strong>CI/CD</strong>: Automated testing and deployment</li>
      </ul>

      <h2 className="h3 mt-5 mb-3">Scalability</h2>
      <p>
        The architecture supports horizontal scaling:
      </p>
      <ul>
        <li>Run multiple instances of any service</li>
        <li>Load balancer distributes traffic</li>
        <li>Stateless services (JWT in token, not session)</li>
        <li>Database connection pooling</li>
        <li>Message queue for async processing</li>
      </ul>

      <h2 className="h3 mt-5 mb-3">Security</h2>

      <h3 className="h5 mt-4 mb-2">Authentication & Authorization</h3>
      <ul>
        <li>JWT tokens with RS256 signing</li>
        <li>Role-based access control (RBAC)</li>
        <li>OAuth2 for social login</li>
        <li>Token refresh mechanism</li>
      </ul>

      <h3 className="h5 mt-4 mb-2">Data Protection</h3>
      <ul>
        <li>HTTPS for all communication</li>
        <li>Encrypted passwords (BCrypt)</li>
        <li>Secure file storage</li>
        <li>SQL injection prevention (JPA)</li>
      </ul>

      <Callout type="tip" title="Next Steps">
        Explore individual <a href="/docs/services">service documentation</a> to learn about specific APIs 
        and features, or check out the <a href="/docs/roadmap">roadmap</a> to see what's coming next.
      </Callout>
    </DocsContent>
  );
};

export default ArchitecturePage;
