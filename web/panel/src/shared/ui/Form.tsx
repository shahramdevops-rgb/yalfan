import { useId, type FormEvent, type KeyboardEvent, type ReactNode } from 'react';

/**
 * A label over something that is not a single text field: a row of chips, a name edited where it
 * stands, a date. Screen readers read the label as the name of the group.
 */
export function Field({ label, hint, children }: { label: string; hint?: string; children: ReactNode }) {
  const labelId = useId();
  return (
    <div role="group" aria-labelledby={labelId} className="flex flex-col gap-1.5">
      <p id={labelId} className="text-sm font-medium text-label">
        {label}
      </p>
      {hint && <p className="-mt-1 text-sm text-muted">{hint}</p>}
      {children}
    </div>
  );
}

/** What is wrong with one field, under it, announced by screen readers as soon as it appears. */
export function FieldError({ id, children }: { id?: string; children: ReactNode }) {
  return (
    <p id={id} role="alert" className="text-sm text-danger">
      {children}
    </p>
  );
}

/** The short fields of a form, one under the other and no wider than a name or a number needs. */
export function FormFields({ children }: { children: ReactNode }) {
  return <div className="flex flex-col gap-5 sm:max-w-md">{children}</div>;
}

/** A part of a long form with its own title, such as a new model's sizes. */
export function FormSection({ title, children }: { title: string; children: ReactNode }) {
  const titleId = useId();
  return (
    <section aria-labelledby={titleId} className="flex flex-col gap-3">
      <h2 id={titleId} className="text-lg font-bold">
        {title}
      </h2>
      {children}
    </section>
  );
}

/**
 * The end of a form, under a thin line: what saving will do, why it was refused (`Notice`), and
 * the button that saves. Give each button `self-start`, so it keeps its own width.
 */
export function FormActions({ children }: { children: ReactNode }) {
  return <div className="flex flex-col gap-3 border-t border-line-soft pt-5">{children}</div>;
}

interface InsetFormProps {
  children: ReactNode;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  onKeyDown?: (event: KeyboardEvent<HTMLFormElement>) => void;
  labelledBy: string;
}

/**
 * A small form opened inside a card by a tap, such as a new IBAN or a new rate, framed in the
 * workshop's colour so it is seen as open; it closes on «ثبت» or «انصراف».
 */
export function InsetForm({ children, onSubmit, onKeyDown, labelledBy }: InsetFormProps) {
  return (
    <form
      aria-labelledby={labelledBy}
      onSubmit={onSubmit}
      onKeyDown={onKeyDown}
      noValidate
      className="flex flex-col gap-4 rounded-xl p-4 ring-1 ring-brand-edge"
    >
      {children}
    </form>
  );
}
