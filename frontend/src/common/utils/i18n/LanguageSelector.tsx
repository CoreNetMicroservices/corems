import { Dropdown, Nav } from 'react-bootstrap';
import { Globe } from 'react-bootstrap-icons';
import { useI18nContext } from './I18nContext';

/**
 * LanguageSelector displays a navbar dropdown of available languages.
 * It uses the I18nContext to get the languages list and change language.
 * 
 * Must be used within an I18nProvider.
 */
export const LanguageSelector = () => {
  const { languages, currentLanguage, changeLanguage } = useI18nContext();

  return (
    <Dropdown as={Nav.Item} align="end">
      <Dropdown.Toggle as={Nav.Link} id="language-selector-dropdown">
        <Globe size={20} />
      </Dropdown.Toggle>
      <Dropdown.Menu style={{ minWidth: 'unset' }}>
        {languages.map((lang) => (
          <Dropdown.Item
            key={lang}
            active={currentLanguage === lang}
            onClick={() => changeLanguage(lang)}
          >
            {lang.toUpperCase()}
          </Dropdown.Item>
        ))}
      </Dropdown.Menu>
    </Dropdown>
  );
};

export default LanguageSelector;
