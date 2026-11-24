import { describe, expect, it } from 'vitest';
import { formatDay } from './dates';
import { daysInJalaliMonth, fromJalali, jalaliMonthNames, toJalali } from './jalali';

// These run with TZ=America/Los_Angeles (vite.config.ts), so a conversion that leaned on the
// machine's zone would shift days here.

const dayMs = 86_400_000;
const isoDay = (time: number) => new Date(time).toISOString().slice(0, 10);

describe('toJalali and fromJalali', () => {
  it('convert known days both ways', () => {
    expect(toJalali('2026-11-25')).toEqual({ year: 1405, month: 9, day: 4 });
    expect(toJalali('2026-03-21')).toEqual({ year: 1405, month: 1, day: 1 });
    expect(fromJalali({ year: 1405, month: 9, day: 4 })).toBe('2026-11-25');
    expect(fromJalali({ year: 1405, month: 1, day: 1 })).toBe('2026-03-21');
  });

  it('go there and back for every day from 1395 to 1410', () => {
    const start = Date.UTC(2016, 2, 20); // 1 Farvardin 1395
    const end = Date.UTC(2032, 2, 20);
    for (let time = start; time < end; time += dayMs) {
      const day = isoDay(time);
      expect(fromJalali(toJalali(day))).toBe(day);
    }
  });

  it('agree with how the panel shows a day, the same calendar both ways', () => {
    const day = fromJalali({ year: 1403, month: 12, day: 30 })!;
    expect(formatDay(day)).toBe('۳۰ اسفند ۱۴۰۳');
  });

  it('find no 30th of Esfand in a year that is not a leap year, and no 31st of Mehr', () => {
    expect(fromJalali({ year: 1403, month: 12, day: 30 })).toBe('2025-03-20');
    expect(fromJalali({ year: 1404, month: 12, day: 30 })).toBeNull();
    expect(fromJalali({ year: 1405, month: 7, day: 31 })).toBeNull();
    expect(fromJalali({ year: 1405, month: 13, day: 1 })).toBeNull();
  });

  it('refuse something that is not a calendar day', () => {
    expect(() => toJalali('2026-11-25T00:00:00Z')).toThrow(RangeError);
  });
});

describe('daysInJalaliMonth', () => {
  it('gives 31, then 30, and Esfand 30 only in a leap year', () => {
    expect(daysInJalaliMonth(1405, 1)).toBe(31);
    expect(daysInJalaliMonth(1405, 6)).toBe(31);
    expect(daysInJalaliMonth(1405, 7)).toBe(30);
    expect(daysInJalaliMonth(1405, 11)).toBe(30);
    expect(daysInJalaliMonth(1403, 12)).toBe(30);
    expect(daysInJalaliMonth(1404, 12)).toBe(29);
  });
});

it('names the months as the dates are shown', () => {
  expect(jalaliMonthNames).toHaveLength(12);
  expect(jalaliMonthNames[0]).toBe('فروردین');
  expect(jalaliMonthNames[8]).toBe('آذر');
  expect(jalaliMonthNames[11]).toBe('اسفند');
});
