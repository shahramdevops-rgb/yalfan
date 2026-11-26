import type { ReactNode } from 'react';

type Tone = 'warning' | 'danger';

const tones: Record<Tone, string> = {
  warning: 'bg-warning-soft text-warning-strong',
  danger: 'bg-danger-soft text-danger-strong',
};

/**
 * A sentence in a soft box: a warning to read before going on («این مدل غیرفعال است…»), or why
 * a save was refused. A refusal is announced by screen readers as soon as it appears.
 */
export function Notice({ tone, children }: { tone: Tone; children: ReactNode }) {
  return (
    <p role={tone === 'danger' ? 'alert' : undefined} className={`rounded-lg px-3 py-2 text-sm ${tones[tone]}`}>
      {children}
    </p>
  );
}
