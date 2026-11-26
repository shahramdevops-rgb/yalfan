import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Navigate, NavLink, useParams } from 'react-router';
import { upsert } from '../../shared/api/listCache';
import { attempt } from '../../shared/api/refusal';
import { byPersianName } from '../../shared/format/persianOrder';
import { t } from '../../shared/i18n/fa';
import { AddNameForm } from '../../shared/ui/AddNameForm';
import { Card } from '../../shared/ui/Card';
import { NameList } from '../../shared/ui/NameList';
import { PageHeader } from '../../shared/ui/PageHeader';
import { ListSkeleton, LoadError } from '../../shared/ui/QueryStates';
import {
  catalogKinds,
  catalogListKey,
  catalogLists,
  isCatalogKind,
  type CatalogKind,
  type CatalogListItem,
} from './catalogListsApi';

/**
 * Fabrics, colours and categories on one page with a tab each, because the three work exactly
 * alike and the sidebar stays short for the operator (decided 1405/09/03). The tab is in the
 * address, so a reload or a bookmark opens the same list.
 */
export function CatalogListsPage() {
  const { kind } = useParams();
  if (!isCatalogKind(kind)) {
    return <Navigate to={`/lists/${catalogKinds[0]}`} replace />;
  }

  return (
    <div className="flex max-w-3xl flex-col gap-6">
      <PageHeader title={t('lists.title')} hint={t('lists.hint')} />

      <nav className="flex gap-1 border-b border-line">
        {catalogKinds.map((tab) => (
          <NavLink
            key={tab}
            to={`/lists/${tab}`}
            className={({ isActive }) =>
              `-mb-px border-b-2 px-4 py-2.5 text-base transition-colors ${
                isActive ? 'border-brand font-medium text-brand-strong' : 'border-transparent text-body hover:text-ink'
              }`
            }
          >
            {t(`lists.tab.${tab}`)}
          </NavLink>
        ))}
      </nav>

      <Card>
        <CatalogList key={kind} kind={kind} />
      </Card>
    </div>
  );
}

function CatalogList({ kind }: { kind: CatalogKind }) {
  const queryClient = useQueryClient();
  const key = catalogListKey(kind);
  const endpoints = catalogLists[kind];
  const list = useQuery({ queryKey: key, queryFn: endpoints.list });

  const put = (item: CatalogListItem) =>
    queryClient.setQueryData<CatalogListItem[]>(key, (items) => upsert(items, item));

  return (
    <div className="flex flex-col gap-6">
      <AddNameForm
        label={t(`lists.add.${kind}`)}
        onAdd={(name) => attempt(() => endpoints.create(name), put, 'name')}
      />

      {list.isPending && <ListSkeleton />}
      {list.isError && <LoadError error={list.error} onRetry={() => void list.refetch()} />}
      {list.data && (
        <NameList
          items={byPersianName(list.data)}
          onRename={(id, name) => attempt(() => endpoints.rename(id, name), put, 'name')}
          onSetActive={(id, isActive) => attempt(() => endpoints.setActive(id, isActive), put)}
          emptyText={t(`lists.empty.${kind}`)}
        />
      )}
    </div>
  );
}
