import { useRouteError } from 'react-router';
import { t } from '../shared/i18n/fa';
import { Button } from '../shared/ui/Button';

/** A page crashed. The details go to the console for the developer, never to the screen. */
export function ErrorPage() {
  console.error(useRouteError());

  return (
    <div className="flex min-h-dvh flex-col items-center justify-center gap-4 px-4 text-center">
      <h1 className="text-xl font-bold">{t('error.title')}</h1>
      <Button variant="secondary" onClick={() => window.location.reload()}>
        {t('error.reload')}
      </Button>
    </div>
  );
}
