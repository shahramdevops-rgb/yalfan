import { describe, expect, it } from 'vitest';
import { persianWords } from './numberWords';

describe('persianWords', () => {
  it.each([
    [0n, 'صفر'],
    [1n, 'یک'],
    [10n, 'ده'],
    [12n, 'دوازده'],
    [18n, 'هجده'],
    [20n, 'بیست'],
    [21n, 'بیست و یک'],
    [100n, 'صد'],
    [101n, 'صد و یک'],
    [110n, 'صد و ده'],
    [250n, 'دویست و پنجاه'],
    [999n, 'نهصد و نود و نه'],
  ])('reads %s below a thousand', (value, words) => {
    expect(persianWords(value)).toBe(words);
  });

  it('says a thousand without «یک», as it is said aloud', () => {
    expect(persianWords(1_000n)).toBe('هزار');
    expect(persianWords(1_001n)).toBe('هزار و یک');
    expect(persianWords(12_000n)).toBe('دوازده هزار');
    expect(persianWords(150_500n)).toBe('صد و پنجاه هزار و پانصد');
  });

  it('reads millions and billions', () => {
    expect(persianWords(1_000_000n)).toBe('یک میلیون');
    expect(persianWords(1_250_000n)).toBe('یک میلیون و دویست و پنجاه هزار');
    expect(persianWords(3_000_004n)).toBe('سه میلیون و چهار');
    expect(persianWords(1_000_000_000n)).toBe('یک میلیارد');
    expect(persianWords(7_000_001_000n)).toBe('هفت میلیارد و هزار');
  });

  it('reads the count of billions out above a billion billions', () => {
    expect(persianWords(2_500_000_000_000n)).toBe('دو هزار و پانصد میلیارد');
    expect(persianWords(1_000_000_000_000n)).toBe('هزار میلیارد');
    // The largest int64, so any amount the API can send has its words.
    expect(persianWords(9_223_372_036_854_775_807n)).toBe(
      'نه میلیارد و دویست و بیست و سه میلیون و سیصد و هفتاد و دو هزار و سی و شش میلیارد و هشتصد و پنجاه و چهار میلیون و هفتصد و هفتاد و پنج هزار و هشتصد و هفت',
    );
  });

  it('starts a negative number with «منفی»', () => {
    expect(persianWords(-12_000n)).toBe('منفی دوازده هزار');
  });
});
