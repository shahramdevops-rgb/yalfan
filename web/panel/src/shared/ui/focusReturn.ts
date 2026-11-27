import { useLayoutEffect, useRef, type RefObject } from 'react';

// The element last pressed with a finger or the mouse, and not yet followed by a key. Safari on an
// iPhone does not focus a button that is tapped, so the focus says nothing about what opened an
// overlay there; this does (ADR 0031).
let pressed: HTMLElement | null = null;

if (typeof document !== 'undefined') {
  document.addEventListener(
    'pointerdown',
    (event) => {
      pressed =
        event.target instanceof Element
          ? event.target.closest<HTMLElement>(
              'button, a[href], input, select, textarea, [tabindex]:not([tabindex="-1"])',
            )
          : null;
    },
    true,
  );
  // A key opens things from where the focus is; a press made before it no longer counts.
  document.addEventListener('keydown', () => (pressed = null), true);
}

/**
 * What the person used to open something: the element just pressed, else the one with the focus.
 * The press comes first because on an iPhone the focus stays wherever it was (often the body, or a
 * field typed in before) when a button is tapped.
 */
export function openedBy(): HTMLElement | null {
  if (pressed?.isConnected) {
    return pressed;
  }

  const active = document.activeElement;
  return active instanceof HTMLElement && active !== document.body ? active : null;
}

/**
 * When a small form opened inside a page closes (saved, cancelled, Escape), the focus goes to
 * `target`, the line that opened it, instead of falling back to the start of the page where a
 * keyboard loses its place (ADR 0031). Only when the focus was inside the form or fell with it:
 * a click elsewhere that closed it keeps the focus it gave.
 *
 * The target is read after the page has drawn itself without the form, because the line that
 * opened it may be drawn anew (a worker's start date shows either the date or its form).
 */
export function useReturnFocus(target: RefObject<HTMLElement | null>): RefObject<HTMLFormElement | null> {
  const box = useRef<HTMLFormElement>(null);

  useLayoutEffect(() => {
    const form = box.current;
    return () => {
      const active = document.activeElement;
      const fellWithIt = active === null || active === document.body || (form?.contains(active) ?? false);
      if (!fellWithIt) {
        return;
      }

      focusOnceDrawn(target);
    };
  }, [target]);

  return box;
}

/**
 * Focuses what `target` points to once the page has drawn itself, read then and not before: the
 * line may be a new element by that time. Unless something else took the focus meanwhile, such as
 * the next form opening with its first field.
 */
function focusOnceDrawn(target: RefObject<HTMLElement | null>) {
  queueMicrotask(() => {
    const now = document.activeElement;
    if (now === null || now === document.body) {
      target.current?.focus({ preventScroll: true });
    }
  });
}
