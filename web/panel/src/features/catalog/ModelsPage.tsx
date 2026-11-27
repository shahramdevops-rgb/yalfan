import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { toPersianDigits } from '../../shared/format/digits';
import { t } from '../../shared/i18n/fa';
import { Card } from '../../shared/ui/Card';
import { PagedTable, type Column } from '../../shared/ui/DataTable';
import { PlusIcon } from '../../shared/ui/icons';
import { ButtonLink } from '../../shared/ui/Links';
import { useListAddress } from '../../shared/ui/listAddress';
import { SearchField } from '../../shared/ui/ListSearch';
import { PageHeader } from '../../shared/ui/PageHeader';
import { productSearchKey, productsApi, type ProductListItem } from './productsApi';

const digits = (value: number) => toPersianDigits(String(value));

/** A model's number, name, category and how many bags it makes; on a phone the last two go under the name. */
const columns: Column<ProductListItem>[] = [
  { header: t('list.columnNumber'), place: 'lead', cell: (item) => digits(item.number) },
  { header: t('list.columnName'), place: 'title', cell: (item) => item.name },
  { header: t('model.category'), place: 'detail', cell: (item) => item.category?.name },
  {
    header: t('model.bags'),
    place: 'detail',
    cell: (item) => t('models.bagCount', { count: digits(item.variantCount) }),
  },
];

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
        <PagedTable
          results={results}
          page={page}
          onGo={list.goToPage}
          onReplacePage={list.replacePage}
          pagesLabel={t('models.pages')}
          columns={columns}
          rowKey={(item) => item.id}
          rowLink={(item) => `/models/${item.id}`}
          inactive={(item) => !item.isActive}
          label={t('models.title')}
          emptyText={search ? t('models.notFound', { search }) : t('models.empty')}
        />
      </Card>
    </div>
  );
}
