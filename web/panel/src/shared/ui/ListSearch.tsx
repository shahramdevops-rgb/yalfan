import { useId } from 'react';
import { toPersianDigits } from '../format/digits';
import { t } from '../i18n/fa';
import { Button } from './Button';
import { SearchIcon } from './icons';

const digits = (value: number) => toPersianDigits(String(value));

/** The one search box of a list (models, contractors, workers), with what it searches by as its label. */
export function SearchField({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string;
  onChange: (text: string) => void;
}) {
  const id = useId();

  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium text-stone-700">
        {label}
      </label>
      <div className="relative sm:max-w-sm">
        <SearchIcon className="pointer-events-none absolute start-3 top-3.5 size-5 text-stone-400" />
        <input
          id={id}
          type="search"
          value={value}
          onChange={(event) => onChange(event.target.value)}
          maxLength={100}
          autoComplete="off"
          className="h-12 w-full rounded-lg border border-stone-300 bg-white ps-10 pe-3 text-base outline-none focus:border-brand-600 focus:ring-2 focus:ring-brand-100"
        />
      </div>
    </div>
  );
}

/** The previous and next page under a list, and where the reader is; nothing when there is one page. */
export function Pager({
  page,
  totalCount,
  pageSize,
  label,
  onGo,
}: {
  page: number;
  totalCount: number;
  pageSize: number;
  /** What the pages are of, for screen readers: «صفحه‌های فهرست مدل‌ها». */
  label: string;
  onGo: (page: number) => void;
}) {
  const pages = Math.max(1, Math.ceil(totalCount / pageSize));
  if (pages <= 1) {
    return null;
  }

  return (
    <nav className="flex items-center justify-between gap-3 border-t border-stone-100 pt-4" aria-label={label}>
      <Button variant="secondary" disabled={page <= 1} onClick={() => onGo(page - 1)}>
        {t('pager.previous')}
      </Button>
      <span className="text-sm text-stone-600">{t('pager.pageOf', { page: digits(page), pages: digits(pages) })}</span>
      <Button variant="secondary" disabled={page >= pages} onClick={() => onGo(page + 1)}>
        {t('pager.next')}
      </Button>
    </nav>
  );
}
