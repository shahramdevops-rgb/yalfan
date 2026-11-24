// A calendar day (a DateOnly from the API, "2026-11-25") and its Jalali year, month and day.
// Both directions go through the browser's own Persian calendar (Intl), the one formatDay shows
// dates with, so a day picked in the date field is always shown back as the same day. A separate
// algorithm or library could disagree with the browser's calendar in some years, and the picked
// day and the shown one would then differ (ADR 0024).

export interface JalaliDay {
  year: number;
  /** 1 is Farvardin, 12 Esfand. */
  month: number;
  day: number;
}

// A calendar day is read in UTC, as formatDay does, so the machine's time zone never shifts it.
const parts = new Intl.DateTimeFormat('en-u-ca-persian-nu-latn', {
  timeZone: 'UTC',
  year: 'numeric',
  month: 'numeric',
  day: 'numeric',
});

const dayMs = 86_400_000;
const isoDay = /^(\d{4})-(\d{2})-(\d{2})$/;

/** The Jalali day of a calendar day: "2026-11-25" is 1405/9/4. */
export function toJalali(day: string): JalaliDay {
  return jalaliOf(new Date(utcMidnight(day)));
}

/**
 * The calendar day of a Jalali day, or null when there is no such day (the 30th of Esfand in a
 * year that is not a leap year, the 31st of Mehr).
 */
export function fromJalali({ year, month, day }: JalaliDay): string | null {
  if (month < 1 || month > 12 || day < 1 || day > 31) {
    return null;
  }

  // 1 Farvardin falls on 19 to 22 March, so starting from 21 March plus the day's place in the
  // year lands within two days of the answer; then step towards it. A day that does not exist is
  // stepped over back and forth, and the bound ends the search.
  const placeInYear = month <= 6 ? (month - 1) * 31 : 186 + (month - 7) * 30;
  let time = Date.UTC(year + 621, 2, 21) + (placeInYear + day - 1) * dayMs;

  for (let step = 0; step < 6; step++) {
    const order = compare(jalaliOf(new Date(time)), { year, month, day });
    if (order === 0) {
      return new Date(time).toISOString().slice(0, 10);
    }

    time += order < 0 ? dayMs : -dayMs;
  }

  return null;
}

/** 31 days in the first six months, 30 in the next five, and Esfand 30 in a leap year, else 29. */
export function daysInJalaliMonth(year: number, month: number): number {
  if (month <= 6) {
    return 31;
  }

  if (month <= 11) {
    return 30;
  }

  return fromJalali({ year, month: 12, day: 30 }) ? 30 : 29;
}

const monthName = new Intl.DateTimeFormat('fa-IR-u-ca-persian', { timeZone: 'UTC', month: 'long' });

/** The twelve month names, «فروردین» to «اسفند», as the dates are shown. */
export const jalaliMonthNames: readonly string[] = Array.from({ length: 12 }, (_, index) =>
  monthName.format(new Date(utcMidnight(fromJalali({ year: 1405, month: index + 1, day: 1 })!))),
);

function jalaliOf(date: Date): JalaliDay {
  const found: Partial<Record<Intl.DateTimeFormatPartTypes, number>> = {};
  for (const part of parts.formatToParts(date)) {
    found[part.type] = Number(part.value);
  }

  return { year: found.year!, month: found.month!, day: found.day! };
}

function compare(a: JalaliDay, b: JalaliDay): number {
  return a.year - b.year || a.month - b.month || a.day - b.day;
}

function utcMidnight(day: string): number {
  const match = isoDay.exec(day);
  if (!match) {
    throw new RangeError(`Not a calendar day: ${day}`);
  }

  const [, year, month, date] = match;
  return Date.UTC(Number(year), Number(month) - 1, Number(date));
}
