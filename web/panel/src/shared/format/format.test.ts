import { describe, expect, it } from 'vitest';
import { dayInTehran, formatDate, formatDay, formatTime, formatWeekdayDate, formatWeekdayDay } from './dates';
import { digitsOnly, formatCountdown, toLatinDigits, toPersianDigits } from './digits';
import { formatRial, tomanLine } from './money';

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

describe('formatTime', () => {
  it('shows the time in Tehran on a 24-hour clock', () => {
    expect(formatTime('2026-11-18T06:05:00Z')).toBe('۰۹:۳۵');
    expect(formatTime('2026-11-18T20:30:00Z')).toBe('۰۰:۰۰');
    expect(formatTime('2026-11-18T12:00:00Z')).toBe('۱۵:۳۰');
  });
});

describe('dayInTehran', () => {
  it('is the calendar day in Tehran, not in the zone of the machine', () => {
    // 21:00 UTC on the 18th is 13:00 on the 18th in Los Angeles, but already the 19th in Tehran.
    expect(dayInTehran(new Date('2026-11-18T21:00:00Z'))).toBe('2026-11-19');
    expect(dayInTehran(new Date('2026-11-18T20:00:00Z'))).toBe('2026-11-18');
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

  it('adds the weekday of the day itself, whatever the browser zone', () => {
    expect(formatWeekdayDay('2026-11-25')).toBe('چهارشنبه ۴ آذر ۱۴۰۵');
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

describe('tomanLine', () => {
  it('says the amount in words, in Toman, with no second figure (ADR 0024)', () => {
    expect(tomanLine(120_000)).toBe('دوازده هزار تومان');
    expect(tomanLine(12_500_000)).toBe('یک میلیون و دویست و پنجاه هزار تومان');
  });

  it('never rounds: a Rial amount that is not a multiple of ten keeps its last Rial', () => {
    expect(tomanLine(12_345)).toBe('هزار و دویست و سی و چهار تومان و پنج ریال');
    expect(tomanLine(5)).toBe('پنج ریال');
  });

  it('reads zero and a negative amount, for the ledgers to come', () => {
    expect(tomanLine(0)).toBe('صفر تومان');
    expect(tomanLine(-120_000)).toBe('منفی دوازده هزار تومان');
    expect(tomanLine(-5)).toBe('منفی پنج ریال');
  });

  it('reads a decimal string from the API exactly', () => {
    expect(tomanLine('98765432109876540')).toBe(
      'نه میلیون و هشتصد و هفتاد و شش هزار و پانصد و چهل و سه میلیارد و دویست و ده میلیون و نهصد و هشتاد و هفت هزار و ششصد و پنجاه و چهار تومان',
    );
  });

  it('refuses a fraction of a Rial or a number too large to hold exactly', () => {
    expect(() => tomanLine(1.5)).toThrow(RangeError);
    expect(() => tomanLine(2 ** 60)).toThrow(RangeError);
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
