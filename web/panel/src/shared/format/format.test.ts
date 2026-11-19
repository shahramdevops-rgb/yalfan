import { describe, expect, it } from 'vitest';
import { formatDate, formatDay, formatWeekdayDate } from './dates';
import { digitsOnly, formatCountdown, toLatinDigits, toPersianDigits } from './digits';
import { formatRial } from './money';

// These run with TZ=America/Los_Angeles (vite.config.ts), so a formatter that used the
// machine's zone instead of Tehran's would fail.

it('runs in a time zone far from Tehran', () => {
  expect(Intl.DateTimeFormat().resolvedOptions().timeZone).toBe('America/Los_Angeles');
});

describe('formatDate', () => {
  it('shows the Jalali day in Tehran, which starts at 20:30 UTC', () => {
    expect(formatDate('2026-11-18T20:29:59Z')).toBe('۲۷ آبان ۱۴۰۵');
    expect(formatDate('2026-11-18T20:30:00Z')).toBe('۲۸ آبان ۱۴۰۵');
  });

  it('adds the weekday of the Tehran day, not of the UTC or the local one', () => {
    // Wednesday 21:00 in UTC and 13:00 in Los Angeles, already Thursday 00:30 in Tehran.
    expect(formatWeekdayDate('2026-11-18T21:00:00Z')).toBe('پنجشنبه ۲۸ آبان ۱۴۰۵');
  });

  it('refuses something that is not a date', () => {
    expect(() => formatDate('yesterday')).toThrow(RangeError);
  });
});

describe('formatDay', () => {
  it('shows a calendar day without shifting it by a time zone', () => {
    expect(formatDay('2026-11-19')).toBe('۲۸ آبان ۱۴۰۵');
    expect(formatDay('2026-03-21')).toBe('۱ فروردین ۱۴۰۵');
  });

  it('refuses a moment where a calendar day is expected', () => {
    expect(() => formatDay('2026-11-19T00:00:00Z')).toThrow(RangeError);
  });
});

describe('formatRial', () => {
  it('uses Persian digits, thousands separators and the Rial label', () => {
    expect(formatRial(12_500_000)).toBe('۱۲٬۵۰۰٬۰۰۰ ریال');
    expect(formatRial(0)).toBe('۰ ریال');
  });

  it('formats a decimal string from the API exactly, beyond what a double can hold', () => {
    expect(formatRial('98765432109876543')).toBe('۹۸٬۷۶۵٬۴۳۲٬۱۰۹٬۸۷۶٬۵۴۳ ریال');
  });
});

describe('digits', () => {
  it('turns Persian and Arabic digits into Latin ones and leaves the rest', () => {
    expect(toLatinDigits('۰۹۱۲ ٣٤٥')).toBe('0912 345');
  });

  it('shows Latin digits as Persian ones, keeping the leading zero of a mobile number', () => {
    expect(toPersianDigits('09121234567')).toBe('۰۹۱۲۱۲۳۴۵۶۷');
  });

  it('keeps only the digits of a typed number', () => {
    expect(digitsOnly('۰۹۱۲ ۱۲۳-۴۵۶۷')).toBe('09121234567');
  });

  it('shows a countdown as minutes and seconds in Persian digits', () => {
    expect(formatCountdown(105)).toBe('۱:۴۵');
    expect(formatCountdown(59.2)).toBe('۱:۰۰');
    expect(formatCountdown(-3)).toBe('۰:۰۰');
  });
});
