// The API sends ISO 8601 in UTC; Jalali is a display choice made here (ADR 0005). A moment is
// shown in the workshop's time zone, never the browser's: the owner may open the panel while
// travelling, and "which day was it" must not change with him.

const workshopTimeZone = 'Asia/Tehran';

const jalaliDate: Intl.DateTimeFormatOptions = {
  calendar: 'persian',
  year: 'numeric',
  month: 'long',
  day: 'numeric',
};

const dateInTehran = new Intl.DateTimeFormat('fa-IR', { ...jalaliDate, timeZone: workshopTimeZone });
// The weekday is formatted on its own and put in front by hand: ICU's combined pattern for
// weekday + Persian date differs between versions (Node gives "۱۴۰۵ آبان ۲۸, پنجشنبه").
const weekdayInTehran = new Intl.DateTimeFormat('fa-IR', { weekday: 'long', timeZone: workshopTimeZone });
// A calendar day has no time and no zone; it is read and shown in UTC so it never shifts.
const calendarDay = new Intl.DateTimeFormat('fa-IR', { ...jalaliDate, timeZone: 'UTC' });
const calendarWeekday = new Intl.DateTimeFormat('fa-IR', { weekday: 'long', timeZone: 'UTC' });

/** The Jalali date of a moment, in Tehran: "2026-11-18T20:30:00Z" is "۲۸ آبان ۱۴۰۵". */
export function formatDate(instant: string | Date): string {
  return dateInTehran.format(toDate(instant));
}

const timeInTehran = new Intl.DateTimeFormat('fa-IR', {
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
  timeZone: workshopTimeZone,
});

/** The time of a moment in Tehran, on a 24-hour clock: "2026-11-18T06:05:00Z" is "۰۹:۳۵". */
export function formatTime(instant: string | Date): string {
  return timeInTehran.format(toDate(instant));
}

/** The same, with the weekday: "پنجشنبه ۲۸ آبان ۱۴۰۵". */
export function formatWeekdayDate(instant: string | Date): string {
  const date = toDate(instant);
  return `${weekdayInTehran.format(date)} ${dateInTehran.format(date)}`;
}

/** A calendar day from the API ("2026-11-19", a DateOnly) in Jalali, whatever the browser's zone. */
export function formatDay(day: string): string {
  return calendarDay.format(dayToDate(day));
}

/** The same, with the weekday: "2026-11-25" is "چهارشنبه ۴ آذر ۱۴۰۵". */
export function formatWeekdayDay(day: string): string {
  const date = dayToDate(day);
  return `${calendarWeekday.format(date)} ${calendarDay.format(date)}`;
}

const dayPartsInTehran = new Intl.DateTimeFormat('en-u-ca-gregory-nu-latn', {
  timeZone: workshopTimeZone,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
});

/**
 * The workshop's calendar day at a moment, as the API writes a day: "2026-11-25". The panel uses
 * it only to bound what it offers (the years of a start date); the API checks against its own today.
 */
export function dayInTehran(instant: Date = new Date()): string {
  const parts: Partial<Record<Intl.DateTimeFormatPartTypes, string>> = {};
  for (const part of dayPartsInTehran.formatToParts(instant)) {
    parts[part.type] = part.value;
  }

  return `${parts.year}-${parts.month}-${parts.day}`;
}

function dayToDate(day: string): Date {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(day);
  if (!match) {
    throw new RangeError(`Not a calendar day: ${day}`);
  }

  const [, year, month, date] = match;
  return new Date(Date.UTC(Number(year), Number(month) - 1, Number(date)));
}

function toDate(instant: string | Date): Date {
  const date = typeof instant === 'string' ? new Date(instant) : instant;
  if (Number.isNaN(date.getTime())) {
    throw new RangeError(`Not a date: ${String(instant)}`);
  }

  return date;
}
