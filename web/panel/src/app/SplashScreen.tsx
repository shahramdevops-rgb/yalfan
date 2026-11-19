import { t } from '../shared/i18n/fa';
import { BagMark } from '../shared/ui/icons';

/** Shown while the panel checks the refresh cookie at start-up, so the login form never flashes. */
export function SplashScreen() {
  return (
    <div className="flex min-h-dvh flex-col items-center justify-center gap-4" role="status">
      <BagMark className="size-12 animate-pulse" />
      <p className="text-sm text-stone-500">{t('app.loading')}</p>
    </div>
  );
}
