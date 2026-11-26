import { useQuery } from '@tanstack/react-query';
import { api } from '../../shared/api/client';
import { ProblemError, toProblem } from '../../shared/api/problem';
import { errorMessage, t } from '../../shared/i18n/fa';
import { Card } from '../../shared/ui/Card';
import { PageHeader } from '../../shared/ui/PageHeader';

/** Empty until the modules arrive; it already reads the user through the typed client. */
export function DashboardPage() {
  const me = useQuery({
    queryKey: ['me'],
    queryFn: async () => {
      const { data, error } = await api.GET('/panel/auth/me');
      if (!data) {
        throw new ProblemError(toProblem(error));
      }
      return data;
    },
  });

  return (
    <div className="flex flex-col gap-6">
      <PageHeader title={t('nav.dashboard')} />

      <Card>
        {me.isPending && <div className="h-6 w-48 animate-pulse rounded bg-fill-strong" />}
        {me.isError && <p className="text-danger">{errorMessage(toProblem(me.error).code)}</p>}
        {me.data && (
          <div>
            <p className="text-lg font-medium">{t('dashboard.greeting', { name: me.data.fullName })}</p>
            <p className="mt-2 text-muted">{t('dashboard.empty')}</p>
          </div>
        )}
      </Card>
    </div>
  );
}
