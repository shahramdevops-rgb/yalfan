import { Link } from 'react-router';
import { t } from '../../shared/i18n/fa';
import { useSession } from '../auth/useSession';
import type { HistoryKind } from './historyApi';

/**
 * «تاریخچه» at the top of a model's, contractor's or worker's page, for the owner alone: the
 * others never see it, and the API refuses them the history as well (ADR 0027).
 */
export function HistoryLink({ kind, id }: { kind: HistoryKind; id: string }) {
  const session = useSession();
  if (session.status !== 'signedIn' || session.user.role !== 'owner') {
    return null;
  }

  return (
    <Link to={`/${kind}/${id}/history`} className="text-sm text-brand-700 hover:underline">
      {t('history.link')}
    </Link>
  );
}
