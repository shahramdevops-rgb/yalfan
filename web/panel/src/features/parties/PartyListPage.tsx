import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { formatMobile } from '../../shared/format/mobile';
import { t } from '../../shared/i18n/fa';
import { Card } from '../../shared/ui/Card';
import { PagedTable, type Column } from '../../shared/ui/DataTable';
import { PlusIcon } from '../../shared/ui/icons';
import { ButtonLink } from '../../shared/ui/Links';
import { useListAddress } from '../../shared/ui/listAddress';
import { SearchField } from '../../shared/ui/ListSearch';
import { PageHeader } from '../../shared/ui/PageHeader';
import { Switch } from '../../shared/ui/Switch';
import { partiesApi, partySearchKey, type PartyKind, type PartyListItem } from './partiesApi';

/** A person's name, stages and mobile; on a phone the stages go under the name and the mobile at the end. */
const columns: Column<PartyListItem>[] = [
  { header: t('list.columnName'), place: 'title', cell: (item) => item.fullName },
  {
    header: t('parties.stages'),
    place: 'detail',
    cell: (item) => item.stages.map((stage) => stage.name).join(t('list.separator')) || t('parties.noStages'),
  },
  {
    header: t('parties.mobile'),
    place: 'end',
    // A mobile number reads left to right, as it is dialled.
    cell: (item) => <span dir="ltr">{formatMobile(item.mobileNumber)}</span>,
  },
];

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

  const emptyText = search
    ? t(`parties.${kind}.notFound`, { search })
    : withInactive
      ? t(`parties.${kind}.empty`)
      : t(`parties.${kind}.noActive`);

  return (
    <div className="flex max-w-3xl flex-col gap-6">
      <PageHeader
        title={t(`parties.${kind}.title`)}
        actions={
          <ButtonLink to={`/${kind}/new`}>
            <PlusIcon />
            {t(`parties.${kind}.new`)}
          </ButtonLink>
        }
      />

      <Card gap={4}>
        <SearchField label={t('parties.search')} value={list.typed} onChange={list.setTyped} />
        <div className="flex items-center gap-3">
          <Switch checked={withInactive} onChange={list.showInactive} label={t('parties.withInactive')} />
          <span className="text-sm text-label" aria-hidden>
            {t('parties.withInactive')}
          </span>
        </div>

        <PagedTable
          results={results}
          page={page}
          onGo={list.goToPage}
          onReplacePage={list.replacePage}
          pagesLabel={t(`parties.${kind}.pages`)}
          columns={columns}
          rowKey={(item) => item.id}
          rowLink={(item) => `/${kind}/${item.id}`}
          inactive={(item) => !item.isActive}
          label={t(`parties.${kind}.title`)}
          emptyText={emptyText}
        />
      </Card>
    </div>
  );
}
