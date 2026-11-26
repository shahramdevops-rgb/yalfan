import { useEffect, useId, useLayoutEffect, useRef, type ChangeEvent } from 'react';
import { groupIbanDigits, readIbanInput } from '../format/iban';
import { FieldError } from './Form';

interface IbanFieldProps {
  label: string;
  /** Up to 24 Latin digits, the IBAN without its IR; '' when empty. */
  value: string;
  onChange: (digits: string) => void;
  hint?: string;
  error?: string;
  autoFocus?: boolean;
  disabled?: boolean;
  /**
   * The caret returns to the field when a refusal comes back, so the digit can be fixed at once.
   * Off on a form with several fields, where it would pull the caret past the first mistake.
   */
  focusOnError?: boolean;
}

/**
 * Where an Iranian IBAN (شبا) is typed: IR stands fixed in front and the 24 digits are grouped as
 * on a bank card while they are typed, left to right, so they can be read against the card or the
 * paper book. Persian, Arabic and Latin digits are taken, and a whole IBAN pasted with its IR is
 * read as its digits (ADR 0025). The check digits are checked by the API.
 */
export function IbanField({
  label,
  value,
  onChange,
  hint,
  error,
  autoFocus,
  disabled,
  focusOnError = false,
}: IbanFieldProps) {
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

  // The field was disabled while the request ran, which takes the caret out of it.
  useEffect(() => {
    if (focusOnError && error && !disabled) {
      input.current?.focus();
    }
  }, [focusOnError, error, disabled]);

  function change(event: ChangeEvent<HTMLInputElement>) {
    const field = event.target;
    const read = readIbanInput(field.value, field.selectionStart ?? field.value.length);

    // A letter or a space typed changes no digit, so nothing is drawn again: put the text back here.
    if (read.digits === value) {
      field.value = read.display;
      field.setSelectionRange(read.caret, read.caret);
      return;
    }

    caret.current = read.caret;
    onChange(read.digits);
  }

  const hintId = `${id}-hint`;
  const errorId = `${id}-error`;

  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium text-label">
        {label}
      </label>
      {/* Left to right, like the number on the card: IR on the left, then the digits. */}
      <div dir="ltr" className="relative sm:max-w-md">
        <span className="pointer-events-none absolute inset-y-0 start-3 flex items-center text-lg font-medium text-muted">
          IR
        </span>
        <input
          ref={input}
          id={id}
          inputMode="numeric"
          autoComplete="off"
          spellCheck={false}
          value={groupIbanDigits(value)}
          onChange={change}
          autoFocus={autoFocus}
          disabled={disabled}
          aria-invalid={error ? true : undefined}
          aria-describedby={[hint && !error && hintId, error && errorId].filter(Boolean).join(' ') || undefined}
          className={`h-12 w-full rounded-lg border bg-surface ps-10 pe-3 text-lg tracking-wide text-ink outline-none focus:border-brand-line focus:ring-2 focus:ring-brand-glow disabled:opacity-60 ${
            error ? 'border-danger-line' : 'border-line-strong'
          }`}
        />
      </div>
      {hint && !error && (
        <p id={hintId} className="text-sm text-muted">
          {hint}
        </p>
      )}
      {error && <FieldError id={errorId}>{error}</FieldError>}
    </div>
  );
}
