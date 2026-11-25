import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router';

/** How long typing must pause before the search is sent, so a slow line is not sent every letter. */
const typingPause = 300;

/**
 * What a searched list shows, kept in the page's address so a reload or the back button keeps
 * it: `?q=…&page=2&inactive=1`.
 */
export interface ListAddress {
  search: string;
  page: number;
  /** The inactive rows are shown too (contractors and workers, ADR 0025). */
  withInactive: boolean;
}

/** The list's state from the address; anything missing or odd falls back to the first page of everything active. */
export function readListAddress(params: URLSearchParams): ListAddress {
  return {
    search: params.get('q')?.trim() ?? '',
    page: Math.max(1, Math.floor(Number(params.get('page'))) || 1),
    withInactive: params.get('inactive') === '1',
  };
}

/** The address of a list's state, leaving out what is the default, so a plain list has a plain address. */
export function writeListAddress({ search, page, withInactive }: ListAddress): Record<string, string> {
  return {
    ...(search && { q: search }),
    ...(page > 1 && { page: String(page) }),
    ...(withInactive && { inactive: '1' }),
  };
}

/**
 * A searched, paged list's state in the address, and the search box's text. The search is sent
 * when typing pauses and starts again from the first page; it and the inactive switch replace the
 * address, so the back button leaves the list instead of undoing letters, while a page change
 * adds a step, so the back button returns to the page before.
 */
export function useListAddress() {
  const [params, setParams] = useSearchParams();
  const { search, page, withInactive } = readListAddress(params);
  const [typed, setTyped] = useState(search);

  useEffect(() => {
    if (typed.trim() === search) {
      return;
    }

    const timer = setTimeout(
      () => setParams(writeListAddress({ search: typed.trim(), page: 1, withInactive }), { replace: true }),
      typingPause,
    );
    return () => clearTimeout(timer);
  }, [typed, search, withInactive, setParams]);

  return {
    search,
    page,
    withInactive,
    typed,
    setTyped,
    /** Another page: on a phone it must start from its top, not where the finger left the last one. */
    goToPage: (next: number) => {
      setParams(writeListAddress({ search, page: next, withInactive }));
      window.scrollTo({ top: 0 });
    },
    showInactive: (show: boolean) =>
      setParams(writeListAddress({ search, page: 1, withInactive: show }), { replace: true }),
  };
}
