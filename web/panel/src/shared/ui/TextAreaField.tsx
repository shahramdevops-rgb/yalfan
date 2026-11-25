import { useId, type TextareaHTMLAttributes } from 'react';

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
      <label htmlFor={id} className="text-sm font-medium text-stone-700">
        {label}
      </label>
      <textarea
        id={id}
        rows={rows}
        {...props}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
        className={`rounded-lg border bg-white px-3 py-2 text-base text-stone-900 transition-colors outline-none placeholder:text-stone-400 focus:border-brand-600 focus:ring-2 focus:ring-brand-100 ${error ? 'border-red-500' : 'border-stone-300'} ${className}`}
      />
      {hint && !error && (
        <p id={hintId} className="text-sm text-stone-500">
          {hint}
        </p>
      )}
      {error && (
        <p id={errorId} role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  );
}
