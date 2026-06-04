import { DocsContent, CodeBlock, Callout } from '@/common/component/docs';
import { Card, Row, Col } from 'react-bootstrap';

const CommonLibrariesPage = () => {
  return (
    <DocsContent
      title="Common Libraries"
      breadcrumbs={[
        { label: 'Documentation', path: '/docs/introduction' },
        { label: 'Common Libraries' },
      ]}
    >
      <p className="lead">
        Shared libraries and utilities that power all Core Microservices. These modules provide 
        consistent functionality across services, reducing code duplication and ensuring best practices.
      </p>

      <Callout type="info" title="Key Benefit">
        Common libraries are one of CoreMS's most powerful features - write once, use everywhere. 
        All services automatically benefit from security updates, bug fixes, and new features.
      </Callout>

      <h2 className="h4 mt-5 mb-3">Available Modules</h2>

      <Row className="g-4 mb-5">
        <Col md={6}>
          <Card className="h-100">
            <Card.Body>
              <h3 className="h5">
                <i className="bi bi-shield-lock-fill text-primary me-2"></i>
                Security
              </h3>
              <p className="small">JWT authentication and role-based authorization</p>
              <ul className="small">
                <li>JWT token generation and validation</li>
                <li><code>@RequireRoles</code> annotation</li>
                <li><code>SecurityUtils</code> for current user access</li>
                <li>OAuth2 client support</li>
                <li>Automatic token validation on all endpoints</li>
              </ul>
            </Card.Body>
          </Card>
        </Col>

        <Col md={6}>
          <Card className="h-100">
            <Card.Body>
              <h3 className="h5">
                <i className="bi bi-file-text-fill text-primary me-2"></i>
                Logging
              </h3>
              <p className="small">Structured logging with request tracing</p>
              <ul className="small">
                <li>Log4j2 configuration</li>
                <li>MDC context propagation</li>
                <li>Request/response logging filter</li>
                <li>Correlation ID tracking</li>
                <li>Consistent log format across services</li>
              </ul>
            </Card.Body>
          </Card>
        </Col>

        <Col md={6}>
          <Card className="h-100">
            <Card.Body>
              <h3 className="h5">
                <i className="bi bi-exclamation-triangle-fill text-primary me-2"></i>
                Exception Handling
              </h3>
              <p className="small">Global exception handling and error responses</p>
              <ul className="small">
                <li>Global <code>@ControllerAdvice</code> handler</li>
                <li>Standardized error response format</li>
                <li>Validation error handling</li>
                <li>Custom exception types</li>
                <li>Automatic HTTP status mapping</li>
              </ul>
            </Card.Body>
          </Card>
        </Col>

        <Col md={6}>
          <Card className="h-100">
            <Card.Body>
              <h3 className="h5">
                <i className="bi bi-database-fill text-primary me-2"></i>
                Database Utilities
              </h3>
              <p className="small">Advanced querying and pagination</p>
              <ul className="small">
                <li><code>SearchableRepository</code> interface</li>
                <li><code>PaginatedQueryExecutor</code></li>
                <li>Dynamic filtering and sorting</li>
                <li>Full-text search support</li>
                <li>Consistent pagination across services</li>
              </ul>
            </Card.Body>
          </Card>
        </Col>

        <Col md={6}>
          <Card className="h-100">
            <Card.Body>
              <h3 className="h5">
                <i className="bi bi-arrow-left-right text-primary me-2"></i>
                Message Queue
              </h3>
              <p className="small">RabbitMQ integration for async processing</p>
              <ul className="small">
                <li>RabbitMQ auto-configuration</li>
                <li>Message serialization/deserialization</li>
                <li>Dead letter queue support</li>
                <li>Retry mechanisms</li>
                <li>Event-driven architecture support</li>
              </ul>
            </Card.Body>
          </Card>
        </Col>

        <Col md={6}>
          <Card className="h-100">
            <Card.Body>
              <h3 className="h5">
                <i className="bi bi-globe text-primary me-2"></i>
                Inbound Client
              </h3>
              <p className="small">Service-to-service communication</p>
              <ul className="small">
                <li>WebClient configuration</li>
                <li>Automatic JWT token propagation</li>
                <li>Circuit breaker support</li>
                <li>Retry policies</li>
                <li>Load balancing</li>
              </ul>
            </Card.Body>
          </Card>
        </Col>

        <Col md={6}>
          <Card className="h-100">
            <Card.Body>
              <h3 className="h5">
                <i className="bi bi-graph-up text-primary me-2"></i>
                Observability
              </h3>
              <p className="small">Health checks and metrics</p>
              <ul className="small">
                <li>Spring Boot Actuator integration</li>
                <li>Prometheus metrics</li>
                <li>Custom health indicators</li>
                <li>Performance monitoring</li>
                <li>Ready for Grafana dashboards</li>
              </ul>
            </Card.Body>
          </Card>
        </Col>

        <Col md={6}>
          <Card className="h-100">
            <Card.Body>
              <h3 className="h5">
                <i className="bi bi-code-square text-primary me-2"></i>
                API Support
              </h3>
              <p className="small">OpenAPI codegen and common resources</p>
              <ul className="small">
                <li>OpenAPI code generation support</li>
                <li>Common API models</li>
                <li>Standardized response formats</li>
                <li>API versioning support</li>
                <li>Swagger UI integration</li>
              </ul>
            </Card.Body>
          </Card>
        </Col>
      </Row>

      <h2 className="h4 mt-5 mb-3">Usage Example</h2>
      <p>Add common libraries to your service:</p>

      <CodeBlock language="xml">
{`<dependencies>
    <!-- Security with JWT -->
    <dependency>
        <groupId>com.corems.common</groupId>
        <artifactId>security</artifactId>
        <version>0.0.1-SNAPSHOT</version>
    </dependency>
    
    <!-- Logging -->
    <dependency>
        <groupId>com.corems.common</groupId>
        <artifactId>logging</artifactId>
        <version>0.0.1-SNAPSHOT</version>
    </dependency>
    
    <!-- Database utilities -->
    <dependency>
        <groupId>com.corems.common</groupId>
        <artifactId>db-utils</artifactId>
        <version>0.0.1-SNAPSHOT</version>
    </dependency>
    
    <!-- Exception handling -->
    <dependency>
        <groupId>com.corems.common</groupId>
        <artifactId>exception</artifactId>
        <version>0.0.1-SNAPSHOT</version>
    </dependency>
</dependencies>`}
      </CodeBlock>

      <h2 className="h4 mt-5 mb-3">Security Module Example</h2>
      <CodeBlock language="java">
{`@RestController
@RequestMapping("/api/users")
public class UserController {
    
    // Require specific role
    @RequireRoles(CoreMsRoles.UserMsAdmin)
    @GetMapping
    public List<User> getAllUsers() {
        // Get current authenticated user
        UserPrincipal currentUser = SecurityUtils.getUserPrincipal();
        
        return userService.findAll();
    }
    
    // Multiple roles (OR logic)
    @RequireRoles({CoreMsRoles.UserMsAdmin, CoreMsRoles.SuperAdmin})
    @DeleteMapping("/{id}")
    public void deleteUser(@PathVariable UUID id) {
        userService.delete(id);
    }
}`}
      </CodeBlock>

      <h2 className="h4 mt-5 mb-3">Database Utilities Example</h2>
      <CodeBlock language="java">
{`// Repository with search support
public interface UserRepository extends SearchableRepository<User, UUID> {
    // Inherits: findAll, findById, save, delete
    // Plus: search with filters, pagination, sorting
}

// Controller with pagination
@GetMapping
public Page<User> getUsers(
    @RequestParam(required = false) String search,
    @RequestParam(defaultValue = "0") int page,
    @RequestParam(defaultValue = "20") int size,
    @RequestParam(defaultValue = "createdAt,desc") String sort
) {
    DataTableQueryParams params = DataTableQueryParams.builder()
        .search(search)
        .page(page)
        .size(size)
        .sort(sort)
        .build();
        
    return userRepository.findAll(params);
}`}
      </CodeBlock>

      <h2 className="h4 mt-5 mb-3">Exception Handling Example</h2>
      <CodeBlock language="java">
{`// Throw custom exceptions
if (user == null) {
    throw new ResourceNotFoundException("User not found");
}

if (!user.isActive()) {
    throw new BusinessException("User account is inactive");
}

// Automatically converted to proper HTTP responses:
// ResourceNotFoundException -> 404 Not Found
// BusinessException -> 400 Bad Request
// ValidationException -> 422 Unprocessable Entity`}
      </CodeBlock>

      <Callout type="tip" title="Auto-Configuration">
        Most common libraries use Spring Boot auto-configuration. Simply add the dependency 
        and they're automatically wired - no manual configuration needed!
      </Callout>

      <h2 className="h4 mt-5 mb-3">Benefits</h2>
      <ul>
        <li><strong>Consistency</strong> - All services behave the same way</li>
        <li><strong>Maintainability</strong> - Fix once, deploy everywhere</li>
        <li><strong>Best Practices</strong> - Battle-tested patterns built-in</li>
        <li><strong>Rapid Development</strong> - Focus on business logic, not infrastructure</li>
        <li><strong>Type Safety</strong> - Compile-time checks across services</li>
        <li><strong>Testing</strong> - Shared test utilities and mocks</li>
      </ul>

      <h2 className="h4 mt-5 mb-3">Installation</h2>
      <CodeBlock language="bash">
{`# Install parent POM first
cd repos/parent
mvn clean install

# Install common libraries
cd ../common
mvn clean install

# Now available to all services`}
      </CodeBlock>

      <Callout type="info" title="Version Management">
        Common library versions are managed in the parent POM. Services inherit versions 
        automatically, ensuring compatibility across the entire stack.
      </Callout>
    </DocsContent>
  );
};

export default CommonLibrariesPage;
