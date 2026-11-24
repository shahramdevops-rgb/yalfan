import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Navigate, NavLink, useParams } from 'react-router';
import { upsert } from '../../shared/api/listCache';
import { attempt } from '../../shared/api/refusal';
import { byPersianName } from '../../shared/format/persianOrder';
import { t } from '../../shared/i18n/fa';
import { AddNameForm } from '../../shared/ui/AddNameForm';
import { NameList } from '../../shared/ui/NameList';
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
      <div>
        <h1 className="text-2xl font-bold">{t('lists.title')}</h1>
        <p className="mt-1 text-stone-500">{t('lists.hint')}</p>
      </div>

      <nav className="flex gap-1 border-b border-stone-200">
        {catalogKinds.map((tab) => (
          <NavLink
            key={tab}
            to={`/lists/${tab}`}
            className={({ isActive }) =>
              `-mb-px border-b-2 px-4 py-2.5 text-base transition-colors ${
                isActive
                  ? 'border-brand-700 font-medium text-brand-800'
                  : 'border-transparent text-stone-600 hover:text-stone-900'
              }`
            }
          >
            {t(`lists.tab.${tab}`)}
          </NavLink>
        ))}
      </nav>

      <section className="rounded-2xl bg-white p-4 shadow-sm ring-1 ring-stone-200 sm:p-6">
        <CatalogList key={kind} kind={kind} />
      </section>
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
