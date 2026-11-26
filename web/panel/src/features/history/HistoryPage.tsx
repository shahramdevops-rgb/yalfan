import { useInfiniteQuery, useQuery } from '@tanstack/react-query';
import { useId } from 'react';
import { Link, useParams } from 'react-router';
import { NotFoundPage } from '../../app/NotFoundPage';
import { toProblem } from '../../shared/api/problem';
import { formatDate, formatTime } from '../../shared/format/dates';
import { toPersianDigits } from '../../shared/format/digits';
import { t } from '../../shared/i18n/fa';
import { Button } from '../../shared/ui/Button';
import { Card } from '../../shared/ui/Card';
import { Money } from '../../shared/ui/Money';
import { ListSkeleton, LoadError } from '../../shared/ui/QueryStates';
import { useSession } from '../auth/useSession';
import { productKey, productsApi, type ProductDetails } from '../catalog/productsApi';
import { partiesApi, partyKey, type PartyDetails } from '../parties/partiesApi';
import { historyApi, historyKey, type HistoryKind, type HistorySave } from './historyApi';
import { describeSave, type HistoryLine } from './historyLines';

const missingCodes = ['catalog.product_not_found', 'parties.contractor_not_found', 'parties.worker_not_found'];

/**
 * The owner's change history of a model, a contractor or a worker (ADR 0027): one card per save,
 * newest first, with who saved it and when, and each change in the workshop's words. Twenty saves
 * at a time; «قدیمی‌ترها» brings the next twenty. Only an owner sees it; the API refuses everyone
 * else as well.
 */
export function HistoryPage({ kind }: { kind: HistoryKind }) {
  const session = useSession();
  if (session.status !== 'signedIn' || session.user.role !== 'owner') {
    return <NotFoundPage />;
  }

  return <OwnerHistory kind={kind} />;
}

function OwnerHistory({ kind }: { kind: HistoryKind }) {
  const { id = '' } = useParams();
  const headingId = useId();
  const title = useRecordTitle(kind, id);
  // Read again each time the page opens: the record may have changed since it was last open.
  const history = useInfiniteQuery({
    queryKey: historyKey(kind, id),
    queryFn: ({ pageParam }) => historyApi[kind](id, pageParam),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (page) => page.before ?? undefined,
  });

  const back = (
    <Link to={`/${kind}/${id}`} className="self-start text-sm text-brand-700 hover:underline">
      {t(`history.back.${kind}`)}
    </Link>
  );

  if (history.isPending) {
    return (
      <div className="flex max-w-3xl flex-col gap-4">
        {back}
        <ListSkeleton rows={4} />
      </div>
    );
  }

  if (history.isError) {
    const missing = missingCodes.includes(toProblem(history.error).code ?? '');
    return (
      <div className="flex max-w-3xl flex-col gap-4">
        {back}
        {missing ? (
          <p className="text-stone-600">{t(kind === 'models' ? 'model.notFound' : `parties.${kind}.missing`)}</p>
        ) : (
          <LoadError error={history.error} onRetry={() => void history.refetch()} />
        )}
      </div>
    );
  }

  const saves = history.data.pages.flatMap((page) => page.saves);

  return (
    <div className="flex max-w-3xl flex-col gap-6">
      {back}
      <div className="flex flex-col gap-1">
        <h1 id={headingId} className="text-xl font-bold text-stone-900">
          {title ? t('history.titleOf', { name: title }) : t('history.title')}
        </h1>
        <p className="text-sm text-stone-500">{t('history.hint')}</p>
      </div>

      {saves.length === 0 ? (
        <p className="text-stone-600">{t('history.empty')}</p>
      ) : (
        <ol aria-labelledby={headingId} className="flex flex-col gap-4">
          {saves.map((save, index) => (
            <li key={`${save.at}-${String(index)}`}>
              <SaveCard save={save} />
            </li>
          ))}
        </ol>
      )}

      {history.hasNextPage && (
        <Button
          variant="secondary"
          className="self-center"
          busy={history.isFetchingNextPage}
          onClick={() => void history.fetchNextPage()}
        >
          {t('history.older')}
        </Button>
      )}
    </div>
  );
}

/**
 * The record's name for the heading, from the same request and cache as its own page, so coming
 * from that page shows it at once. Null until it arrives, or when the record is not found.
 */
function useRecordTitle(kind: HistoryKind, id: string): string | null {
  const record = useQuery<ProductDetails | PartyDetails, Error, string>({
    queryKey: kind === 'models' ? productKey(id) : partyKey(kind, id),
    queryFn: () => (kind === 'models' ? productsApi.get(id) : partiesApi[kind].get(id)),
    select: (found) =>
      'number' in found ? `${found.name} (${toPersianDigits(String(found.number))})` : found.fullName,
  });

  return record.data ?? null;
}

function SaveCard({ save }: { save: HistorySave }) {
  return (
    <Card>
      <p className="text-sm text-stone-500">
        {/* Isolated, so a name in Latin letters does not pull the date around it. */}
        <bdi className="font-medium text-stone-800">{save.by ?? t('history.system')}</bdi>
        {' · '}
        {t('history.when', { date: formatDate(save.at), time: formatTime(save.at) })}
      </p>
      <ul className="-mt-2 flex flex-col gap-2">
        {describeSave(save.changes).map((line, index) => (
          <li key={index}>
            <Line line={line} />
          </li>
        ))}
      </ul>
    </Card>
  );
}

function Line({ line }: { line: HistoryLine }) {
  const details = line.details && line.details.length > 0 && (
    <ul className="mt-1 flex list-disc flex-col gap-0.5 ps-5 text-sm text-stone-600">
      {line.details.map((detail, index) => (
        <li key={index}>{detail}</li>
      ))}
    </ul>
  );

  if (line.folded) {
    return (
      <details>
        <summary className="cursor-pointer text-stone-900">{line.text}</summary>
        {details}
      </details>
    );
  }

  return (
    <div className="flex flex-col gap-1">
      <p className="text-stone-900">{line.text}</p>
      {line.amount && <Money amount={line.amount as `${number}`} />}
      {details}
    </div>
  );
}
