interface SwitchProps {
  checked: boolean;
  onChange: (checked: boolean) => void;
  /** What the switch turns on and off, for screen readers: «فعال بودن برزنت». */
  label: string;
  disabled?: boolean;
}

/**
 * An on/off switch: one tap, and its position says the state, so no "activate" and "deactivate"
 * buttons are needed. The knob sits at the inline end when on, which is the left in a
 * right-to-left page, as the phone's own Persian settings show it.
 */
export function Switch({ checked, onChange, label, disabled = false }: SwitchProps) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label}
      disabled={disabled}
      onClick={() => onChange(!checked)}
      className={`inline-flex h-7 w-12 shrink-0 items-center rounded-full p-0.5 transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand disabled:cursor-not-allowed disabled:opacity-60 ${
        checked ? 'justify-end bg-brand' : 'justify-start bg-fill-off'
      }`}
    >
      <span className="size-6 rounded-full bg-surface shadow" aria-hidden />
    </button>
  );
}
