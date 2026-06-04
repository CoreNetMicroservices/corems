import { Outlet } from 'react-router-dom';

import AppLayout from '@/app/layout/AppLayout';
import { I18nProvider } from '@/common/utils/i18n/I18nProvider';
import ScrollTo from '@/common/component/ScrollTo';

// Default translations - for no authorized users and initial load
import en from '@/app/locales/en.json';
import no from '@/app/locales/no.json';

// Import loader functions from translation module - these merge additional translations from API
// Remove these imports if not using the translation module (defaults will still work)
import { fetchTranslations, fetchAvailableLanguages } from '@/translation/utils/TranslationApi';

const App = () => {

  return (
    <I18nProvider
      realm="corems"
      defaultLanguage="en"
      // Static defaults load immediately, API translations merge on top after fetch
      staticTranslations={{ en, no }}
      availableLanguages={['en', 'no']}
      loader={fetchTranslations}
      languagesLoader={fetchAvailableLanguages}
    >
      <ScrollTo />
      <AppLayout>
        <Outlet />
      </AppLayout>
    </I18nProvider>
  );
}

export default App;
