import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { Link } from 'react-router';
import { toPersianDigits } from '../../shared/format/digits';
import { t } from '../../shared/i18n/fa';
import { Card } from '../../shared/ui/Card';
import { PlusIcon } from '../../shared/ui/icons';
import { ButtonLink } from '../../shared/ui/Links';
import { pageCount, useListAddress, usePageWithin } from '../../shared/ui/listAddress';
import { Pager, SearchField } from '../../shared/ui/ListSearch';
import { PageHeader } from '../../shared/ui/PageHeader';
import { ListSkeleton, LoadError } from '../../shared/ui/QueryStates';
import { StatusBadge } from '../../shared/ui/StatusBadge';
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
  // Data kept from the last search while this one loads belongs to another address: no page is
  // judged out of range by it.
  const pages = data && !results.isPlaceholderData ? pageCount(data.totalCount, data.pageSize) : undefined;
  const outOfRange = usePageWithin(page, pages, list.replacePage);

  return (
    <div className="flex max-w-3xl flex-col gap-6">
      <PageHeader
        title={t('models.title')}
        actions={
          <ButtonLink to="/models/new">
            <PlusIcon />
            {t('models.new')}
          </ButtonLink>
        }
      />

      <Card gap={4}>
        <SearchField label={t('models.search')} value={list.typed} onChange={list.setTyped} />

        {/* A page past the last one is replaced by the last page; until then it is only loading. */}
        {(results.isPending || outOfRange) && <ListSkeleton />}
        {results.isError && <LoadError error={results.error} onRetry={() => void results.refetch()} />}
        {data && !outOfRange && data.items.length === 0 && (
          <p className="py-6 text-center text-muted">{search ? t('models.notFound', { search }) : t('models.empty')}</p>
        )}
        {data && !outOfRange && data.items.length > 0 && (
          <ul className={`divide-y divide-line-soft ${results.isPlaceholderData ? 'opacity-60' : ''}`}>
            {data.items.map((item) => (
              <ModelRow key={item.id} item={item} />
            ))}
          </ul>
        )}

        {data && !outOfRange && (
          <Pager
            page={page}
            totalCount={data.totalCount}
            pageSize={data.pageSize}
            label={t('models.pages')}
            onGo={list.goToPage}
          />
        )}
      </Card>
    </div>
  );
}

function ModelRow({ item }: { item: ProductListItem }) {
  const details = [item.category?.name, t('models.bagCount', { count: digits(item.variantCount) })].filter(Boolean);

  return (
    <li>
      <Link
        to={`/models/${item.id}`}
        className="-mx-2 flex items-center gap-4 rounded-lg px-2 py-3 hover:bg-subtle focus-visible:outline-2 focus-visible:outline-brand"
      >
        <span className={`w-14 shrink-0 text-lg font-bold ${item.isActive ? 'text-brand-strong' : 'text-faint'}`}>
          {digits(item.number)}
        </span>
        <span className="flex min-w-0 flex-1 flex-col">
          <span className={`truncate text-base ${item.isActive ? 'text-ink' : 'text-faint'}`}>{item.name}</span>
          <span className="truncate text-sm text-muted">{details.join(t('models.separator'))}</span>
        </span>
        {!item.isActive && <StatusBadge>{t('list.inactive')}</StatusBadge>}
      </Link>
    </li>
  );
}
