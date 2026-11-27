import { useRef, useState, type KeyboardEvent } from 'react';
import { formatWeekdayDay } from '../format/dates';
import { toPersianDigits } from '../format/digits';
import { daysInJalaliMonth, fromJalali, jalaliMonthNames, toJalali, type JalaliDay } from '../format/jalali';
import { readTypedNumber, typedDigit } from '../format/typedNumber';
import { t } from '../i18n/fa';
import { FieldError } from './Form';

interface JalaliDateFieldProps {
  label: string;
  /**
   * A calendar day as the API takes it, "2026-11-25", or null for a day not picked yet: each list
   * then starts empty, and `onChange` is called once all three are picked.
   */
  value: string | null;
  onChange: (day: string) => void;
  /** The first and the last Jalali year offered. */
  years: { from: number; to: number };
  /** The year list starts with the latest year, for a day that is almost always in the past. */
  newestFirst?: boolean;
  /** A quieter line under the field, such as what the chosen day means. */
  hint?: string;
  error?: string;
  disabled?: boolean;
  /** The day list takes the focus when it is drawn: the first field of a small form opened by a tap. */
  autoFocus?: boolean;
}

/** A leap year, so Esfand offers its 30th until the year is picked; picking a year clamps it. */
const leapYear = 1403;

/** How long a list waits for the next digit of one number, as a list's own type-ahead does. */
const typingPause = 1000;

const selectClass =
  'h-12 rounded-lg border bg-surface px-2 text-base outline-none focus:border-brand-line focus:ring-2 focus:ring-brand-glow disabled:opacity-60';

/**
 * A Jalali day picked from three lists, day, month and year, in the order a Persian date is
 * read. Nothing is typed, so nothing can be mistyped, and a phone opens its own picker for each
 * list. The day list follows the month (Esfand has its 30th only in a leap year), and the chosen
 * day is written out with its weekday under the lists (ADR 0024). A day that must be chosen on
 * purpose, such as a worker's start date, starts empty instead of on today (ADR 0025).
 *
 * A number typed on a list picks it, with Latin, Persian or Arabic digits: «۱۵» the 15th, «۹»
 * Azar, «۱۴۰۳» the year (ADR 0031). The lists show Persian digits, which the browser's own
 * type-ahead matches only to Persian digits typed, never to the numpad's.
 */
export function JalaliDateField({
  label,
  value,
  onChange,
  years,
  newestFirst = false,
  hint,
  error,
  disabled,
  autoFocus,
}: JalaliDateFieldProps) {
  // The lists picked so far while the day is not whole yet.
  const [partial, setPartial] = useState<Partial<JalaliDay>>({});
  // The digits typed so far into one list, and when the last one was.
  const typing = useRef({ list: '', typed: '', at: 0 });
  const chosen: Partial<JalaliDay> = value ? toJalali(value) : partial;

  function pick(change: Partial<JalaliDay>) {
    const next = { ...chosen, ...change };
    const { year, month } = next;
    if (year === undefined || month === undefined || next.day === undefined) {
      setPartial(next);
      return;
    }

    // The 31st of a 30-day month becomes its 30th, and so on, instead of no day at all.
    const day = fromJalali({ year, month, day: Math.min(next.day, daysInJalaliMonth(year, month)) });
    if (day) {
      onChange(day);
    }
  }

  const dayCount = chosen.month === undefined ? 31 : daysInJalaliMonth(chosen.year ?? leapYear, chosen.month);
  const days = Array.from({ length: dayCount }, (_, index) => index + 1);
  const first = Math.min(years.from, chosen.year ?? years.from);
  const last = Math.max(years.to, chosen.year ?? years.to);
  const yearList = Array.from({ length: last - first + 1 }, (_, index) => first + index);
  if (newestFirst) {
    yearList.reverse();
  }

  /** A digit typed on a list: added to the number being typed, which is picked once it is whole. */
  function typeDigit(list: keyof JalaliDay, values: readonly number[], event: KeyboardEvent<HTMLSelectElement>) {
    const digit = typedDigit(event.key);
    if (digit === null || event.ctrlKey || event.altKey || event.metaKey) {
      return;
    }

    // The browser's own type-ahead would act on the same key, matching the label's text.
    event.preventDefault();
    const before = typing.current;
    const goesOn = before.list === list && event.timeStamp - before.at < typingPause;
    const read = readTypedNumber((goesOn ? before.typed : '') + digit, values);
    typing.current = { list, typed: read.kept, at: event.timeStamp };
    if (read.pick !== undefined && read.pick !== chosen[list]) {
      pick({ [list]: read.pick });
    }
  }

  const months = jalaliMonthNames.map((_, index) => index + 1);
  const empty = value === null;
  const border = error ? 'border-danger-line' : 'border-line-strong';

  return (
    <fieldset className="flex flex-col gap-1.5" disabled={disabled}>
      <legend className="mb-1.5 text-sm font-medium text-label">{label}</legend>
      {/* One row even on a phone: the month takes what the day and the year leave. */}
      <div className="grid max-w-sm grid-cols-[4rem_minmax(0,1fr)_5rem] gap-2">
        <select
          aria-label={t('date.day')}
          aria-invalid={error ? true : undefined}
          autoFocus={autoFocus}
          className={`${selectClass} ${border}`}
          value={chosen.day ?? ''}
          onChange={(event) => pick({ day: Number(event.target.value) })}
          onKeyDown={(event) => typeDigit('day', days, event)}
        >
          {chosen.day === undefined && (
            <option value="" disabled>
              {t('date.day')}
            </option>
          )}
          {days.map((day) => (
            <option key={day} value={day}>
              {toPersianDigits(String(day))}
            </option>
          ))}
        </select>
        <select
          aria-label={t('date.month')}
          aria-invalid={error ? true : undefined}
          className={`${selectClass} ${border}`}
          value={chosen.month ?? ''}
          onChange={(event) => pick({ month: Number(event.target.value) })}
          onKeyDown={(event) => typeDigit('month', months, event)}
        >
          {chosen.month === undefined && (
            <option value="" disabled>
              {t('date.month')}
            </option>
          )}
          {jalaliMonthNames.map((name, index) => (
            <option key={name} value={index + 1}>
              {name}
            </option>
          ))}
        </select>
        <select
          aria-label={t('date.year')}
          aria-invalid={error ? true : undefined}
          className={`${selectClass} ${border}`}
          value={chosen.year ?? ''}
          onChange={(event) => pick({ year: Number(event.target.value) })}
          onKeyDown={(event) => typeDigit('year', yearList, event)}
        >
          {chosen.year === undefined && (
            <option value="" disabled>
              {t('date.year')}
            </option>
          )}
          {yearList.map((year) => (
            <option key={year} value={year}>
              {toPersianDigits(String(year))}
            </option>
          ))}
        </select>
      </div>
      {!empty && (
        <p className="text-sm text-body" aria-live="polite">
          {formatWeekdayDay(value)}
        </p>
      )}
      {hint && !error && <p className="text-sm text-muted">{hint}</p>}
      {error && <FieldError>{error}</FieldError>}
    </fieldset>
  );
}
