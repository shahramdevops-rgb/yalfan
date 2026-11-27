import {
  useId,
  useLayoutEffect,
  useRef,
  type KeyboardEvent,
  type PointerEvent,
  type ReactNode,
  type RefObject,
  type SyntheticEvent,
} from 'react';
import { t } from '../i18n/fa';
import { Button } from './Button';
import { openedBy } from './focusReturn';

interface ModalProps {
  /** Called when the person closes it: Escape, a tap outside it, or one of its own buttons. */
  onClose: () => void;
  /** The id of its visible title; or `label`, when it has none. */
  labelledBy?: string;
  label?: string;
  describedBy?: string;
  /** `alertdialog` for a question that interrupts the work (`ConfirmDialog`). */
  role?: 'dialog' | 'alertdialog';
  /** What takes the focus when it opens; the dialog itself when nothing is named. */
  initialFocus?: RefObject<HTMLElement | null>;
  className: string;
  children: ReactNode;
}

/**
 * The base of every overlay: the browser's own modal `<dialog>` (ADR 0030). The browser puts it
 * above everything and makes the rest of the page inert, so neither Tab nor a screen reader
 * reaches what is behind it; Escape closes it. It exists only while it is open: drawing it opens
 * it, and the page closes it by no longer drawing it, so the page's own state is the one truth.
 * When it closes, the focus goes back to what opened it: the button pressed, also on an iPhone,
 * where Safari does not focus a tapped button (ADR 0031).
 */
export function Modal({
  onClose,
  labelledBy,
  label,
  describedBy,
  role = 'dialog',
  initialFocus,
  className,
  children,
}: ModalProps) {
  const ref = useRef<HTMLDialogElement>(null);
  // Whether the press began and ended outside the box: only then is it a tap outside it.
  const pressedOutside = useRef(false);
  const releasedOutside = useRef(false);

  useLayoutEffect(() => {
    const dialog = ref.current!;
    const opener = openedBy();
    dialog.showModal();
    (initialFocus?.current ?? dialog).focus();

    return () => {
      if (dialog.open) {
        dialog.close();
      }

      // Back to what opened it, as the browser does when it closes a dialog itself.
      if (opener?.isConnected) {
        opener.focus({ preventScroll: true });
      }
    };
  }, [initialFocus]);

  function cancel(event: SyntheticEvent) {
    // The page closes it, by no longer drawing it, rather than the browser behind its back.
    event.preventDefault();
    onClose();
  }

  function onKeyDown(event: KeyboardEvent) {
    // Escape belongs to the dialog: should a page draw it inside a form that closes on Escape
    // (as an IBAN's small form does), that form must not close too. A guard: the IBAN's question
    // is drawn beside its form.
    if (event.key === 'Escape') {
      event.stopPropagation();
    }
  }

  function onPointerDown(event: PointerEvent) {
    pressedOutside.current = event.target === event.currentTarget;
  }

  function onPointerUp(event: PointerEvent) {
    releasedOutside.current = event.target === event.currentTarget;
  }

  function onClick(event: SyntheticEvent) {
    // The box fills the dialog edge to edge, so only the dimmed page around it is the dialog
    // itself. A press that begins or ends inside the box (a selection dragged out of it, or a
    // press slid onto a button) is answered with a click on the dialog too, so both ends count.
    if (pressedOutside.current && releasedOutside.current && event.target === event.currentTarget) {
      onClose();
    }

    pressedOutside.current = false;
    releasedOutside.current = false;
  }

  return (
    <dialog
      ref={ref}
      role={role}
      aria-labelledby={labelledBy}
      aria-label={label}
      aria-describedby={describedBy}
      tabIndex={-1}
      onCancel={cancel}
      // Should the browser close it anyway (it may, on a second Escape), the page follows. The
      // check skips the late «close» of a dialog already open again (React's StrictMode opens,
      // closes and reopens it once while developing).
      onClose={() => {
        if (!ref.current?.open) {
          onClose();
        }
      }}
      onKeyDown={onKeyDown}
      onPointerDown={onPointerDown}
      onPointerUp={onPointerUp}
      onClick={onClick}
      className={`p-0 text-ink outline-none backdrop:bg-ink/40 ${className}`}
    >
      {children}
    </dialog>
  );
}

interface ConfirmDialogProps {
  /** The question: «شبا پاک شود؟». */
  title: string;
  /** What will happen, and what tells which record it is (the IBAN's last four digits). */
  children: ReactNode;
  /** The button that does it, named by what it does: «پاک کردن شبا». */
  confirmLabel: string;
  onConfirm: () => void;
  onCancel: () => void;
}

/**
 * The question before a step that the person cannot take back from the panel himself, such as
 * removing an IBAN (ADR 0030). It is only the question: on the confirm button the page closes it
 * and runs the step with its own busy state and refusal. «انصراف» has the focus, so an Enter
 * pressed from habit takes nothing away; Escape and a tap outside it cancel too.
 */
export function ConfirmDialog({ title, children, confirmLabel, onConfirm, onCancel }: ConfirmDialogProps) {
  const titleId = useId();
  const bodyId = useId();
  const cancelButton = useRef<HTMLButtonElement>(null);

  return (
    <Modal
      role="alertdialog"
      labelledBy={titleId}
      describedBy={bodyId}
      initialFocus={cancelButton}
      onClose={onCancel}
      className="m-auto w-[calc(100%-2rem)] max-w-md rounded-2xl bg-surface shadow-xl ring-1 ring-line"
    >
      <div className="flex flex-col gap-4 p-6">
        <h2 id={titleId} className="text-lg font-bold">
          {title}
        </h2>
        <div id={bodyId} className="flex flex-col gap-2 text-base text-body">
          {children}
        </div>
        <div className="flex flex-wrap gap-3 pt-2">
          <Button variant="danger" onClick={onConfirm}>
            {confirmLabel}
          </Button>
          <Button ref={cancelButton} variant="secondary" onClick={onCancel}>
            {t('dialog.cancel')}
          </Button>
        </div>
      </div>
    </Modal>
  );
}

/**
 * A panel that slides over the page from its inline start (the right, in Persian), such as the
 * sidebar on a phone. Escape, a tap on the dimmed page or the page's own close button closes it.
 */
export function Drawer({ label, onClose, children }: { label: string; onClose: () => void; children: ReactNode }) {
  return (
    <Modal
      label={label}
      onClose={onClose}
      className="start-0 end-auto m-0 h-dvh max-h-none w-72 max-w-[85%] bg-surface shadow-xl"
    >
      {children}
    </Modal>
  );
}
