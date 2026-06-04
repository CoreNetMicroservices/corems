import { DocsContent, CodeBlock, Callout } from '@/common/component/docs';

const IntroductionPage = () => {
  return (
    <DocsContent
      title="Introduction to Core Microservices"
      breadcrumbs={[
        { label: 'Documentation', path: '/docs/introduction' },
        { label: 'Introduction' },
      ]}
    >
      <p className="lead">
        Core Microservices is a production-ready toolkit that provides enterprise-grade backend services 
        and a modular frontend—helping teams launch applications faster without sacrificing quality or scalability.
      </p>

      <h2 className="h3 mt-5 mb-3">What is Core Microservices?</h2>
      <p>
        Core Microservices (CoreMS) eliminates months of infrastructure development by providing battle-tested, 
        production-ready services that work together seamlessly. Instead of building authentication, user management, 
        file storage, and communication systems from scratch—you get a complete foundation that scales with your business.
      </p>

      <p>
        Built with modern enterprise patterns: microservices architecture, JWT authentication, role-based access control, 
        message queuing, and cloud-native deployment. Everything you need to go from idea to production quickly.
      </p>

      <h2 className="h3 mt-5 mb-3">Who is this for?</h2>

      <p>
        Core Microservices is built for teams who need enterprise-grade infrastructure without enterprise complexity. 
        Whether you're building a SaaS platform, an e-commerce site, a logistics system, or a customer portal—if you 
        need user management, secure file storage, multi-channel communications, or internationalization, you can 
        start here and scale as you grow.
      </p>

      <div className="row g-3 my-4">
        <div className="col-md-4">
          <div className="p-3 border rounded h-100">
            <h4 className="h6 fw-bold">Development Teams</h4>
            <p className="small mb-0">
              Skip 3-6 months of infrastructure work. Start building business features on day one with 
              proven architecture that scales.
            </p>
          </div>
        </div>
        <div className="col-md-4">
          <div className="p-3 border rounded h-100">
            <h4 className="h6 fw-bold">Product Companies</h4>
            <p className="small mb-0">
              Launch faster with ready-to-use services. Focus on what makes your product unique, 
              not reinventing authentication and file storage.
            </p>
          </div>
        </div>
        <div className="col-md-4">
          <div className="p-3 border rounded h-100">
            <h4 className="h6 fw-bold">Technical Founders</h4>
            <p className="small mb-0">
              Get to market quickly with production-ready infrastructure. Build your MVP in weeks, 
              not months.
            </p>
          </div>
        </div>
      </div>

      <Callout type="info" title="Real-World Applications">
        Perfect for e-commerce platforms, logistics systems, SaaS applications, customer portals, mobile app 
        backends, and internal tools. Any application that needs user accounts, file management, and notifications.
      </Callout>

      <Callout type="tip" title="Free & Open Source">
        Licensed under Apache 2.0. Use it commercially without restrictions. No vendor lock-in, no hidden costs.
      </Callout>

      <h2 className="h3 mt-5 mb-3">Key Benefits</h2>
      
      <h3 className="h5 mt-4 mb-2">Launch 10x Faster</h3>
      <p>
        What typically takes 3-6 months to build is ready in minutes. Authentication with OAuth2, user management, 
        file storage, email/SMS notifications, and multi-language support—all working out of the box.
      </p>

      <h3 className="h5 mt-4 mb-2">Production-Ready from Day One</h3>
      <p>
        Built with enterprise patterns: microservices architecture, JWT authentication, role-based access control, 
        message queuing, and cloud-native deployment. Security, scalability, and observability included.
      </p>

      <h3 className="h5 mt-4 mb-2">Modular & Customizable</h3>
      <p>
        Use only what you need. Each service is independent and can be deployed separately. Customize the frontend 
        components to match your brand. Add more services as your application grows.
      </p>

      <h3 className="h5 mt-4 mb-2">No Vendor Lock-In</h3>
      <p>
        Apache 2.0 license means you own the code. Deploy anywhere—AWS, Azure, GCP, or on-premises. 
        No subscriptions, no usage limits, no hidden costs.
      </p>

      <h2 className="h3 mt-5 mb-3">What's Included?</h2>
      
      <div className="row g-3 my-3">
        <div className="col-md-6">
          <div className="p-3 border rounded">
            <h4 className="h6 fw-bold">User Management Service</h4>
            <p className="small mb-0">
              Complete authentication with JWT, OAuth2 (Google, GitHub, LinkedIn), role-based access 
              control, and user profiles.
            </p>
          </div>
        </div>
        <div className="col-md-6">
          <div className="p-3 border rounded">
            <h4 className="h6 fw-bold">Communication Service</h4>
            <p className="small mb-0">
              Multi-channel messaging: email, SMS, push notifications. Template management and 
              delivery tracking included.
            </p>
          </div>
        </div>
        <div className="col-md-6">
          <div className="p-3 border rounded">
            <h4 className="h6 fw-bold">Document Service</h4>
            <p className="small mb-0">
              S3-compatible storage with MinIO. Upload, organize, and secure files with built-in 
              access control.
            </p>
          </div>
        </div>
        <div className="col-md-6">
          <div className="p-3 border rounded">
            <h4 className="h6 fw-bold">Translation Service</h4>
            <p className="small mb-0">
              Multi-language support with translation management and dynamic content localization.
            </p>
          </div>
        </div>
      </div>

      <Callout type="info" title="Coming Soon">
        Payment processing, billing management, dynamic forms, template engine, and more services 
        are in active development.
      </Callout>

      <h2 className="h3 mt-5 mb-3">Technology Stack</h2>
      
      <h3 className="h5 mt-4 mb-2">Backend</h3>
      <ul>
        <li><strong>Java 25</strong> with <strong>Spring Boot 4.0</strong></li>
        <li><strong>PostgreSQL</strong> for data persistence</li>
        <li><strong>RabbitMQ</strong> for message queuing</li>
        <li><strong>Docker</strong> for containerization</li>
        <li><strong>JWT</strong> for authentication</li>
      </ul>

      <h3 className="h5 mt-4 mb-2">Frontend</h3>
      <ul>
        <li><strong>React 18+</strong> with <strong>TypeScript</strong></li>
        <li><strong>Vite</strong> for fast development</li>
        <li><strong>React Bootstrap</strong> for UI components</li>
        <li><strong>React Router</strong> for navigation</li>
        <li>Modular architecture for easy customization</li>
      </ul>

      <h3 className="h5 mt-4 mb-2">DevOps</h3>
      <ul>
        <li><strong>Docker Compose</strong> for local development</li>
        <li><strong>GitHub Actions</strong> for CI/CD</li>
        <li><strong>Terraform</strong> for infrastructure as code</li>
        <li><strong>Kubernetes</strong> ready for cloud deployment</li>
      </ul>

      <h2 className="h3 mt-5 mb-3">Quick Example</h2>
      <p>Get the entire stack running in minutes:</p>

      <CodeBlock language="bash">
{`# Clone the repository
git clone https://github.com/CoreWebMicroservices/corems-project.git
cd corems-project

# Initialize all services
./setup.sh init-all

# Start infrastructure (PostgreSQL, RabbitMQ, MinIO)
./setup.sh infra

# Build all services
./setup.sh build all

# Run migrations
./setup.sh migrate --mockdata

# Start all services
./setup.sh start-all`}
      </CodeBlock>

      <p className="mt-3">
        That's it! Your entire microservices platform is now running with sample data.
      </p>

      <Callout type="tip" title="Next Steps">
        Check out the <a href="/docs/quick-start">Quick Start Guide</a> for detailed setup instructions, 
        or explore the <a href="/docs/architecture">Architecture Overview</a> to understand how everything 
        works together.
      </Callout>
    </DocsContent>
  );
};

export default IntroductionPage;
