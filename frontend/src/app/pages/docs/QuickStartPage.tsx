import { DocsContent, CodeBlock, Callout } from '@/common/component/docs';

const QuickStartPage = () => {
  return (
    <DocsContent
      title="Quick Start Guide"
      breadcrumbs={[
        { label: 'Documentation', path: '/docs/introduction' },
        { label: 'Quick Start' },
      ]}
    >
      <p className="lead">
        Get Core Microservices running on your local machine in under 10 minutes.
      </p>

      <h2 className="h3 mt-5 mb-3">Prerequisites</h2>
      <p>Before you begin, make sure you have the following installed:</p>
      <ul>
        <li><strong>Java 25+</strong> - <a href="https://adoptium.net/" target="_blank" rel="noopener noreferrer">Download</a></li>
        <li><strong>Maven 3.9+</strong> - <a href="https://maven.apache.org/download.cgi" target="_blank" rel="noopener noreferrer">Download</a></li>
        <li><strong>Docker & Docker Compose</strong> - <a href="https://www.docker.com/get-started" target="_blank" rel="noopener noreferrer">Download</a></li>
        <li><strong>Node.js 18+</strong> - <a href="https://nodejs.org/" target="_blank" rel="noopener noreferrer">Download</a></li>
        <li><strong>Git</strong> - <a href="https://git-scm.com/downloads" target="_blank" rel="noopener noreferrer">Download</a></li>
      </ul>

      <Callout type="info">
        Make sure Docker is running before proceeding with the setup.
      </Callout>

      <h2 className="h3 mt-5 mb-3">Step 1: Clone the Repository</h2>
      <CodeBlock language="bash">
{`git clone https://github.com/CoreWebMicroservices/corems-project.git
cd corems-project`}
      </CodeBlock>

      <h2 className="h3 mt-5 mb-3">Step 2: Initialize Services</h2>
      <p>
        The setup script will clone all service repositories and set up the project structure:
      </p>
      <CodeBlock language="bash">
{`# Initialize all services (user, document, communication, translation)
./setup.sh init-all

# Or initialize only specific services
./setup.sh init user-ms document-ms`}
      </CodeBlock>

      <h2 className="h3 mt-5 mb-3">Step 3: Start Infrastructure</h2>
      <p>
        Start PostgreSQL, RabbitMQ, and MinIO using Docker Compose:
      </p>
      <CodeBlock language="bash">
{`./setup.sh infra`}
      </CodeBlock>
      <p className="mt-2">
        This will start:
      </p>
      <ul>
        <li><strong>PostgreSQL</strong> on port 5432</li>
        <li><strong>RabbitMQ</strong> on port 5672 (Management UI: 15672)</li>
        <li><strong>MinIO</strong> on port 9000 (Console: 9001)</li>
      </ul>

      <h2 className="h3 mt-5 mb-3">Step 4: Configure Environment</h2>
      <p>
        Create environment files for each service:
      </p>
      <CodeBlock language="bash">
{`./setup.sh create-env`}
      </CodeBlock>
      <p className="mt-2">
        This creates <code>.env</code> files with default configurations. You can customize them as needed.
      </p>

      <Callout type="warning" title="Important">
        Never commit <code>.env</code> files to version control. They contain sensitive information 
        like database passwords and API keys.
      </Callout>

      <h2 className="h3 mt-5 mb-3">Step 5: Build Services</h2>
      <p>
        Build all services using Maven:
      </p>
      <CodeBlock language="bash">
{`./setup.sh build all`}
      </CodeBlock>
      <p className="mt-2">
        This will:
      </p>
      <ul>
        <li>Build the parent POM</li>
        <li>Build common libraries</li>
        <li>Generate API clients from OpenAPI specs</li>
        <li>Build all service modules</li>
      </ul>

      <h2 className="h3 mt-5 mb-3">Step 6: Run Database Migrations</h2>
      <p>
        Set up database schemas and optionally load sample data:
      </p>
      <CodeBlock language="bash">
{`# Run migrations with sample data
./setup.sh migrate --mockdata

# Or run migrations without sample data
./setup.sh migrate`}
      </CodeBlock>

      <h2 className="h3 mt-5 mb-3">Step 7: Start Services</h2>
      <p>
        Start all services in Docker containers:
      </p>
      <CodeBlock language="bash">
{`./setup.sh start-all`}
      </CodeBlock>
      <p className="mt-2">
        Services will be available at:
      </p>
      <ul>
        <li><strong>Frontend</strong>: <a href="http://localhost:8080" target="_blank" rel="noopener noreferrer">http://localhost:8080</a></li>
        <li><strong>User Service</strong>: <a href="http://localhost:3000" target="_blank" rel="noopener noreferrer">http://localhost:3000</a></li>
        <li><strong>Communication Service</strong>: <a href="http://localhost:3001" target="_blank" rel="noopener noreferrer">http://localhost:3001</a></li>
        <li><strong>Document Service</strong>: <a href="http://localhost:3002" target="_blank" rel="noopener noreferrer">http://localhost:3002</a></li>
        <li><strong>Translation Service</strong>: <a href="http://localhost:3003" target="_blank" rel="noopener noreferrer">http://localhost:3003</a></li>
      </ul>

      <h2 className="h3 mt-5 mb-3">Step 8: Access the Application</h2>
      <p>
        Open your browser and navigate to <a href="http://localhost:8080" target="_blank" rel="noopener noreferrer">http://localhost:8080</a>
      </p>
      <p>
        If you ran migrations with <code>--mockdata</code>, you can log in with:
      </p>
      <CodeBlock language="text">
{`Email: admin@corems.local
Password: admin123`}
      </CodeBlock>

      <Callout type="tip" title="Success!">
        You now have a fully functional microservices platform running locally. Explore the features, 
        check out the API documentation, and start building your application!
      </Callout>

      <h2 className="h3 mt-5 mb-3">Useful Commands</h2>
      
      <h3 className="h5 mt-4 mb-2">View Service Logs</h3>
      <CodeBlock language="bash">
{`./setup.sh logs user-ms`}
      </CodeBlock>

      <h3 className="h5 mt-4 mb-2">Stop a Service</h3>
      <CodeBlock language="bash">
{`./setup.sh stop user-ms`}
      </CodeBlock>

      <h3 className="h5 mt-4 mb-2">Restart a Service</h3>
      <CodeBlock language="bash">
{`./setup.sh stop user-ms
./setup.sh start user-ms`}
      </CodeBlock>

      <h3 className="h5 mt-4 mb-2">Stop All Services</h3>
      <CodeBlock language="bash">
{`./setup.sh stop-all`}
      </CodeBlock>

      <h2 className="h3 mt-5 mb-3">Troubleshooting</h2>
      
      <h3 className="h5 mt-4 mb-2">Port Already in Use</h3>
      <p>
        If you see "port already in use" errors, check what's running on that port:
      </p>
      <CodeBlock language="bash">
{`# On Linux/Mac
lsof -i :3000

# On Windows
netstat -ano | findstr :3000`}
      </CodeBlock>

      <h3 className="h5 mt-4 mb-2">Docker Issues</h3>
      <p>
        If services fail to start, try restarting Docker:
      </p>
      <CodeBlock language="bash">
{`docker-compose down
docker-compose up -d`}
      </CodeBlock>

      <h3 className="h5 mt-4 mb-2">Build Failures</h3>
      <p>
        Clean and rebuild if you encounter build issues:
      </p>
      <CodeBlock language="bash">
{`./setup.sh clean
./setup.sh build all`}
      </CodeBlock>

      <Callout type="info" title="Need Help?">
        Check out the <a href="https://github.com/CoreWebMicroservices/corems-project/issues" target="_blank" rel="noopener noreferrer">GitHub Issues</a> or 
        join our community for support.
      </Callout>

      <h2 className="h3 mt-5 mb-3">Next Steps</h2>
      <ul>
        <li>Explore the <a href="/docs/architecture">Architecture Overview</a> to understand the system design</li>
        <li>Check out the <a href="/docs/services">Services Documentation</a> for API details</li>
        <li>Review the <a href="/docs/roadmap">Roadmap</a> to see what's coming next</li>
      </ul>
    </DocsContent>
  );
};

export default QuickStartPage;
