import { t } from '../shared/i18n/fa';
import { TextLink } from '../shared/ui/Links';
import { PageHeader } from '../shared/ui/PageHeader';

export function NotFoundPage() {
  return (
    <div className="flex flex-col items-start gap-4 py-10">
      <PageHeader title={t('notFound.title')} />
      <TextLink to="/">{t('notFound.back')}</TextLink>
    </div>
  );
}
