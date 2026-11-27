import { useId, useState, type KeyboardEvent } from 'react';
import { normalizePersianName, samePersianName } from '../../shared/format/persianName';
import { t } from '../../shared/i18n/fa';
import { Button } from '../../shared/ui/Button';
import { FieldError } from '../../shared/ui/Form';
import { CloseIcon } from '../../shared/ui/icons';

interface SizeNamesFieldProps {
  sizes: readonly string[];
  onChange: (sizes: string[]) => void;
  error?: string;
}

/** The longest size name the API takes (ProductSize.NameMaxLength). */
const maxLength = 30;

/**
 * The sizes of a new model, typed one after another in their order; the count is how many were
 * typed, so there is no separate field for it (decided 1405/09/03). A typed size is a small tag
 * that can be taken out again before the model is saved.
 */
export function SizeNamesField({ sizes, onChange, error }: SizeNamesFieldProps) {
  const id = useId();
  const [draft, setDraft] = useState('');
  const [duplicate, setDuplicate] = useState(false);

  function add() {
    const name = draft.trim();
    if (name === '') {
      return;
    }

    if (sizes.some((size) => samePersianName(size, name))) {
      setDuplicate(true);
      return;
    }

    onChange([...sizes, normalizePersianName(name)]);
    setDraft('');
    setDuplicate(false);
  }

  // Enter adds the size, and the field is ready for the next one. In an empty field it moves on
  // to the next field of the form, as Enter does everywhere else (ADR 0031).
  function onKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'Enter' && draft.trim() !== '') {
      event.preventDefault();
      add();
    }
  }

  const message = duplicate ? t('newModel.sizeDuplicate') : error;

  return (
    <div className="flex flex-col gap-2">
      {sizes.length > 0 && (
        <ol className="flex flex-wrap gap-2">
          {sizes.map((size) => (
            <li
              key={size}
              className="inline-flex h-10 items-center gap-1 rounded-full bg-fill ps-3.5 pe-0.5 text-sm text-ink-soft"
            >
              {size}
              <button
                type="button"
                onClick={() => onChange(sizes.filter((other) => other !== size))}
                aria-label={t('newModel.removeSize', { size })}
                // As large as the tag allows (36px), so a finger on a phone hits it, not the next one.
                className="flex size-9 items-center justify-center rounded-full text-muted hover:bg-fill-strong hover:text-ink-soft"
              >
                <CloseIcon className="size-4" />
              </button>
            </li>
          ))}
        </ol>
      )}
      <label htmlFor={id} className="text-sm font-medium text-label">
        {sizes.length === 0 ? t('newModel.firstSize') : t('newModel.nextSize')}
      </label>
      <div className="flex gap-2">
        <input
          id={id}
          value={draft}
          onChange={(event) => {
            setDraft(event.target.value);
            setDuplicate(false);
          }}
          onKeyDown={onKeyDown}
          maxLength={maxLength}
          autoComplete="off"
          aria-invalid={message ? true : undefined}
          aria-describedby={message ? `${id}-error` : undefined}
          className={`h-12 min-w-0 flex-1 rounded-lg border bg-surface px-3 text-base outline-none focus:border-brand-line focus:ring-2 focus:ring-brand-glow sm:max-w-xs ${
            message ? 'border-danger-line' : 'border-line-strong'
          }`}
        />
        <Button variant="secondary" onClick={add} disabled={draft.trim() === ''}>
          {t('list.add')}
        </Button>
      </div>
      {message && <FieldError id={`${id}-error`}>{message}</FieldError>}
    </div>
  );
}
