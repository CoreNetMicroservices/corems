import { DocsContent, CodeBlock, Callout } from '@/common/component/docs';
import { Badge } from 'react-bootstrap';

const DocumentServicePage = () => {
  return (
    <DocsContent
      title="Document Management Service"
      breadcrumbs={[
        { label: 'Documentation', path: '/docs/introduction' },
        { label: 'Services', path: '/docs/services' },
        { label: 'Document' },
      ]}
    >
      <p className="lead">
        File upload, storage, and access control microservice with S3-compatible storage.
      </p>

      <div className="mb-4">
        <Badge bg="success" className="me-2">Production Ready</Badge>
        <Badge bg="secondary">Port: 3002</Badge>
      </div>

      <h2 className="h4 mt-5 mb-3">Key Features</h2>
      <ul>
        <li><strong>File upload</strong> (multipart/base64)</li>
        <li><strong>Document metadata management</strong></li>
        <li><strong>Access control and permissions</strong></li>
        <li><strong>Public/private document sharing</strong></li>
        <li><strong>S3-compatible storage</strong> (MinIO)</li>
        <li><strong>Document tagging</strong> and organization</li>
        <li><strong>Temporary access tokens</strong></li>
      </ul>

      <h2 className="h4 mt-5 mb-3">Quick Start</h2>
      <CodeBlock language="bash">
{`# Start document service
./setup.sh start document-ms

# View logs
./setup.sh logs document-ms`}
      </CodeBlock>

      <h2 className="h4 mt-5 mb-3">Upload File Example</h2>
      <CodeBlock language="bash">
{`curl -X POST http://localhost:3002/api/documents/upload \\
  -H "Authorization: Bearer <token>" \\
  -F "file=@/path/to/document.pdf" \\
  -F "name=Important Document" \\
  -F "isPublic=false"

# Response
{
  "uuid": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Important Document",
  "fileName": "document.pdf",
  "contentType": "application/pdf",
  "size": 102400,
  "isPublic": false,
  "uploadedAt": "2025-01-17T10:30:00Z"
}`}
      </CodeBlock>

      <h2 className="h4 mt-5 mb-3">Download File Example</h2>
      <CodeBlock language="bash">
{`curl -X GET http://localhost:3002/api/documents/{uuid}/download \\
  -H "Authorization: Bearer <token>" \\
  -o downloaded-file.pdf`}
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
            <td><code>/api/documents/upload</code></td>
            <td>POST</td>
            <td>Upload new document</td>
          </tr>
          <tr>
            <td><code>/api/documents/{'{uuid}'}/download</code></td>
            <td>GET</td>
            <td>Download document</td>
          </tr>
          <tr>
            <td><code>/api/documents</code></td>
            <td>GET</td>
            <td>List user documents</td>
          </tr>
          <tr>
            <td><code>/api/documents/{'{uuid}'}</code></td>
            <td>GET</td>
            <td>Get document metadata</td>
          </tr>
          <tr>
            <td><code>/api/documents/{'{uuid}'}</code></td>
            <td>DELETE</td>
            <td>Delete document</td>
          </tr>
        </tbody>
      </table>

      <h2 className="h4 mt-5 mb-3">Environment Configuration</h2>
      <CodeBlock language="bash">
{`# S3 Storage (MinIO)
S3_ENDPOINT=http://localhost:9000
S3_ACCESS_KEY=minioadmin
S3_SECRET_KEY=minioadmin
S3_BUCKET_NAME=documents

# Database
DATABASE_URL=jdbc:postgresql://localhost:5432/corems`}
      </CodeBlock>

      <Callout type="info" title="Storage Backend">
        Uses MinIO for local development, but works with any S3-compatible storage 
        (AWS S3, DigitalOcean Spaces, etc.) by changing the endpoint.
      </Callout>

      <h2 className="h4 mt-5 mb-3">Database Schema</h2>
      <p>Schema: <code>document_ms</code></p>
      <ul>
        <li><code>document</code> - Document metadata and ownership</li>
        <li><code>document_tags</code> - Document tagging for organization</li>
        <li><code>document_access_token</code> - Temporary access tokens for sharing</li>
      </ul>

      <h2 className="h4 mt-5 mb-3">Access Control</h2>
      <ul>
        <li><strong>Private documents</strong> - Only owner can access</li>
        <li><strong>Public documents</strong> - Anyone with link can access</li>
        <li><strong>Temporary tokens</strong> - Time-limited access for sharing</li>
        <li><strong>Role-based access</strong> - Admin can access all documents</li>
      </ul>

      <h2 className="h4 mt-5 mb-3">Supported File Types</h2>
      <p>All file types are supported. Common types include:</p>
      <ul>
        <li>Documents: PDF, DOC, DOCX, TXT</li>
        <li>Images: JPG, PNG, GIF, SVG</li>
        <li>Archives: ZIP, RAR, TAR</li>
        <li>Data: CSV, JSON, XML</li>
      </ul>
    </DocsContent>
  );
};

export default DocumentServicePage;
