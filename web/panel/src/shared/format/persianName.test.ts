import { describe, expect, it } from 'vitest';
import { normalizePersianName, samePersianName } from './persianName';

describe('normalizePersianName', () => {
  it('trims, joins spaces and turns the Arabic yeh and kaf into the Persian ones', () => {
    expect(normalizePersianName('  كيف   كوچك ')).toBe('کیف کوچک');
  });

  it('keeps the zero-width non-joiner and digits as typed', () => {
    expect(normalizePersianName('کوچک‌تر ۲')).toBe('کوچک‌تر ۲');
  });
});

describe('samePersianName', () => {
  it('finds a name typed on an Arabic keyboard to be the same name', () => {
    expect(samePersianName('بزرگ', ' بزرگ ')).toBe(true);
    expect(samePersianName('کوچک', 'كوچك')).toBe(true);
    expect(samePersianName('کوچک', 'بزرگ')).toBe(false);
  });
});
