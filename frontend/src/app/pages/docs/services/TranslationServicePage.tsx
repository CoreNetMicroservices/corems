import { DocsContent, CodeBlock, Callout } from '@/common/component/docs';
import { Badge } from 'react-bootstrap';

const TranslationServicePage = () => {
  return (
    <DocsContent
      title="Translation Service"
      breadcrumbs={[
        { label: 'Documentation', path: '/docs/introduction' },
        { label: 'Services', path: '/docs/services' },
        { label: 'Translation' },
      ]}
    >
      <p className="lead">
        Comprehensive internationalization (i18n) support for multi-language applications with dynamic content localization.
      </p>

      <div className="mb-4">
        <Badge bg="success" className="me-2">Production Ready</Badge>
        <Badge bg="secondary">Port: 3003</Badge>
      </div>

      <h2 className="h4 mt-5 mb-3">Key Features</h2>
      <ul>
        <li><strong>Multi-language support</strong> for global applications</li>
        <li><strong>Translation management UI</strong> for easy updates</li>
        <li><strong>Dynamic content localization</strong></li>
        <li><strong>Realm-based organization</strong> for multi-tenant apps</li>
        <li><strong>Bulk import/export</strong> capabilities</li>
        <li><strong>Translation history</strong> and versioning</li>
        <li><strong>Fallback language</strong> support</li>
      </ul>

      <h2 className="h4 mt-5 mb-3">Quick Start</h2>
      <CodeBlock language="bash">
{`# Start translation service
./setup.sh start translation-ms

# View logs
./setup.sh logs translation-ms`}
      </CodeBlock>

      <h2 className="h4 mt-5 mb-3">Get Translations Example</h2>
      <CodeBlock language="bash">
{`# Get all translations for a language
curl http://localhost:3003/api/translation/corems/en

# Response
{
  "app.title": "CoreMS",
  "nav.home": "Home",
  "nav.users": "Users",
  "auth.login": "Login",
  "auth.logout": "Logout"
}`}
      </CodeBlock>

      <h2 className="h4 mt-5 mb-3">Frontend Integration</h2>
      <CodeBlock language="javascript">
{`// React i18n integration
import { useTranslation } from 'react-i18next';

function MyComponent() {
  const { t } = useTranslation();
  
  return (
    <div>
      <h1>{t('app.title')}</h1>
      <button>{t('auth.login')}</button>
    </div>
  );
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
            <td><code>/api/translation/{'{realm}'}/{'{lang}'}</code></td>
            <td>GET</td>
            <td>Get all translations for language</td>
          </tr>
          <tr>
            <td><code>/api/translations</code></td>
            <td>GET</td>
            <td>List all translations (admin)</td>
          </tr>
          <tr>
            <td><code>/api/translations</code></td>
            <td>POST</td>
            <td>Create new translation</td>
          </tr>
          <tr>
            <td><code>/api/translations/{'{id}'}</code></td>
            <td>PUT</td>
            <td>Update translation</td>
          </tr>
          <tr>
            <td><code>/api/languages</code></td>
            <td>GET</td>
            <td>List supported languages</td>
          </tr>
        </tbody>
      </table>

      <h2 className="h4 mt-5 mb-3">Environment Configuration</h2>
      <CodeBlock language="bash">
{`# Translation Configuration
TRANSLATION_DEFAULT_LANGUAGE=en
TRANSLATION_FALLBACK_LANGUAGE=en

# Database
DATABASE_URL=jdbc:postgresql://localhost:5432/corems

# Redis (optional caching)
SPRING_REDIS_HOST=localhost
SPRING_REDIS_PORT=6379`}
      </CodeBlock>

      <Callout type="tip" title="Realm-Based Translations">
        Organize translations by realm (e.g., "corems", "admin", "customer-portal") to support 
        multi-tenant applications with different translation sets.
      </Callout>

      <h2 className="h4 mt-5 mb-3">Database Schema</h2>
      <p>Schema: <code>translation_ms</code></p>
      <ul>
        <li><code>languages</code> - Supported languages and configurations</li>
        <li><code>translation_keys</code> - Translation key definitions</li>
        <li><code>translations</code> - Translation values per language</li>
        <li><code>translation_history</code> - Change history and versioning</li>
      </ul>

      <h2 className="h4 mt-5 mb-3">Supported Languages</h2>
      <p>Currently configured languages:</p>
      <ul>
        <li><strong>English (en)</strong> - Default</li>
        <li><strong>Spanish (es)</strong></li>
        <li><strong>French (fr)</strong></li>
        <li><strong>German (de)</strong></li>
        <li><strong>Russian (ru)</strong></li>
      </ul>

      <Callout type="info">
        New languages can be added dynamically through the API without code changes.
      </Callout>

      <h2 className="h4 mt-5 mb-3">Bulk Operations</h2>
      <CodeBlock language="bash">
{`# Export translations for a language
curl http://localhost:3003/api/translations/export/en \\
  -H "Authorization: Bearer <token>" \\
  -o translations-en.json

# Import translations from file
curl -X POST http://localhost:3003/api/translations/import \\
  -H "Authorization: Bearer <token>" \\
  -F "file=@translations-es.json" \\
  -F "language=es"`}
      </CodeBlock>
    </DocsContent>
  );
};

export default TranslationServicePage;
