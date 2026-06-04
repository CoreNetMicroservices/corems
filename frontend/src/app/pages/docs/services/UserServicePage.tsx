import { DocsContent, CodeBlock, Callout } from '@/common/component/docs';
import { Badge } from 'react-bootstrap';

const UserServicePage = () => {
  return (
    <DocsContent
      title="User Management Service"
      breadcrumbs={[
        { label: 'Documentation', path: '/docs/introduction' },
        { label: 'Services', path: '/docs/services' },
        { label: 'User Service' },
      ]}
    >
      <p className="lead">
        OAuth2/OIDC Authorization Server with user authentication, authorization, and profile management.
      </p>

      <div className="mb-4">
        <Badge bg="success" className="me-2">Production Ready</Badge>
        <Badge bg="secondary">Port: 3000</Badge>
      </div>

      <h2 className="h4 mt-5 mb-3">Key Features</h2>
      <ul>
        <li><strong>OAuth2 Authorization Server</strong> with standard endpoints</li>
        <li><strong>OpenID Connect (OIDC)</strong> provider with ID tokens</li>
        <li><strong>PKCE</strong> support for secure authorization code flow</li>
        <li><strong>JWT token management</strong> (HS256/RS256 algorithms)</li>
        <li><strong>Social OAuth2</strong> (Google, GitHub, LinkedIn)</li>
        <li><strong>Role-based access control</strong> (RBAC)</li>
        <li><strong>Email/phone verification</strong></li>
        <li><strong>Password reset flow</strong></li>
      </ul>

      <h2 className="h4 mt-5 mb-3">Grant Types</h2>
      <ul>
        <li><strong>Password Grant</strong> - First-party applications (username/password)</li>
        <li><strong>Authorization Code Grant</strong> - Third-party applications with PKCE</li>
        <li><strong>Refresh Token Grant</strong> - Token renewal without re-authentication</li>
      </ul>

      <h2 className="h4 mt-5 mb-3">Quick Start</h2>
      <CodeBlock language="bash">
{`# Start user service
./setup.sh start user-ms

# View logs
./setup.sh logs user-ms`}
      </CodeBlock>

      <h2 className="h4 mt-5 mb-3">Authentication Example</h2>
      <CodeBlock language="bash">
{`# Sign in with username/password
curl -X POST http://localhost:3000/oauth2/token \\
  -H "Content-Type: application/x-www-form-urlencoded" \\
  -d "grant_type=password" \\
  -d "username=user@example.com" \\
  -d "password=SecurePass123!" \\
  -d "scope=openid profile email"

# Response
{
  "access_token": "eyJ0eXAiOiJhY2Nlc3NfdG9rZW4i...",
  "refresh_token": "eyJ0eXAiOiJyZWZyZXNoX3Rva2VuI...",
  "id_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 600
}`}
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
            <td><code>/oauth2/token</code></td>
            <td>POST</td>
            <td>Token endpoint (all grant types)</td>
          </tr>
          <tr>
            <td><code>/oauth2/userinfo</code></td>
            <td>GET</td>
            <td>OIDC UserInfo endpoint</td>
          </tr>
          <tr>
            <td><code>/api/auth/signup</code></td>
            <td>POST</td>
            <td>Register new user</td>
          </tr>
          <tr>
            <td><code>/api/users</code></td>
            <td>GET</td>
            <td>List all users (admin)</td>
          </tr>
          <tr>
            <td><code>/api/profile</code></td>
            <td>PATCH</td>
            <td>Update own profile</td>
          </tr>
        </tbody>
      </table>

      <h2 className="h4 mt-5 mb-3">Token Configuration</h2>
      <table className="table table-sm">
        <thead>
          <tr>
            <th>Token Type</th>
            <th>TTL</th>
            <th>Algorithm</th>
            <th>Revocable</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td>Access Token</td>
            <td>10 minutes</td>
            <td>HS256/RS256</td>
            <td>No</td>
          </tr>
          <tr>
            <td>Refresh Token</td>
            <td>24 hours</td>
            <td>HS256/RS256</td>
            <td>Yes</td>
          </tr>
          <tr>
            <td>ID Token</td>
            <td>60 minutes</td>
            <td>HS256/RS256</td>
            <td>No</td>
          </tr>
        </tbody>
      </table>

      <Callout type="info" title="Security Best Practices">
        Use <strong>RS256</strong> for production with third-party clients. Keep access tokens short-lived 
        and implement refresh token rotation.
      </Callout>

      <h2 className="h4 mt-5 mb-3">Database Schema</h2>
      <p>Schema: <code>user_ms</code></p>
      <ul>
        <li><code>app_user</code> - User accounts with credentials</li>
        <li><code>app_user_role</code> - User role assignments</li>
        <li><code>login_token</code> - Refresh tokens (revocable)</li>
        <li><code>authorization_codes</code> - OAuth2 authorization codes</li>
        <li><code>action_token</code> - Email/phone verification tokens</li>
      </ul>
    </DocsContent>
  );
};

export default UserServicePage;
