import { expect, it } from 'vitest';
import { byPersianName } from './persianOrder';

it('sorts in Persian alphabetical order, not in the order of the Unicode code points', () => {
  // «پ» and «چ» come after «ی» in Unicode but between «ب» and «ت», and «ح» in the alphabet.
  const names = ['چرم', 'تترون', 'پارچه', 'برزنت', 'آبی'].map((name) => ({ name }));

  expect(byPersianName(names).map((item) => item.name)).toEqual(['آبی', 'برزنت', 'پارچه', 'تترون', 'چرم']);
});

it('returns a new list and leaves the given one as it was', () => {
  const names = [{ name: 'ب' }, { name: 'آ' }];

  byPersianName(names);

  expect(names.map((item) => item.name)).toEqual(['ب', 'آ']);
});
