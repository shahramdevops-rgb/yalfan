import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { Link } from 'react-router';
import { formatMobile } from '../../shared/format/mobile';
import { t } from '../../shared/i18n/fa';
import { PlusIcon } from '../../shared/ui/icons';
import { useListAddress } from '../../shared/ui/listAddress';
import { Pager, SearchField } from '../../shared/ui/ListSearch';
import { ListSkeleton, LoadError } from '../../shared/ui/QueryStates';
import { Switch } from '../../shared/ui/Switch';
import { partiesApi, partySearchKey, type PartyKind, type PartyListItem } from './partiesApi';

/**
 * The contractors or the in-house workers, in Persian alphabetical order, twenty at a time, found
 * by part of the name or of the mobile. Only the active ones unless the switch asks for the
 * inactive ones too, because someone who left stays in the list for good and the list would fill
 * with them over the years (ADR 0025). The search, the page and the switch are in the address.
 */
export function PartyListPage({ kind }: { kind: PartyKind }) {
  const list = useListAddress();
  const { search, page, withInactive } = list;

  const results = useQuery({
    queryKey: partySearchKey(kind, search, withInactive, page),
    queryFn: () => partiesApi[kind].search(search, withInactive, page),
    placeholderData: keepPreviousData,
  });

  const data = results.data;
  const emptyText = search
    ? t(`parties.${kind}.notFound`, { search })
    : withInactive
      ? t(`parties.${kind}.empty`)
      : t(`parties.${kind}.noActive`);

  return (
    <div className="flex max-w-3xl flex-col gap-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-bold">{t(`parties.${kind}.title`)}</h1>
        <Link
          to={`/${kind}/new`}
          className="inline-flex h-12 items-center gap-2 rounded-lg bg-brand-700 px-5 font-medium text-white hover:bg-brand-800 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-700"
        >
          <PlusIcon />
          {t(`parties.${kind}.new`)}
        </Link>
      </div>

      <section className="flex flex-col gap-4 rounded-2xl bg-white p-4 shadow-sm ring-1 ring-stone-200 sm:p-6">
        <SearchField label={t('parties.search')} value={list.typed} onChange={list.setTyped} />
        <div className="flex items-center gap-3">
          <Switch checked={withInactive} onChange={list.showInactive} label={t('parties.withInactive')} />
          <span className="text-sm text-stone-700" aria-hidden>
            {t('parties.withInactive')}
          </span>
        </div>

        {results.isPending && <ListSkeleton />}
        {results.isError && <LoadError error={results.error} onRetry={() => void results.refetch()} />}
        {data && data.items.length === 0 && <p className="py-6 text-center text-stone-500">{emptyText}</p>}
        {data && data.items.length > 0 && (
          <ul className={`divide-y divide-stone-100 ${results.isPlaceholderData ? 'opacity-60' : ''}`}>
            {data.items.map((item) => (
              <PartyRow key={item.id} kind={kind} item={item} />
            ))}
          </ul>
        )}

        {data && (
          <Pager
            page={page}
            totalCount={data.totalCount}
            pageSize={data.pageSize}
            label={t(`parties.${kind}.pages`)}
            onGo={list.goToPage}
          />
        )}
      </section>
    </div>
  );
}

function PartyRow({ kind, item }: { kind: PartyKind; item: PartyListItem }) {
  const stages = item.stages.map((stage) => stage.name).join(t('models.separator')) || t('parties.noStages');

  return (
    <li>
      <Link
        to={`/${kind}/${item.id}`}
        className="-mx-2 flex items-center gap-3 rounded-lg px-2 py-3 hover:bg-stone-50 focus-visible:outline-2 focus-visible:outline-brand-700"
      >
        <span className="flex min-w-0 flex-1 flex-col">
          <span className={`truncate text-base ${item.isActive ? 'text-stone-900' : 'text-stone-400'}`}>
            {item.fullName}
          </span>
          <span className="truncate text-sm text-stone-500">{stages}</span>
        </span>
        <span dir="ltr" className={`shrink-0 text-sm ${item.isActive ? 'text-stone-600' : 'text-stone-400'}`}>
          {formatMobile(item.mobileNumber)}
        </span>
        {!item.isActive && (
          <span className="shrink-0 rounded-full bg-stone-100 px-2 py-0.5 text-xs text-stone-500">
            {t('list.inactive')}
          </span>
        )}
      </Link>
    </li>
  );
}
