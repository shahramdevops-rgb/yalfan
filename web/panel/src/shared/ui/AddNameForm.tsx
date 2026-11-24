import { useState, type FormEvent } from 'react';
import { t } from '../i18n/fa';
import { Button } from './Button';

interface AddNameFormProps {
  /** The field's label, such as «پارچهٔ تازه». */
  label: string;
  /** Adds the name; answers the message to show when it is refused. */
  onAdd: (name: string) => Promise<string | undefined>;
}

/**
 * One field and its button. The operator usually types several names from the paper book one
 * after another, so the field is emptied as soon as a name is sent and the next one can be typed
 * at once, even while the first is still on its way over a slow line. A refused name comes back
 * into the field with its message, unless something new has been typed there meanwhile.
 */
export function AddNameForm({ label, onAdd }: AddNameFormProps) {
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

  return (
    <form onSubmit={(event) => void submit(event)} noValidate className="flex flex-col gap-1.5">
      <div className="flex items-center gap-3">
        <label htmlFor="add-name" className="text-sm font-medium text-stone-700">
          {label}
        </label>
        {sending > 0 && (
          <span className="flex items-center gap-1.5 text-xs text-stone-500" role="status">
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
          id="add-name"
          value={name}
          onChange={(event) => setName(event.target.value)}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? 'add-name-error' : undefined}
          maxLength={100}
          autoComplete="off"
          className={`h-12 min-w-0 flex-1 rounded-lg border bg-white px-3 text-base outline-none focus:border-brand-600 focus:ring-2 focus:ring-brand-100 sm:max-w-sm ${
            error ? 'border-red-500' : 'border-stone-300'
          }`}
        />
        <Button type="submit" disabled={name.trim() === ''}>
          {t('list.add')}
        </Button>
      </div>
      {error && (
        <p id="add-name-error" role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
    </form>
  );
}
