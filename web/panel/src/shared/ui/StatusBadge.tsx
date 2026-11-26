type Tone = 'brand' | 'warning' | 'neutral' | 'muted';

const tones: Record<Tone, string> = {
  brand: 'bg-brand-soft text-brand-strong',
  warning: 'bg-warning-soft text-warning-strong',
  neutral: 'bg-fill text-body',
  muted: 'bg-fill text-muted',
};

/**
 * A short state beside a name: «غیرفعال», «شما», a rate's «جاری» or «از آینده». Always a word,
 * never a colour alone, because a colour says nothing to someone who cannot tell it apart, and
 * nothing at all on a black-and-white print.
 */
export function StatusBadge({ tone = 'muted', children }: { tone?: Tone; children: string }) {
  return <span className={`shrink-0 rounded-full px-2 py-0.5 text-xs ${tones[tone]}`}>{children}</span>;
}
