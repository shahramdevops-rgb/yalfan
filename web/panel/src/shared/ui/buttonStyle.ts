export type ButtonVariant = 'primary' | 'secondary' | 'danger' | 'link';

const variants: Record<ButtonVariant, string> = {
  primary:
    'bg-brand text-on-brand hover:bg-brand-strong focus-visible:outline-brand disabled:bg-fill-off disabled:text-muted',
  secondary:
    'bg-surface text-ink-soft ring-1 ring-line-strong hover:bg-subtle focus-visible:outline-brand disabled:text-faint',
  // A rare step that takes something away, such as removing an IBAN: quiet, but not mistaken for the save.
  danger:
    'bg-surface text-danger ring-1 ring-danger-edge hover:bg-danger-soft focus-visible:outline-danger disabled:text-faint',
  link: 'text-brand hover:text-brand-strong hover:underline disabled:text-faint disabled:no-underline',
};

/**
 * The look of a button, shared by `Button` and by `ButtonLink`, a link that goes to another page
 * («مدل جدید»), so the two can never drift apart. Large and clear on purpose: the office operator
 * is not technical (CLAUDE.md).
 */
export function buttonClass(variant: ButtonVariant, extra = ''): string {
  const size = variant === 'link' ? 'px-1 py-1 text-sm' : 'h-12 px-5 text-base';
  return `inline-flex items-center justify-center gap-2 rounded-lg font-medium transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 disabled:cursor-not-allowed ${size} ${variants[variant]} ${extra}`;
}
