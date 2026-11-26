import { useId, useState, type FormEvent } from 'react';
import { t } from '../i18n/fa';
import { Button } from './Button';
import { FieldError } from './Form';

interface AddNameFormProps {
  /** The field's label, such as «پارچهٔ تازه». */
  label: string;
  /** A quieter line under the field, saying what adding does («… همین سایز حساب می‌شوند»). */
  hint?: string;
  /** Adds the name; answers the message to show when it is refused. */
  onAdd: (name: string) => Promise<string | undefined>;
  /** The longest name the API takes for this list. */
  maxLength?: number;
}

/**
 * One field and its button. The operator usually types several names from the paper book one
 * after another, so the field is emptied as soon as a name is sent and the next one can be typed
 * at once, even while the first is still on its way over a slow line. A refused name comes back
 * into the field with its message, unless something new has been typed there meanwhile.
 */
export function AddNameForm({ label, hint, onAdd, maxLength = 100 }: AddNameFormProps) {
  const id = useId();
  const [name, setName] = useState('');
  const [error, setError] = useState<string>();
  const [sending, setSending] = useState(0);

  async function submit(event: FormEvent) {
    event.preventDefault();
    const sent = name;
    if (sent.trim() === '') {
      return;
    }

    setName('');
    setError(undefined);
    setSending((count) => count + 1);
    const refused = await onAdd(sent);
    setSending((count) => count - 1);

    if (refused) {
      setError(refused);
      setName((typed) => (typed === '' ? sent : typed));
    }
  }

  const describedBy = [hint && `${id}-hint`, error && `${id}-error`].filter(Boolean).join(' ') || undefined;

  return (
    <form onSubmit={(event) => void submit(event)} noValidate className="flex flex-col gap-1.5">
      <div className="flex items-center gap-3">
        <label htmlFor={id} className="text-sm font-medium text-label">
          {label}
        </label>
        {sending > 0 && (
          <span className="flex items-center gap-1.5 text-xs text-muted" role="status">
            <span
              className="size-3 animate-spin rounded-full border-2 border-current border-e-transparent"
              aria-hidden
            />
            {t('list.sending')}
          </span>
        )}
      </div>
      <div className="flex gap-2">
        <input
          id={id}
          value={name}
          onChange={(event) => setName(event.target.value)}
          aria-invalid={error ? true : undefined}
          aria-describedby={describedBy}
          maxLength={maxLength}
          autoComplete="off"
          className={`h-12 min-w-0 flex-1 rounded-lg border bg-surface px-3 text-base outline-none focus:border-brand-line focus:ring-2 focus:ring-brand-glow sm:max-w-sm ${
            error ? 'border-danger-line' : 'border-line-strong'
          }`}
        />
        <Button type="submit" disabled={name.trim() === ''}>
          {t('list.add')}
        </Button>
      </div>
      {hint && (
        <p id={`${id}-hint`} className="text-sm text-muted">
          {hint}
        </p>
      )}
      {error && <FieldError id={`${id}-error`}>{error}</FieldError>}
    </form>
  );
}
