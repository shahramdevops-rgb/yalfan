/** A short message at the bottom of the screen that goes away by itself (ADR 0030). */
export interface Toast {
  id: number;
  message: string;
}

/** Long enough to read a sentence twice. */
export const toastShownFor = 5000;

/** More than this at once is noise: the oldest goes first. */
export const toastsAtOnce = 3;

export interface ToastStore {
  /** Shows a message. The same message again, while it is still shown, shows it longer, not twice. */
  show: (message: string) => void;
  dismiss: (id: number) => void;
  /**
   * Stops the clock while the pointer is on this message, so none leaves while it is read. Ends
   * by itself if that message goes (pushed out by newer ones), since its pointer can no longer
   * leave it.
   */
  pause: (id: number) => void;
  /** Starts each message's time again once the pointer leaves. */
  resume: () => void;
  subscribe: (listener: () => void) => () => void;
  getSnapshot: () => readonly Toast[];
}

/** The messages on screen and their timers; one store for the panel, a fresh one per test. */
export function createToastStore(): ToastStore {
  let toasts: readonly Toast[] = [];
  let nextId = 1;
  /** The message under the pointer, while there is one: every clock waits for it. */
  let hovered: number | null = null;
  const timers = new Map<number, ReturnType<typeof setTimeout>>();
  const listeners = new Set<() => void>();

  function publish(next: readonly Toast[]) {
    toasts = next;
    listeners.forEach((listener) => listener());
  }

  function stopTimer(id: number) {
    clearTimeout(timers.get(id));
    timers.delete(id);
  }

  function startTimer(id: number) {
    stopTimer(id);
    if (hovered === null) {
      timers.set(
        id,
        setTimeout(() => dismiss(id), toastShownFor),
      );
    }
  }

  function dismiss(id: number) {
    stopTimer(id);
    publish(toasts.filter((toast) => toast.id !== id));
    if (id === hovered) {
      resume();
    }
  }

  function show(message: string) {
    const last = toasts.at(-1);
    if (last?.message === message) {
      startTimer(last.id);
      return;
    }

    const toast = { id: nextId++, message };
    const kept = toasts.slice(-(toastsAtOnce - 1));
    const dropped = toasts.filter((old) => !kept.includes(old));
    dropped.forEach((old) => stopTimer(old.id));
    publish([...kept, toast]);
    if (dropped.some((old) => old.id === hovered)) {
      resume();
    } else {
      startTimer(toast.id);
    }
  }

  function pause(id: number) {
    hovered = id;
    [...timers.keys()].forEach(stopTimer);
  }

  function resume() {
    hovered = null;
    toasts.forEach((toast) => startTimer(toast.id));
  }

  return {
    show,
    dismiss,
    pause,
    resume,
    subscribe: (listener) => {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
    getSnapshot: () => toasts,
  };
}

/** The panel's one store, read by `Toaster`. */
export const toasts = createToastStore();

/**
 * Says that something was done which the page does not show by itself («کپی شد»). Never a refusal:
 * that stays beside its field until it is put right (docs/design/UI.md).
 */
export function toast(message: string) {
  toasts.show(message);
}
