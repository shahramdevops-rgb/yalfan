import { useLayoutEffect, useRef, useState, type KeyboardEvent, type PointerEvent, type ReactNode } from 'react';
import { toPersianDigits } from '../format/digits';
import { t } from '../i18n/fa';
import { GripIcon } from './icons';
import { dropIndex, shiftOf } from './sortable';

interface Drag {
  from: number;
  to: number;
  pointerId: number;
  startY: number;
  offset: number;
  /** The rows' vertical middles when the drag started. */
  middles: number[];
  /** The height of the dragged row plus the gap after it: how far the others move to make room. */
  slot: number;
}

interface SortableListProps<T> {
  items: readonly T[];
  getId: (item: T) => string;
  /** The row's name for screen readers, such as «برش». */
  getName: (item: T) => string;
  /** A row was dropped at a new place, or moved with the arrow keys. */
  onMove: (from: number, to: number) => void;
  /** While true, a drag or a key press does nothing (the last move is still being saved). */
  locked?: boolean;
  renderItem: (item: T) => ReactNode;
}

/**
 * A list whose rows are put in order by dragging them by their handle (decided 1405/09/03: drag,
 * not up and down buttons). Written on Pointer Events, so one code path serves a mouse, a finger
 * on the owner's phone and a pen; the handle stops the page from scrolling under a finger. A
 * keyboard user focuses the handle and presses the up or down arrow. No library: the list is
 * short, and the panel stays light on mobile internet (ADR 0014).
 */
export function SortableList<T>({ items, getId, getName, onMove, locked = false, renderItem }: SortableListProps<T>) {
  const listRef = useRef<HTMLUListElement>(null);
  // The handlers read the drag from a ref, not from the rendered state: a quick flick sends its
  // last move and its release before React has rendered the move, and the release must still
  // land where the finger was (found in the 1.6.2 browser check; the state is for drawing only).
  const dragRef = useRef<Drag | null>(null);
  const [drag, setDragState] = useState<Drag | null>(null);
  const setDrag = (next: Drag | null) => {
    dragRef.current = next;
    setDragState(next);
  };
  const [announcement, setAnnouncement] = useState('');
  // React moves a row's element to its new place, and a moved element loses the focus; the handle
  // of a row moved with the keyboard gets it back after the move.
  const refocus = useRef<string | null>(null);

  useLayoutEffect(() => {
    if (refocus.current === null) {
      return;
    }

    const handle = listRef.current?.querySelector<HTMLElement>(`[data-handle="${CSS.escape(refocus.current)}"]`);
    handle?.focus();
    refocus.current = null;
  }, [items]);

  function start(event: PointerEvent<HTMLButtonElement>, index: number) {
    if (locked || !event.isPrimary || event.button !== 0 || !listRef.current) {
      return;
    }

    const rects = Array.from(listRef.current.children, (row) => row.getBoundingClientRect());
    const own = rects[index];
    if (!own) {
      return;
    }

    const gap = rects.length > 1 ? rects[1]!.top - rects[0]!.bottom : 0;
    event.currentTarget.setPointerCapture(event.pointerId);
    setDrag({
      from: index,
      to: index,
      pointerId: event.pointerId,
      startY: event.clientY,
      offset: 0,
      middles: rects.map((rect) => rect.top + rect.height / 2),
      slot: own.height + gap,
    });
  }

  function move(event: PointerEvent<HTMLButtonElement>) {
    const drag = dragRef.current;
    if (!drag || event.pointerId !== drag.pointerId) {
      return;
    }

    const offset = event.clientY - drag.startY;
    const to = dropIndex(drag.middles, drag.from, drag.middles[drag.from]! + offset);
    setDrag({ ...drag, offset, to });
  }

  function end(event: PointerEvent<HTMLButtonElement>, dropped: boolean) {
    const drag = dragRef.current;
    if (!drag || event.pointerId !== drag.pointerId) {
      return;
    }

    // One render for both, so the rows land at their new places without sliding back first.
    setDrag(null);
    if (dropped && drag.to !== drag.from) {
      announce(drag.from, drag.to);
      onMove(drag.from, drag.to);
    }
  }

  function onKeyDown(event: KeyboardEvent<HTMLButtonElement>, index: number) {
    const to = event.key === 'ArrowUp' ? index - 1 : event.key === 'ArrowDown' ? index + 1 : null;
    if (to === null) {
      return;
    }

    event.preventDefault();
    if (locked || to < 0 || to >= items.length) {
      return;
    }

    refocus.current = getId(items[index]!);
    announce(index, to);
    onMove(index, to);
  }

  function announce(from: number, to: number) {
    setAnnouncement(t('sortable.moved', { name: getName(items[from]!), place: toPersianDigits(String(to + 1)) }));
  }

  return (
    <>
      <ul ref={listRef} className={`divide-y divide-line-soft ${drag ? 'select-none' : ''}`}>
        {items.map((item, index) => {
          const id = getId(item);
          const dragged = drag?.from === index;
          const shift = drag && !dragged ? shiftOf(index, drag.from, drag.to) * drag.slot : 0;
          const offset = dragged ? drag.offset : shift;

          return (
            <li
              key={id}
              style={offset ? { transform: `translateY(${offset}px)` } : undefined}
              className={`relative flex items-center gap-2 bg-surface py-2 ${
                dragged ? 'z-10 rounded-lg shadow-lg ring-1 ring-line' : drag ? 'transition-transform duration-150' : ''
              }`}
            >
              <button
                type="button"
                data-handle={id}
                aria-label={t('sortable.handle', { name: getName(item) })}
                aria-disabled={locked || undefined}
                onPointerDown={(event) => start(event, index)}
                onPointerMove={move}
                onPointerUp={(event) => end(event, true)}
                onPointerCancel={(event) => end(event, false)}
                onKeyDown={(event) => onKeyDown(event, index)}
                className={`shrink-0 touch-none rounded-md p-2 text-faint hover:bg-fill hover:text-body focus-visible:outline-2 focus-visible:outline-brand ${
                  locked ? 'cursor-wait opacity-40' : dragged ? 'cursor-grabbing' : 'cursor-grab'
                }`}
              >
                <GripIcon />
              </button>
              {renderItem(item)}
            </li>
          );
        })}
      </ul>
      <p aria-live="polite" className="sr-only">
        {announcement}
      </p>
    </>
  );
}
