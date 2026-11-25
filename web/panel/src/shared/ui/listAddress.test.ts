import { describe, expect, it } from 'vitest';
import { pageCount, readListAddress, writeListAddress } from './listAddress';

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
