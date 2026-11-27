import { useSyncExternalStore } from 'react';
import { CheckIcon } from './icons';
import { toasts } from './toasts';

/**
 * Where the panel's short messages appear: at the bottom, in the middle, above the page (ADR 0030).
 * The region is always there, empty, because a screen reader announces only what is added to a
 * region that already exists. Drawn once, beside the router, so a message outlives the page that
 * showed it.
 */
export function Toaster() {
  const shown = useSyncExternalStore(toasts.subscribe, toasts.getSnapshot);

  return (
    <div
      role="status"
      // Only the new message is read, not every one still on screen.
      aria-atomic={false}
      className="pointer-events-none fixed inset-x-0 bottom-4 z-50 flex flex-col items-center gap-2 px-4"
    >
      {shown.map((toast) => (
        <p
          key={toast.id}
          onPointerEnter={() => toasts.pause(toast.id)}
          onPointerLeave={toasts.resume}
          className="pointer-events-auto flex max-w-md items-center gap-2 rounded-lg bg-surface px-4 py-3 text-sm text-ink shadow-lg ring-1 ring-line"
        >
          <CheckIcon className="size-4 shrink-0 text-success" />
          {toast.message}
        </p>
      ))}
    </div>
  );
}
