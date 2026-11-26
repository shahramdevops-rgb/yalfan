import { CheckIcon } from './icons';

interface ChipProps {
  label: string;
  on: boolean;
  onToggle: () => void;
  /** Shown ticked and cannot be changed, with `title` saying why. */
  locked?: boolean;
  /** Cannot be pressed for now, such as while the last change is being saved. */
  disabled?: boolean;
  title?: string;
  /** A quieter word after the label, such as «غیرفعال». */
  note?: string;
}

/**
 * A choice that is ticked by pressing it: a fabric, a colour, a stage. Large rounded buttons that
 * wrap to the next line, not checkboxes in a table, so a finger hits them and a phone shows them all.
 */
export function Chip({ label, on, onToggle, locked = false, disabled = false, title, note }: ChipProps) {
  return (
    <button
      type="button"
      aria-pressed={on}
      disabled={locked || disabled}
      title={title}
      onClick={onToggle}
      className={`inline-flex h-10 items-center gap-1.5 rounded-full px-3.5 text-sm ring-1 transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand ${
        on
          ? 'bg-brand-soft font-medium text-brand-strong ring-brand-line'
          : 'bg-surface text-label ring-line-strong hover:bg-subtle'
      } ${locked ? 'cursor-default opacity-75' : ''} ${disabled && !locked ? 'cursor-wait' : ''}`}
    >
      {on && <CheckIcon />}
      {label}
      {note && <span className="text-xs font-normal text-muted">{note}</span>}
    </button>
  );
}
