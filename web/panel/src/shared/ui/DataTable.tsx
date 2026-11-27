import type { UseQueryResult } from '@tanstack/react-query';
import { Fragment, type MouseEvent, type ReactNode } from 'react';
import { Link, useNavigate } from 'react-router';
import { t } from '../i18n/fa';
import { pageCount, usePageWithin } from './listAddress';
import { Pager } from './ListSearch';
import { ListSkeleton, LoadError } from './QueryStates';
import { StatusBadge } from './StatusBadge';

/**
 * Where a column's cell goes on a phone's card, which also says how it reads in the table: `lead`
 * before the name (a model's number, bold in the workshop's colour), `title` the name, which
 * opens the row's page, `detail` the line under the name, the details joined with « · », and
 * `end` at the far end (a mobile number).
 */
export type Place = 'lead' | 'title' | 'detail' | 'end';

export interface Column<Row> {
  /** The column's heading in the table. */
  header: string;
  /** Nothing (null, undefined, '') leaves the cell empty and the detail out of the card's line. */
  cell: (row: Row) => ReactNode;
  /** Exactly one column is the `title`; any other number stops the page (the crash page shows). */
  place: Place;
}

export interface DataTableProps<Row> {
  rows: readonly Row[];
  columns: readonly Column<Row>[];
  rowKey: (row: Row) => string;
  /** The row's own page: the whole row opens it. */
  rowLink?: (row: Row) => string;
  /** An inactive row stays where it is, greyed and marked «غیرفعال» (ADR 0022). */
  inactive?: (row: Row) => boolean;
  /** What the rows are, for screen readers: the page's title. */
  label: string;
  /** The sentence in place of the rows when there are none. */
  emptyText: string;
  /** Rows kept from the last page while the next one loads. */
  dimmed?: boolean;
}

const never = () => false;

const empty = (node: ReactNode) => node === null || node === undefined || node === false || node === '';

/**
 * The rows of a list with several things to say about each (ADR 0032). From a tablet's width up a
 * real table, whose header stays at the top of the window while the rows scroll under it; on a
 * phone, where columns do not fit, each row is a card: the name, the details on the line under it,
 * and the rest at the end. Both are drawn and the width shows one, rather than one table turned
 * into cards by CSS, which Safari then no longer reads as a table.
 *
 * The whole row opens its page, but only the name is a link: one Tab per row, and what a screen
 * reader says is the name, not every cell.
 */
export function DataTable<Row>({
  rows,
  columns,
  rowKey,
  rowLink,
  inactive = never,
  label,
  emptyText,
  dimmed = false,
}: DataTableProps<Row>) {
  // A list with no name, or two, would leave its rows with no link, or two: no way in by the
  // keyboard, or two stops per row. A mistake of the page's code, so it stops the page: the crash
  // page shows in its place, and the page's screenshot test fails.
  const titles = columns.filter((column) => column.place === 'title').length;
  if (titles !== 1) {
    throw new Error(`A DataTable needs exactly one title column; it has ${String(titles)}.`);
  }

  if (rows.length === 0) {
    return <p className="py-6 text-center text-muted">{emptyText}</p>;
  }

  const dim = dimmed ? 'opacity-60' : '';

  return (
    <>
      <table
        aria-label={label}
        aria-busy={dimmed}
        className={`-mx-2 hidden w-[calc(100%+1rem)] border-separate border-spacing-0 sm:table ${dim}`}
      >
        <thead>
          <tr>
            {columns.map((column, index) => (
              <th
                key={index}
                scope="col"
                className="sticky top-0 z-10 border-b border-line bg-surface px-2 py-2 text-start text-sm font-normal text-muted"
              >
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <TableRow key={rowKey(row)} row={row} columns={columns} link={rowLink?.(row)} inactive={inactive(row)} />
          ))}
        </tbody>
      </table>

      {/* Named a list again: Safari stops reading a list as one when its bullets are taken away. */}
      <ul role="list" aria-label={label} aria-busy={dimmed} className={`divide-y divide-line-soft sm:hidden ${dim}`}>
        {rows.map((row) => (
          <li key={rowKey(row)}>
            <RowCard row={row} columns={columns} link={rowLink?.(row)} inactive={inactive(row)} />
          </li>
        ))}
      </ul>
    </>
  );
}

interface RowProps<Row> {
  row: Row;
  columns: readonly Column<Row>[];
  link: string | undefined;
  inactive: boolean;
}

/** How a cell reads in the table, by its place; an inactive row is greyed through. */
function cellTone(place: Place, inactive: boolean): string {
  switch (place) {
    case 'lead':
      return `text-lg font-bold ${inactive ? 'text-faint' : 'text-brand-strong'}`;
    case 'title':
      return `text-base ${inactive ? 'text-faint' : 'text-ink'}`;
    case 'detail':
      return `text-sm ${inactive ? 'text-faint' : 'text-body'}`;
    case 'end':
      return `text-sm whitespace-nowrap ${inactive ? 'text-faint' : 'text-body'}`;
  }
}

/** A modified click (a new tab, a selection) or one on a control of the row is not the row's to answer. */
function isPlainClick(event: MouseEvent<HTMLElement>): boolean {
  return (
    !event.defaultPrevented &&
    event.button === 0 &&
    !(event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) &&
    !(event.target as Element).closest('a, button, input, select, textarea, label')
  );
}

function TableRow<Row>({ row, columns, link, inactive }: RowProps<Row>) {
  const navigate = useNavigate();

  // A click anywhere on the row opens it, as the name's link does; the link itself answers its own
  // click (or the page would be added twice to the back button's steps), and a click that ends a
  // selection of text in the row leaves the selection alone.
  const open =
    link === undefined
      ? undefined
      : (event: MouseEvent<HTMLTableRowElement>) => {
          const selection = window.getSelection();
          const selecting =
            selection !== null && !selection.isCollapsed && event.currentTarget.contains(selection.anchorNode);
          if (isPlainClick(event) && !selecting) {
            void navigate(link);
          }
        };

  const pointer = link === undefined ? '' : 'group-hover:bg-subtle group-has-[a:focus-visible]:bg-subtle';

  return (
    <tr className={`group ${link === undefined ? '' : 'cursor-pointer'}`} onClick={open}>
      {columns.map((column, index) => (
        <td
          key={index}
          className={`border-b border-line-soft px-2 py-3 group-last:border-b-0 ${pointer} ${cellTone(column.place, inactive)}`}
        >
          {column.place === 'title' ? (
            <span className="flex items-center gap-2">
              {link === undefined ? (
                column.cell(row)
              ) : (
                <Link
                  to={link}
                  className="rounded focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand"
                >
                  {column.cell(row)}
                </Link>
              )}
              {inactive && <StatusBadge>{t('list.inactive')}</StatusBadge>}
            </span>
          ) : (
            column.cell(row)
          )}
        </td>
      ))}
    </tr>
  );
}

/** A row on a phone: the lead, the name with its details under it, the end, and the inactive mark. */
function RowCard<Row>({ row, columns, link, inactive }: RowProps<Row>) {
  const of = (place: Place) => columns.filter((column) => column.place === place).map((column) => column.cell(row));
  const details = of('detail').filter((node) => !empty(node));

  const body = (
    <>
      {of('lead').map((node, index) => (
        <span
          key={index}
          className={`w-15 shrink-0 text-lg font-bold ${inactive ? 'text-faint' : 'text-brand-strong'}`}
        >
          {node}
        </span>
      ))}
      <span className="flex min-w-0 flex-1 flex-col">
        {of('title').map((node, index) => (
          <span key={index} className={`truncate text-base ${inactive ? 'text-faint' : 'text-ink'}`}>
            {node}
          </span>
        ))}
        {details.length > 0 && (
          <span className="truncate text-sm text-muted">
            {details.map((node, index) => (
              <Fragment key={index}>
                {index > 0 && t('list.separator')}
                {node}
              </Fragment>
            ))}
          </span>
        )}
      </span>
      {of('end').map((node, index) => (
        <span key={index} className={`shrink-0 text-sm ${inactive ? 'text-faint' : 'text-body'}`}>
          {node}
        </span>
      ))}
      {inactive && <StatusBadge>{t('list.inactive')}</StatusBadge>}
    </>
  );

  return link === undefined ? (
    <div className="flex items-center gap-3 py-3">{body}</div>
  ) : (
    <Link
      to={link}
      className="-mx-2 flex items-center gap-3 rounded-lg px-2 py-3 hover:bg-subtle focus-visible:outline-2 focus-visible:outline-brand"
    >
      {body}
    </Link>
  );
}

/** A page of a list as the API answers it. */
interface Paged<Row> {
  items: Row[];
  totalCount: number;
  pageSize: number;
}

interface PagedTableProps<Row> extends Omit<DataTableProps<Row>, 'rows' | 'dimmed'> {
  results: UseQueryResult<Paged<Row>>;
  /** The page the address asks for. */
  page: number;
  onGo: (page: number) => void;
  /** Another page in place of this one, with no step for the back button. */
  onReplacePage: (page: number) => void;
  /** What the pages are of, for screen readers: «صفحه‌های فهرست مدل‌ها». */
  pagesLabel: string;
}

/**
 * A searched list's page in a `DataTable`, with what the list says while it loads, when it did
 * not load, and when it is empty, and the pager under it. The last page's rows stay, dimmed,
 * while the next page loads. A page past the last one (the list shrank, or an old link) is
 * replaced by the last page, and until then it is only loading.
 */
export function PagedTable<Row>({ results, page, onGo, onReplacePage, pagesLabel, ...table }: PagedTableProps<Row>) {
  const data = results.data;
  // Data kept from the last search while this one loads belongs to another address: no page is
  // judged out of range by it.
  const pages = data && !results.isPlaceholderData ? pageCount(data.totalCount, data.pageSize) : undefined;
  const outOfRange = usePageWithin(page, pages, onReplacePage);

  return (
    <>
      {(results.isPending || outOfRange) && <ListSkeleton />}
      {results.isError && <LoadError error={results.error} onRetry={() => void results.refetch()} />}
      {data && !outOfRange && (
        <>
          <DataTable rows={data.items} dimmed={results.isPlaceholderData} {...table} />
          <Pager page={page} totalCount={data.totalCount} pageSize={data.pageSize} label={pagesLabel} onGo={onGo} />
        </>
      )}
    </>
  );
}
