import { formatWeekdayDay } from '../format/dates';
import { toPersianDigits } from '../format/digits';
import { daysInJalaliMonth, fromJalali, jalaliMonthNames, toJalali, type JalaliDay } from '../format/jalali';
import { t } from '../i18n/fa';

interface JalaliDateFieldProps {
  label: string;
  /** A calendar day as the API takes it, "2026-11-25". */
  value: string;
  onChange: (day: string) => void;
  /** The first and the last Jalali year offered. */
  years: { from: number; to: number };
  /** A quieter line under the field, such as what the chosen day means. */
  hint?: string;
  disabled?: boolean;
}

const selectClass =
  'h-12 rounded-lg border border-stone-300 bg-white px-2 text-base outline-none focus:border-brand-600 focus:ring-2 focus:ring-brand-100 disabled:opacity-60';

/**
 * A Jalali day picked from three lists, day, month and year, in the order a Persian date is
 * read. Nothing is typed, so nothing can be mistyped, and a phone opens its own picker for each
 * list. The day list follows the month (Esfand has its 30th only in a leap year), and the chosen
 * day is written out with its weekday under the lists (ADR 0024).
 */
export function JalaliDateField({ label, value, onChange, years, hint, disabled }: JalaliDateFieldProps) {
  const chosen = toJalali(value);

  function pick(change: Partial<JalaliDay>) {
    const next = { ...chosen, ...change };
    // The 31st of a 30-day month becomes its 30th, and so on, instead of no day at all.
    next.day = Math.min(next.day, daysInJalaliMonth(next.year, next.month));
    const day = fromJalali(next);
    if (day) {
      onChange(day);
    }
  }

  const days = Array.from({ length: daysInJalaliMonth(chosen.year, chosen.month) }, (_, index) => index + 1);
  const yearList = Array.from(
    { length: Math.max(years.to, chosen.year) - Math.min(years.from, chosen.year) + 1 },
    (_, index) => Math.min(years.from, chosen.year) + index,
  );

  return (
    <fieldset className="flex flex-col gap-1.5" disabled={disabled}>
      <legend className="mb-1.5 text-sm font-medium text-stone-700">{label}</legend>
      {/* One row even on a phone: the month takes what the day and the year leave. */}
      <div className="grid max-w-sm grid-cols-[4rem_minmax(0,1fr)_5rem] gap-2">
        <select
          aria-label={t('date.day')}
          className={selectClass}
          value={chosen.day}
          onChange={(event) => pick({ day: Number(event.target.value) })}
        >
          {days.map((day) => (
            <option key={day} value={day}>
              {toPersianDigits(String(day))}
            </option>
          ))}
        </select>
        <select
          aria-label={t('date.month')}
          className={selectClass}
          value={chosen.month}
          onChange={(event) => pick({ month: Number(event.target.value) })}
        >
          {jalaliMonthNames.map((name, index) => (
            <option key={name} value={index + 1}>
              {name}
            </option>
          ))}
        </select>
        <select
          aria-label={t('date.year')}
          className={selectClass}
          value={chosen.year}
          onChange={(event) => pick({ year: Number(event.target.value) })}
        >
          {yearList.map((year) => (
            <option key={year} value={year}>
              {toPersianDigits(String(year))}
            </option>
          ))}
        </select>
      </div>
      <p className="text-sm text-stone-600" aria-live="polite">
        {formatWeekdayDay(value)}
      </p>
      {hint && <p className="text-sm text-stone-500">{hint}</p>}
    </fieldset>
  );
}
