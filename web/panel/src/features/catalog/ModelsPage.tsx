import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useEffect, useId, useState } from 'react';
import { Link, useSearchParams } from 'react-router';
import { toPersianDigits } from '../../shared/format/digits';
import { t } from '../../shared/i18n/fa';
import { Button } from '../../shared/ui/Button';
import { PlusIcon, SearchIcon } from '../../shared/ui/icons';
import { ListSkeleton, LoadError } from '../../shared/ui/QueryStates';
import { productSearchKey, productsApi, type ProductListItem } from './productsApi';

/** How long typing must pause before the search is sent, so a slow line is not sent every letter. */
const typingPause = 300;

const digits = (value: number) => toPersianDigits(String(value));

/**
 * The models, newest first, twenty at a time, found by number or part of the name: the workshop
 * has many models (answered 1405/09/01). The search and the page are in the address, so a reload
 * or the back button keeps them. An inactive model stays in its place, greyed (ADR 0022).
 */
export function ModelsPage() {
  const searchId = useId();
  const [params, setParams] = useSearchParams();
  const search = params.get('q') ?? '';
  const page = Math.max(1, Math.floor(Number(params.get('page'))) || 1);
  const [typed, setTyped] = useState(search);

  useEffect(() => {
    if (typed.trim() === search) {
      return;
    }

    const timer = setTimeout(() => setParams(typed.trim() ? { q: typed.trim() } : {}, { replace: true }), typingPause);
    return () => clearTimeout(timer);
  }, [typed, search, setParams]);

  const results = useQuery({
    queryKey: productSearchKey(search, page),
    queryFn: () => productsApi.search(search, page),
    placeholderData: keepPreviousData,
  });

  // The pager sits under twenty rows; on a phone the next page must start from its top, not where
  // the finger left the last one.
  const goTo = (next: number) => {
    setParams(search ? { q: search, page: String(next) } : { page: String(next) });
    window.scrollTo({ top: 0 });
  };
  const data = results.data;
  const pages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1;

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
        <div className="flex flex-col gap-1.5">
          <label htmlFor={searchId} className="text-sm font-medium text-stone-700">
            {t('models.search')}
          </label>
          <div className="relative sm:max-w-sm">
            <SearchIcon className="pointer-events-none absolute start-3 top-3.5 size-5 text-stone-400" />
            <input
              id={searchId}
              type="search"
              value={typed}
              onChange={(event) => setTyped(event.target.value)}
              maxLength={100}
              autoComplete="off"
              className="h-12 w-full rounded-lg border border-stone-300 bg-white ps-10 pe-3 text-base outline-none focus:border-brand-600 focus:ring-2 focus:ring-brand-100"
            />
          </div>
        </div>

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

        {data && pages > 1 && (
          <nav
            className="flex items-center justify-between gap-3 border-t border-stone-100 pt-4"
            aria-label={t('models.pages')}
          >
            <Button variant="secondary" disabled={page <= 1} onClick={() => goTo(page - 1)}>
              {t('models.previous')}
            </Button>
            <span className="text-sm text-stone-600">
              {t('models.pageOf', { page: digits(page), pages: digits(pages) })}
            </span>
            <Button variant="secondary" disabled={page >= pages} onClick={() => goTo(page + 1)}>
              {t('models.next')}
            </Button>
          </nav>
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
