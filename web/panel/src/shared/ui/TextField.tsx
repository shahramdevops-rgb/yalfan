import { useContext, useId, type InputHTMLAttributes } from 'react';
import { FieldError } from './Form';
import { EnterMovesOn } from './formKeys';

interface TextFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string;
  hint?: string;
  error?: string;
}

export function TextField({ label, hint, error, className = '', ...props }: TextFieldProps) {
  const id = useId();
  const hintId = `${id}-hint`;
  const errorId = `${id}-error`;
  const describedBy = [hint && hintId, error && errorId].filter(Boolean).join(' ') || undefined;
  const movesOn = useContext(EnterMovesOn);

  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium text-label">
        {label}
      </label>
      <input
        id={id}
        enterKeyHint={movesOn ? 'next' : undefined}
        {...props}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
        className={`h-12 rounded-lg border bg-surface px-3 text-lg tracking-wide text-ink transition-colors outline-none placeholder:text-faint focus:border-brand-line focus:ring-2 focus:ring-brand-glow ${error ? 'border-danger-line' : 'border-line-strong'} ${className}`}
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
