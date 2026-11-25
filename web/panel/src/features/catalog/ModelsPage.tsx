import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { Link } from 'react-router';
import { toPersianDigits } from '../../shared/format/digits';
import { t } from '../../shared/i18n/fa';
import { PlusIcon } from '../../shared/ui/icons';
import { useListAddress } from '../../shared/ui/listAddress';
import { Pager, SearchField } from '../../shared/ui/ListSearch';
import { ListSkeleton, LoadError } from '../../shared/ui/QueryStates';
import { productSearchKey, productsApi, type ProductListItem } from './productsApi';

const digits = (value: number) => toPersianDigits(String(value));

/**
 * The models, newest first, twenty at a time, found by number or part of the name: the workshop
 * has many models (answered 1405/09/01). The search and the page are in the address, so a reload
 * or the back button keeps them. An inactive model stays in its place, greyed (ADR 0022).
 */
export function ModelsPage() {
  const list = useListAddress();
  const { search, page } = list;

  const results = useQuery({
    queryKey: productSearchKey(search, page),
    queryFn: () => productsApi.search(search, page),
    placeholderData: keepPreviousData,
  });

  const data = results.data;

  return (
    <div className="flex max-w-3xl flex-col gap-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-bold">{t('models.title')}</h1>
        <Link
          to="/models/new"
          className="inline-flex h-12 items-center gap-2 rounded-lg bg-brand-700 px-5 font-medium text-white hover:bg-brand-800 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-700"
        >
          <PlusIcon />
          {t('models.new')}
        </Link>
      </div>

      <section className="flex flex-col gap-4 rounded-2xl bg-white p-4 shadow-sm ring-1 ring-stone-200 sm:p-6">
        <SearchField label={t('models.search')} value={list.typed} onChange={list.setTyped} />

        {results.isPending && <ListSkeleton />}
        {results.isError && <LoadError error={results.error} onRetry={() => void results.refetch()} />}
        {data && data.items.length === 0 && (
          <p className="py-6 text-center text-stone-500">
            {search ? t('models.notFound', { search }) : t('models.empty')}
          </p>
        )}
        {data && data.items.length > 0 && (
          <ul className={`divide-y divide-stone-100 ${results.isPlaceholderData ? 'opacity-60' : ''}`}>
            {data.items.map((item) => (
              <ModelRow key={item.id} item={item} />
            ))}
          </ul>
        )}

        {data && (
          <Pager
            page={page}
            totalCount={data.totalCount}
            pageSize={data.pageSize}
            label={t('models.pages')}
            onGo={list.goToPage}
          />
        )}
      </section>
    </div>
  );
}

function ModelRow({ item }: { item: ProductListItem }) {
  const details = [item.category?.name, t('models.bagCount', { count: digits(item.variantCount) })].filter(Boolean);

  return (
    <li>
      <Link
        to={`/models/${item.id}`}
        className="-mx-2 flex items-center gap-4 rounded-lg px-2 py-3 hover:bg-stone-50 focus-visible:outline-2 focus-visible:outline-brand-700"
      >
        <span className={`w-14 shrink-0 text-lg font-bold ${item.isActive ? 'text-brand-800' : 'text-stone-400'}`}>
          {digits(item.number)}
        </span>
        <span className="flex min-w-0 flex-1 flex-col">
          <span className={`truncate text-base ${item.isActive ? 'text-stone-900' : 'text-stone-400'}`}>
            {item.name}
          </span>
          <span className="truncate text-sm text-stone-500">{details.join(t('models.separator'))}</span>
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
