import { useId, useLayoutEffect, useRef, type ChangeEvent } from 'react';
import { groupDigits, readAmountInput } from '../format/amountInput';
import { tomanLine } from '../format/money';
import { t } from '../i18n/fa';

interface AmountFieldProps {
  label: string;
  /** Latin digits without leading zeros, '' when empty: the Rial amount as typed. */
  value: string;
  onChange: (digits: string) => void;
  error?: string;
  autoFocus?: boolean;
  disabled?: boolean;
}

/**
 * Where an amount is typed, always in Rial (ADR 0002). The digits are grouped in threes while
 * they are typed, «ریال» sits inside the field, and under it the Toman equivalent and its words
 * follow every key, because a Toman typed by habit into a Rial field is the usual mistake and
 * must show at that moment (ADR 0002 point 9, ADR 0024).
 */
export function AmountField({ label, value, onChange, error, autoFocus, disabled }: AmountFieldProps) {
  const id = useId();
  const input = useRef<HTMLInputElement>(null);
  // Where the caret goes once the grouped text is drawn; grouping moves the characters under it.
  const caret = useRef<number | null>(null);

  useLayoutEffect(() => {
    if (caret.current !== null && input.current === document.activeElement) {
      input.current?.setSelectionRange(caret.current, caret.current);
    }

    caret.current = null;
  });

  function change(event: ChangeEvent<HTMLInputElement>) {
    const field = event.target;
    const read = readAmountInput(field.value, field.selectionStart ?? field.value.length);

    // A letter or a separator typed changes no digit, so nothing is drawn again: put the text
    // back and the caret where it was, here.
    if (read.digits === value) {
      field.value = read.display;
      field.setSelectionRange(read.caret, read.caret);
      return;
    }

    caret.current = read.caret;
    onChange(read.digits);
  }

  const helpId = `${id}-help`;
  const errorId = `${id}-error`;

  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium text-stone-700">
        {label} ({t('money.rial')})
      </label>
      <div className="relative sm:max-w-xs">
        {/* Digits and their separators read left to right; the field is left-to-right for them. */}
        <input
          ref={input}
          id={id}
          dir="ltr"
          inputMode="numeric"
          autoComplete="off"
          value={groupDigits(value)}
          onChange={change}
          autoFocus={autoFocus}
          disabled={disabled}
          aria-invalid={error ? true : undefined}
          aria-describedby={[value && helpId, error && errorId].filter(Boolean).join(' ') || undefined}
          className={`h-12 w-full rounded-lg border bg-white ps-14 pe-3 text-end text-lg font-medium tracking-wide text-stone-900 outline-none focus:border-brand-600 focus:ring-2 focus:ring-brand-100 disabled:opacity-60 ${
            error ? 'border-red-500' : 'border-stone-300'
          }`}
        />
        <span className="pointer-events-none absolute inset-y-0 end-3 flex items-center text-sm text-stone-500">
          {t('money.rial')}
        </span>
      </div>
      {/* Always present, so the form does not jump when the first digit is typed. */}
      <p id={helpId} className="min-h-5 text-sm text-stone-600" aria-live="polite">
        {value ? tomanLine(value as `${number}`) : ''}
      </p>
      {error && (
        <p id={errorId} role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  );
}
