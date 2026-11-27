import type { ButtonHTMLAttributes, Ref } from 'react';
import { buttonClass, type ButtonVariant } from './buttonStyle';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  busy?: boolean;
  /** For a dialog that gives one of its buttons the focus when it opens. */
  ref?: Ref<HTMLButtonElement>;
}

/** Large and clear on purpose: the office operator is not technical (CLAUDE.md). */
export function Button({
  variant = 'primary',
  busy = false,
  disabled,
  className = '',
  children,
  ...props
}: ButtonProps) {
  return (
    <button
      type="button"
      {...props}
      disabled={disabled || busy}
      aria-busy={busy || undefined}
      className={buttonClass(variant, className)}
    >
      {busy && (
        <span className="size-4 animate-spin rounded-full border-2 border-current border-e-transparent" aria-hidden />
      )}
      {children}
    </button>
  );
}
