import { DocsContent, CodeBlock, Callout } from '@/common/component/docs';
import { Badge } from 'react-bootstrap';

const CommunicationServicePage = () => {
  return (
    <DocsContent
      title="Communication Service"
      breadcrumbs={[
        { label: 'Documentation', path: '/docs/introduction' },
        { label: 'Services', path: '/docs/services' },
        { label: 'Communication' },
      ]}
    >
      <p className="lead">
        Email, SMS, and notification management microservice with template support and message queuing.
      </p>

      <div className="mb-4">
        <Badge bg="success" className="me-2">Production Ready</Badge>
        <Badge bg="secondary">Port: 3001</Badge>
      </div>

      <h2 className="h4 mt-5 mb-3">Key Features</h2>
      <ul>
        <li><strong>Email sending</strong> (HTML/Text) via SMTP</li>
        <li><strong>SMS notifications</strong> via Twilio</li>
        <li><strong>Slack integration</strong> for team notifications</li>
        <li><strong>Message queuing</strong> with RabbitMQ</li>
        <li><strong>Template management</strong> for reusable content</li>
        <li><strong>Delivery tracking</strong> and status updates</li>
        <li><strong>Multi-channel messaging</strong></li>
      </ul>

      <h2 className="h4 mt-5 mb-3">Quick Start</h2>
      <CodeBlock language="bash">
{`# Start communication service
./setup.sh start communication-ms

# View logs
./setup.sh logs communication-ms`}
      </CodeBlock>

      <h2 className="h4 mt-5 mb-3">Send Email Example</h2>
      <CodeBlock language="bash">
{`curl -X POST http://localhost:3001/api/messages/email \\
  -H "Authorization: Bearer <token>" \\
  -H "Content-Type: application/json" \\
  -d '{
    "to": ["user@example.com"],
    "subject": "Welcome to CoreMS",
    "body": "<h1>Welcome!</h1><p>Thanks for joining.</p>",
    "isHtml": true
  }'`}
      </CodeBlock>

      <h2 className="h4 mt-5 mb-3">Send SMS Example</h2>
      <CodeBlock language="bash">
{`curl -X POST http://localhost:3001/api/messages/sms \\
  -H "Authorization: Bearer <token>" \\
  -H "Content-Type: application/json" \\
  -d '{
    "to": "+1234567890",
    "message": "Your verification code is: 123456"
  }'`}
      </CodeBlock>

      <h2 className="h4 mt-5 mb-3">Main Endpoints</h2>
      <table className="table table-sm">
        <thead>
          <tr>
            <th>Endpoint</th>
            <th>Method</th>
            <th>Description</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td><code>/api/messages/email</code></td>
            <td>POST</td>
            <td>Send email message</td>
          </tr>
          <tr>
            <td><code>/api/messages/sms</code></td>
            <td>POST</td>
            <td>Send SMS message</td>
          </tr>
          <tr>
            <td><code>/api/messages</code></td>
            <td>GET</td>
            <td>List sent messages</td>
          </tr>
          <tr>
            <td><code>/api/messages/{'{id}'}</code></td>
            <td>GET</td>
            <td>Get message details</td>
          </tr>
        </tbody>
      </table>

      <h2 className="h4 mt-5 mb-3">Environment Configuration</h2>
      <CodeBlock language="bash">
{`# Email Configuration
MAIL_HOST=smtp.gmail.com
MAIL_USERNAME=your_email@gmail.com
MAIL_PASSWORD=your_app_password

# SMS Configuration (Twilio)
SMS_ACCOUNT_SID=your_twilio_sid
SMS_AUTH_TOKEN=your_twilio_token
SMS_FROM_NUMBER=+1234567890

# RabbitMQ
RABBIT_DEFAULT_QUEUE=communication_queue`}
      </CodeBlock>

      <Callout type="tip" title="Message Queuing">
        Messages are queued in RabbitMQ for reliable delivery. Failed messages are automatically 
        retried with exponential backoff.
      </Callout>

      <h2 className="h4 mt-5 mb-3">Database Schema</h2>
      <p>Schema: <code>communication_ms</code></p>
      <ul>
        <li><code>message</code> - Message records with status</li>
        <li><code>email</code> - Email-specific details</li>
        <li><code>sms</code> - SMS-specific details</li>
        <li><code>email_attachment</code> - File attachments</li>
      </ul>

      <h2 className="h4 mt-5 mb-3">Message Status</h2>
      <ul>
        <li><strong>PENDING</strong> - Queued for sending</li>
        <li><strong>SENT</strong> - Successfully delivered</li>
        <li><strong>FAILED</strong> - Delivery failed</li>
        <li><strong>RETRY</strong> - Scheduled for retry</li>
      </ul>
    </DocsContent>
  );
};

export default CommunicationServicePage;
