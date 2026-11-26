import { toProblem } from '../api/problem';
import { errorMessage, t } from '../i18n/fa';
import { Button } from './Button';

/** Grey bars where a list's rows will be. */
export function ListSkeleton({ rows = 4 }: { rows?: number }) {
  return (
    <div className="flex flex-col gap-3 py-2" aria-busy>
      {Array.from({ length: rows }, (_, index) => (
        <div key={index} className="h-8 animate-pulse rounded bg-fill" />
      ))}
    </div>
  );
}

/** Why a list did not load, and a way to try again on a slow line. */
export function LoadError({ error, onRetry }: { error: unknown; onRetry: () => void }) {
  return (
    <div className="flex flex-col items-start gap-3 py-2">
      <p role="alert" className="text-danger">
        {errorMessage(toProblem(error).code)}
      </p>
      <Button variant="secondary" onClick={onRetry}>
        {t('list.retry')}
      </Button>
    </div>
  );
}
