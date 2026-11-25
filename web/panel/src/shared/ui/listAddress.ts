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

/** What the search box and the address were last seen as, and the search this list last sent. */
export interface SearchBox {
  /** The text in the box, as typed. */
  typed: string;
  /** The address's search when the box last looked at it. */
  followed: string;
  /** The search the box sent to the address and has not seen arrive yet, or null. */
  sent: string | null;
}

/**
 * The box after the address's search changed. Its own search arriving changes nothing: more may
 * have been typed while the address was updating, and that must not be erased. A change from
 * outside (the sidebar link to the same list, the back button) is followed, or the old text would
 * be sent again after the pause; unless the box already says it.
 */
export function followAddress(box: SearchBox, search: string): SearchBox {
  if (search === box.followed) {
    return box;
  }

  if (search === box.sent) {
    return { ...box, followed: search, sent: null };
  }

  return { typed: search === box.typed.trim() ? box.typed : search, followed: search, sent: null };
}

/** How many pages a list has; an empty list still has its one page. */
export function pageCount(totalCount: number, pageSize: number): number {
  return Math.max(1, Math.ceil(totalCount / pageSize));
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
  const [box, setBox] = useState<SearchBox>({ typed: search, followed: search, sent: null });
  const typed = box.typed;

  // The address can change without the box (see followAddress); adjusted while drawing, so the
  // box never shows the old text for a frame.
  const followedBox = followAddress(box, search);
  if (followedBox !== box) {
    setBox(followedBox);
  }

  useEffect(() => {
    if (typed.trim() === search) {
      return;
    }

    const timer = setTimeout(() => {
      setBox((before) => ({ ...before, sent: typed.trim() }));
      setParams(writeListAddress({ search: typed.trim(), page: 1, withInactive }), { replace: true });
    }, typingPause);
    return () => clearTimeout(timer);
  }, [typed, search, withInactive, setParams]);

  return {
    search,
    page,
    withInactive,
    typed,
    setTyped: (text: string) => setBox((before) => ({ ...before, typed: text })),
    /** Another page: on a phone it must start from its top, not where the finger left the last one. */
    goToPage: (next: number) => {
      setParams(writeListAddress({ search, page: next, withInactive }));
      window.scrollTo({ top: 0 });
    },
    showInactive: (show: boolean) =>
      setParams(writeListAddress({ search, page: 1, withInactive: show }), { replace: true }),
    /** Another page in place of this one, with no step for the back button. */
    replacePage: (next: number) => setParams(writeListAddress({ search, page: next, withInactive }), { replace: true }),
  };
}

/**
 * Whether the address asks for a page past the last one, which happens when the list shrank (a
 * person deactivated from the last page, then the back button) or with an old link. The last page
 * is then shown in its place, instead of an empty page that says the list has nobody.
 */
export function usePageWithin(page: number, pages: number | undefined, replacePage: (page: number) => void): boolean {
  useEffect(() => {
    if (pages !== undefined && page > pages) {
      replacePage(pages);
    }
  }, [page, pages, replacePage]);

  return pages !== undefined && page > pages;
}
