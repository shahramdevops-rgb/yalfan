import { describe, expect, it } from 'vitest';
import { followAddress, pageCount, readListAddress, writeListAddress, type SearchBox } from './listAddress';

describe('readListAddress', () => {
  it('reads the search, the page and the inactive switch', () => {
    expect(readListAddress(new URLSearchParams('q=%D8%AD%D8%B3%D9%86&page=2&inactive=1'))).toEqual({
      search: 'حسن',
      page: 2,
      withInactive: true,
    });
  });

  it('falls back to the first page of everything active for anything missing or odd', () => {
    expect(readListAddress(new URLSearchParams(''))).toEqual({ search: '', page: 1, withInactive: false });
    expect(readListAddress(new URLSearchParams('page=-3&inactive=yes')).page).toBe(1);
    expect(readListAddress(new URLSearchParams('page=abc')).page).toBe(1);
    expect(readListAddress(new URLSearchParams('page=2.7')).page).toBe(2);
  });
});

describe('writeListAddress', () => {
  it('leaves out what is the default, so a plain list has a plain address', () => {
    expect(writeListAddress({ search: '', page: 1, withInactive: false })).toEqual({});
  });

  it('writes back what it reads', () => {
    const address = { search: 'حسن', page: 3, withInactive: true };
    expect(readListAddress(new URLSearchParams(writeListAddress(address)))).toEqual(address);
  });
});

describe('pageCount', () => {
  it('counts the pages, and an empty list still has its one page', () => {
    expect(pageCount(0, 20)).toBe(1);
    expect(pageCount(20, 20)).toBe(1);
    expect(pageCount(21, 20)).toBe(2);
  });
});

describe('followAddress', () => {
  const box = (typed: string, followed: string, sent: string | null = null): SearchBox => ({ typed, followed, sent });

  it('follows a search cleared from outside, such as the sidebar link to the same list', () => {
    expect(followAddress(box('حسن', 'حسن'), '').typed).toBe('');
  });

  it('follows another search brought back by the back button', () => {
    expect(followAddress(box('حسن ح', 'حسن ح'), 'حسن').typed).toBe('حسن');
  });

  it('keeps what was typed while its own search was reaching the address', () => {
    // «عل» was sent after the pause, and «ی» was typed before the address caught up.
    expect(followAddress(box('علی', '', 'عل'), 'عل')).toEqual(box('علی', 'عل'));
  });

  it('keeps a space still being typed when the box already says the search', () => {
    expect(followAddress(box('حسن ', ''), 'حسن').typed).toBe('حسن ');
  });

  it('changes nothing when the address did not change', () => {
    const same = box('حس', 'حسن');
    expect(followAddress(same, 'حسن')).toBe(same);
  });
});
