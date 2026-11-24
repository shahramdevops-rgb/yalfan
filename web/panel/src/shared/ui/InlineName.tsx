import { useRef, useState, type KeyboardEvent } from 'react';
import { t } from '../i18n/fa';

interface InlineNameProps {
  value: string;
  /** Saves the new text; answers the message to show when it is refused. */
  onSave: (text: string) => Promise<string | undefined>;
  muted?: boolean;
  /** A longer text, such as a model's description: a few lines, and Enter starts a new line. */
  multiline?: boolean;
  /** Emptying the field saves an empty text (a description), instead of putting the old one back. */
  allowEmpty?: boolean;
  /** Shown in place of an empty text, saying what a tap does. */
  placeholder?: string;
  maxLength?: number;
  /** Larger text, for the name at the top of a page. */
  large?: boolean;
  /**
   * What the text is, for screen readers, when it is not a name («توضیح مدل»). The field is named
   * by it, and the button is read by its own text.
   */
  label?: string;
}

/**
 * A text that is edited where it stands: a tap turns it into a field, Enter or leaving the field
 * saves, Escape puts the old text back. No rename button and no dialog, because the operator
 * fixes a name by touching it (decided 1405/09/03: no needless buttons).
 */
export function InlineName({
  value,
  onSave,
  muted = false,
  multiline = false,
  allowEmpty = false,
  placeholder,
  maxLength = 100,
  large = false,
  label,
}: InlineNameProps) {
  const [draft, setDraft] = useState<string | null>(null);
  const [error, setError] = useState<string>();
  const [busy, setBusy] = useState(false);
  // Escape removes the field, and a browser may report that as leaving it; that must not save.
  const cancelled = useRef(false);

  function cancel() {
    setDraft(null);
    setError(undefined);
  }

  async function save(text: string) {
    // An unchanged text changes nothing, and neither does an emptied name: the old one comes back.
    if (text.trim() === value.trim() || (text.trim() === '' && !allowEmpty)) {
      cancel();
      return;
    }

    setBusy(true);
    const refused = await onSave(text.trim());
    setBusy(false);

    if (refused) {
      setError(refused);
    } else {
      cancel();
    }
  }

  function onKeyDown(event: KeyboardEvent<HTMLInputElement | HTMLTextAreaElement>) {
    if (event.key === 'Enter' && !multiline) {
      event.preventDefault();
      event.currentTarget.blur();
    } else if (event.key === 'Escape') {
      event.preventDefault();
      cancelled.current = true;
      cancel();
    }
  }

  const size = large ? 'text-xl font-bold' : 'text-base';

  if (draft === null) {
    const empty = value.trim() === '';
    return (
      <button
        type="button"
        onClick={() => {
          cancelled.current = false;
          setDraft(value);
        }}
        title={t('list.renameHint')}
        aria-label={label ? undefined : empty ? placeholder : t('list.renameButton', { name: value })}
        className={`-mx-2 min-w-0 rounded-md px-2 py-1 text-start hover:bg-stone-100 focus-visible:outline-2 focus-visible:outline-brand-700 ${size} ${
          multiline ? 'whitespace-pre-line' : 'truncate'
        } ${empty ? 'text-stone-400' : muted ? 'text-stone-400' : 'text-stone-900'}`}
      >
        {empty ? placeholder : value}
      </button>
    );
  }

  const field = {
    value: draft,
    onChange: (event: { target: { value: string } }) => setDraft(event.target.value),
    onKeyDown,
    onBlur: () => {
      if (!cancelled.current) {
        void save(draft);
      }
    },
    'aria-label': label ?? t('list.renameLabel', { name: value }),
    'aria-invalid': error ? true : undefined,
    readOnly: busy,
    maxLength,
    autoFocus: true,
    className: `w-full rounded-lg border bg-white px-3 outline-none focus:border-brand-600 focus:ring-2 focus:ring-brand-100 ${size} ${
      error ? 'border-red-500' : 'border-stone-300'
    } ${multiline ? 'min-h-24 max-w-xl py-2' : 'h-10 max-w-sm'}`,
  };

  return (
    <div className="flex min-w-0 flex-1 flex-col gap-1">
      {multiline ? <textarea {...field} rows={3} /> : <input {...field} />}
      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  );
}
