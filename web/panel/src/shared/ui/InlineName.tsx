import { useRef, useState, type KeyboardEvent } from 'react';
import { t } from '../i18n/fa';

interface InlineNameProps {
  value: string;
  /** Saves the new name; answers the message to show when it is refused. */
  onSave: (name: string) => Promise<string | undefined>;
  muted?: boolean;
}

/**
 * A name that is edited where it stands: a tap turns it into a field, Enter or leaving the field
 * saves, Escape puts the old name back. No rename button and no dialog, because the operator
 * fixes a name by touching it (decided 1405/09/03: no needless buttons).
 */
export function InlineName({ value, onSave, muted = false }: InlineNameProps) {
  const [draft, setDraft] = useState<string | null>(null);
  const [error, setError] = useState<string>();
  const [busy, setBusy] = useState(false);
  // Escape removes the field, and a browser may report that as leaving it; that must not save.
  const cancelled = useRef(false);

  function cancel() {
    setDraft(null);
    setError(undefined);
  }

  async function save(name: string) {
    // An emptied field or an unchanged name changes nothing; the old name comes back.
    if (name.trim() === '' || name.trim() === value) {
      cancel();
      return;
    }

    setBusy(true);
    const refused = await onSave(name);
    setBusy(false);

    if (refused) {
      setError(refused);
    } else {
      cancel();
    }
  }

  function onKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'Enter') {
      event.preventDefault();
      event.currentTarget.blur();
    } else if (event.key === 'Escape') {
      event.preventDefault();
      cancelled.current = true;
      cancel();
    }
  }

  if (draft === null) {
    return (
      <button
        type="button"
        onClick={() => {
          cancelled.current = false;
          setDraft(value);
        }}
        title={t('list.renameHint')}
        aria-label={t('list.renameButton', { name: value })}
        className={`-mx-2 min-w-0 truncate rounded-md px-2 py-1 text-start text-base hover:bg-stone-100 focus-visible:outline-2 focus-visible:outline-brand-700 ${
          muted ? 'text-stone-400' : 'text-stone-900'
        }`}
      >
        {value}
      </button>
    );
  }

  return (
    <div className="flex min-w-0 flex-1 flex-col gap-1">
      <input
        value={draft}
        onChange={(event) => setDraft(event.target.value)}
        onKeyDown={onKeyDown}
        onBlur={() => {
          if (!cancelled.current) {
            void save(draft);
          }
        }}
        aria-label={t('list.renameLabel', { name: value })}
        aria-invalid={error ? true : undefined}
        readOnly={busy}
        maxLength={100}
        autoFocus
        className={`h-10 w-full max-w-sm rounded-lg border bg-white px-3 text-base outline-none focus:border-brand-600 focus:ring-2 focus:ring-brand-100 ${
          error ? 'border-red-500' : 'border-stone-300'
        }`}
      />
      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  );
}
