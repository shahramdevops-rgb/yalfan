import type { FormEvent, ReactNode } from 'react';
import { EnterMovesOn, moveOnEnter, submitThenFocusError } from './formKeys';

/** Room between the parts of a card: tighter for a list, wider for a long form. */
const gaps = { 4: 'gap-4', 5: 'gap-5', 6: 'gap-6', 8: 'gap-8' } as const;
type Gap = keyof typeof gaps;

const card = 'flex flex-col rounded-2xl bg-surface p-4 shadow-sm ring-1 ring-line sm:p-6';

/** A white panel holding one part of a page, such as a model's sizes or its rates. */
export function Card({ children, labelledBy, gap = 5 }: { children: ReactNode; labelledBy?: string; gap?: Gap }) {
  return (
    <section aria-labelledby={labelledBy} className={`${card} ${gaps[gap]}`}>
      {children}
    </section>
  );
}

interface FormCardProps {
  children: ReactNode;
  /** Saves; answer its promise, so a refusal can put the focus on the wrong field. */
  onSubmit: (event: FormEvent<HTMLFormElement>) => void | Promise<void>;
  labelledBy?: string;
  gap?: Gap;
}

/**
 * A whole page's form in a card: a new model, a new contractor. The panel checks the fields
 * itself and says what is wrong in Persian, so the browser's own checks are off; a refusal puts
 * the focus on the first wrong field, and Enter moves from field to field (ADR 0031).
 */
export function FormCard({ children, onSubmit, labelledBy, gap = 5 }: FormCardProps) {
  return (
    <form
      onSubmit={(event) => submitThenFocusError(event.currentTarget, onSubmit(event))}
      onKeyDown={moveOnEnter}
      noValidate
      aria-labelledby={labelledBy}
      className={`${card} ${gaps[gap]}`}
    >
      <EnterMovesOn value>{children}</EnterMovesOn>
    </form>
  );
}

/** The title of a card, with a sentence under it saying what the card is for. */
export function CardHeader({ id, title, hint }: { id?: string; title: ReactNode; hint?: string }) {
  return (
    <div>
      <h2 id={id} className="text-lg font-bold">
        {title}
      </h2>
      {hint && <p className="mt-1 text-sm text-muted">{hint}</p>}
    </div>
  );
}
