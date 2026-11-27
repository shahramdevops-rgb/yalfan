import { createContext, type KeyboardEvent } from 'react';

/**
 * Set by the panel's forms (`FormCard`, `InsetForm`), where Enter moves to the next field; a
 * field inside one has its phone keyboard's Enter key say «بعدی» (ADR 0031).
 */
export const EnterMovesOn = createContext(false);

/** The one-line fields where Enter moves on; a checkbox, a file or a button keeps its own Enter. */
const lineTypes = new Set(['text', 'tel', 'email', 'number', 'search', 'url', 'password']);

const isChip = (element: Element) => element instanceof HTMLButtonElement && element.hasAttribute('aria-pressed');

/** What Enter moves to and from: a field, a list, a chip or a switch, but not a plain button. */
function isField(element: Element): boolean {
  if (element instanceof HTMLInputElement) {
    return lineTypes.has(element.type);
  }

  return (
    element instanceof HTMLSelectElement ||
    element instanceof HTMLTextAreaElement ||
    isChip(element) ||
    element.getAttribute('role') === 'switch'
  );
}

/** Reachable by Tab right now: enabled, drawn, and not inside something inert. */
function reachable(element: HTMLElement): boolean {
  return (
    !element.matches(':disabled') &&
    element.tabIndex >= 0 &&
    element.getClientRects().length > 0 &&
    element.closest('[inert]') === null
  );
}

/**
 * Where Enter goes from `from`: the next field of the form, a row of chips counting as one; after
 * the last field, the form's main button; null when that button cannot be pressed yet.
 */
function nextStop(form: HTMLFormElement, from: HTMLElement): HTMLElement | null {
  const stops = Array.from(form.querySelectorAll<HTMLElement>('input, select, textarea, button')).filter(reachable);
  const row = isChip(from) ? from.parentElement : null;
  const after = stops.slice(stops.indexOf(from) + 1).filter((stop) => !row?.contains(stop));

  return (
    after.find(isField) ?? after.find((stop) => stop instanceof HTMLButtonElement && stop.type === 'submit') ?? null
  );
}

/**
 * Enter goes forward (ADR 0031): from a field to the next one, from the last to the form's main
 * button, which Enter then presses. So no form is sent half filled by an Enter pressed from habit,
 * as Iranian office software and the numpad's Enter have taught the operator. A row of chips is
 * one field: Enter leaves it and Space ticks a chip, as Space flips a switch. A notes box keeps
 * Enter for a new line, and a field that handles Enter itself (adding a size) says so with
 * `preventDefault`.
 */
export function moveOnEnter(event: KeyboardEvent<HTMLFormElement>) {
  const { key, shiftKey, ctrlKey, altKey, metaKey, nativeEvent } = event;
  if (
    key !== 'Enter' ||
    event.defaultPrevented ||
    shiftKey ||
    ctrlKey ||
    altKey ||
    metaKey ||
    nativeEvent.isComposing
  ) {
    return;
  }

  const from = event.target;
  if (!(from instanceof HTMLElement) || from instanceof HTMLTextAreaElement || !isField(from)) {
    return;
  }

  event.preventDefault();
  nextStop(event.currentTarget, from)?.focus();
}

/**
 * After a save was refused, the focus goes to the first field that says what is wrong, so a
 * keyboard does not have to look for it (ADR 0031): a field marked wrong, or the first control of
 * a group (chips, a date) with a `FieldError` in it. A refusal of the whole form (`Notice`) moves
 * nothing.
 */
export function focusFirstError(form: HTMLFormElement) {
  if (!form.isConnected) {
    return;
  }

  const first = form.querySelector<HTMLElement>('[aria-invalid="true"], [data-field-error]');
  if (!first) {
    return;
  }

  const target = first.hasAttribute('data-field-error')
    ? Array.from(
        first.closest('fieldset, [role="group"]')?.querySelectorAll<HTMLElement>('input, select, textarea, button') ??
          [],
      ).find(reachable)
    : first;
  target?.focus();
}

/**
 * Runs a form's save, and once it has answered and the page has drawn the answer, moves the focus
 * to the first wrong field, if any. A form that saved and closed, or left the page, is not there.
 */
export function submitThenFocusError(form: HTMLFormElement, result: void | Promise<void>) {
  const settled = () => requestAnimationFrame(() => focusFirstError(form));
  void Promise.resolve(result).then(settled, settled);
}
