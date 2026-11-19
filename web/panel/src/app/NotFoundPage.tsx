import { Link } from 'react-router';
import { t } from '../shared/i18n/fa';

export function NotFoundPage() {
  return (
    <div className="flex flex-col items-start gap-4 py-10">
      <h1 className="text-xl font-bold">{t('notFound.title')}</h1>
      <Link to="/" className="text-brand-700 hover:underline">
        {t('notFound.back')}
      </Link>
    </div>
  );
}
