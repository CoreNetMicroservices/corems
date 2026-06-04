export interface DocsNavItem {
  label: string;
  path: string;
}

export interface DocsNavSection {
  section: string;
  items: DocsNavItem[];
}
