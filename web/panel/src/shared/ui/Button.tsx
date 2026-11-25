import type { ButtonHTMLAttributes } from 'react';

type Variant = 'primary' | 'secondary' | 'danger' | 'link';

const variants: Record<Variant, string> = {
  primary:
    'bg-brand-700 text-white hover:bg-brand-800 focus-visible:outline-brand-700 disabled:bg-stone-300 disabled:text-stone-500',
  secondary:
    'bg-white text-stone-800 ring-1 ring-stone-300 hover:bg-stone-50 focus-visible:outline-brand-700 disabled:text-stone-400',
  // A rare step that takes something away, such as removing an IBAN: quiet, but not mistaken for the save.
  danger:
    'bg-white text-red-700 ring-1 ring-red-200 hover:bg-red-50 focus-visible:outline-red-700 disabled:text-stone-400',
  link: 'text-brand-700 hover:text-brand-800 hover:underline disabled:text-stone-400 disabled:no-underline',
};

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  busy?: boolean;
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
  const size = variant === 'link' ? 'px-1 py-1 text-sm' : 'h-12 px-5 text-base';
  return (
    <button
      type="button"
      {...props}
      disabled={disabled || busy}
      aria-busy={busy || undefined}
      className={`inline-flex items-center justify-center gap-2 rounded-lg font-medium transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 disabled:cursor-not-allowed ${size} ${variants[variant]} ${className}`}
    >
      {busy && (
        <span className="size-4 animate-spin rounded-full border-2 border-current border-e-transparent" aria-hidden />
      )}
      {children}
    </button>
  );
}
