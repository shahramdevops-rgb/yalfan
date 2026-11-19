import { useQuery } from '@tanstack/react-query';
import { api } from '../../shared/api/client';
import { ProblemError, toProblem } from '../../shared/api/problem';
import { errorMessage, t } from '../../shared/i18n/fa';

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
      <h1 className="text-2xl font-bold">{t('nav.dashboard')}</h1>

      <section className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-stone-200">
        {me.isPending && <div className="h-6 w-48 animate-pulse rounded bg-stone-200" />}
        {me.isError && <p className="text-red-700">{errorMessage(toProblem(me.error).code)}</p>}
        {me.data && (
          <>
            <p className="text-lg font-medium">{t('dashboard.greeting', { name: me.data.fullName })}</p>
            <p className="mt-2 text-stone-500">{t('dashboard.empty')}</p>
          </>
        )}
      </section>
    </div>
  );
}
