import { useId, type TextareaHTMLAttributes } from 'react';
import { FieldError } from './Form';

interface TextAreaFieldProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  label: string;
  hint?: string;
  error?: string;
}

/** A few lines of free text with its label, such as a contractor's notes; the twin of TextField. */
export function TextAreaField({ label, hint, error, className = '', rows = 3, ...props }: TextAreaFieldProps) {
  const id = useId();
  const hintId = `${id}-hint`;
  const errorId = `${id}-error`;
  const describedBy = [hint && hintId, error && errorId].filter(Boolean).join(' ') || undefined;

  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium text-label">
        {label}
      </label>
      <textarea
        id={id}
        rows={rows}
        {...props}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
        className={`rounded-lg border bg-surface px-3 py-2 text-base text-ink transition-colors outline-none placeholder:text-faint focus:border-brand-line focus:ring-2 focus:ring-brand-glow ${error ? 'border-danger-line' : 'border-line-strong'} ${className}`}
      />
      {hint && !error && (
        <p id={hintId} className="text-sm text-muted">
          {hint}
        </p>
      )}
      {error && <FieldError id={errorId}>{error}</FieldError>}
    </div>
  );
}
