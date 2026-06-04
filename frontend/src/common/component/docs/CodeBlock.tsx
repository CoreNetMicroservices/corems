import { useState } from 'react';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { vscDarkPlus, vs } from 'react-syntax-highlighter/dist/esm/styles/prism';
import { Button } from 'react-bootstrap';

interface CodeBlockProps {
  children: string;
  language?: string;
  showLineNumbers?: boolean;
}

export const CodeBlock: React.FC<CodeBlockProps> = ({ 
  children, 
  language = 'typescript',
  showLineNumbers = false 
}) => {
  const [copied, setCopied] = useState(false);
  const isDark = document.documentElement.getAttribute('data-bs-theme') === 'dark';

  const handleCopy = async () => {
    await navigator.clipboard.writeText(children);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="position-relative my-3">
      <div className="d-flex justify-content-between align-items-center px-3 py-2 bg-body-secondary border-bottom">
        <small className="text-muted text-uppercase fw-bold">{language}</small>
        <Button 
          variant="link" 
          size="sm" 
          onClick={handleCopy}
          className="text-decoration-none p-0"
        >
          <i className={`bi bi-${copied ? 'check' : 'clipboard'} me-1`}></i>
          {copied ? 'Copied!' : 'Copy'}
        </Button>
      </div>
      <SyntaxHighlighter
        language={language}
        style={isDark ? vscDarkPlus : vs}
        showLineNumbers={showLineNumbers}
        customStyle={{
          margin: 0,
          borderRadius: 0,
          fontSize: '0.875rem',
        }}
      >
        {children}
      </SyntaxHighlighter>
    </div>
  );
};

export default CodeBlock;
