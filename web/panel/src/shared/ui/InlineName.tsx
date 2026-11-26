import { useRef, useState, type KeyboardEvent } from 'react';
import { t } from '../i18n/fa';
import { FieldError } from './Form';

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
  /** What is shown while the text is not being edited, when it differs from it (a grouped mobile). */
  display?: string;
  /** A text that reads left to right, such as a phone number, with the phone's number keyboard. */
  numeric?: boolean;
  /** Shown under the field while it is edited, saying what saving will do (a user's new number). */
  hint?: string;
  /**
   * The page's `h1`: the name at the top of a record's own page (a model, a contractor), which
   * has no other title. The button is then read by the name alone, so the page's title is the
   * name and not «تغییر نام …»; its `title` still says a tap changes it. While the name is being
   * edited the field takes the heading's place, with its own label.
   */
  heading?: boolean;
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
  display,
  numeric = false,
  hint,
  heading = false,
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
    const button = (
      <button
        type="button"
        onClick={() => {
          cancelled.current = false;
          setDraft(value);
        }}
        title={t('list.renameHint')}
        dir={numeric ? 'ltr' : undefined}
        aria-label={label || heading ? undefined : empty ? placeholder : t('list.renameButton', { name: value })}
        className={`-mx-2 min-w-0 rounded-md px-2 py-1 text-start hover:bg-fill focus-visible:outline-2 focus-visible:outline-brand ${size} ${
          multiline ? 'whitespace-pre-line' : 'truncate'
        } ${empty ? 'text-faint' : muted ? 'text-faint' : 'text-ink'}`}
      >
        {empty ? placeholder : (display ?? value)}
      </button>
    );
    // A column, so the button still spans the line as it does without the heading around it.
    return heading ? <h1 className="flex min-w-0 flex-col">{button}</h1> : button;
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
    ...(numeric && { dir: 'ltr', inputMode: 'tel' as const, autoComplete: 'off' }),
    className: `w-full rounded-lg border bg-surface px-3 outline-none focus:border-brand-line focus:ring-2 focus:ring-brand-glow ${size} ${
      error ? 'border-danger-line' : 'border-line-strong'
    } ${multiline ? 'min-h-24 max-w-xl py-2' : 'h-10 max-w-sm'}`,
  };

  return (
    <div className="flex min-w-0 flex-1 flex-col gap-1">
      {multiline ? <textarea {...field} rows={3} /> : <input {...field} />}
      {hint && !error && <p className="max-w-sm text-sm text-warning">{hint}</p>}
      {error && <FieldError>{error}</FieldError>}
    </div>
  );
}
