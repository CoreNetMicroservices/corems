import { DocsNavSection } from '@/common/component/docs';

export const docsNavigation: DocsNavSection[] = [
  {
    section: 'Getting Started',
    items: [
      { label: 'Introduction', path: '/docs/introduction' },
      { label: 'Quick Start', path: '/docs/quick-start' },
      { label: 'Architecture', path: '/docs/architecture' },
    ],
  },
  {
    section: 'Core Concepts',
    items: [
      { label: 'Common Libraries', path: '/docs/common-libraries' },
    ],
  },
  {
    section: 'Services',
    items: [
      { label: 'Overview', path: '/docs/services' },
      { label: 'User Management', path: '/docs/services/user' },
      { label: 'Communication', path: '/docs/services/communication' },
      { label: 'Document Management', path: '/docs/services/document' },
      { label: 'Translation', path: '/docs/services/translation' },
    ],
  },
  {
    section: 'Roadmap',
    items: [
      { label: 'Current & Planned', path: '/docs/roadmap' },
    ],
  },
];
