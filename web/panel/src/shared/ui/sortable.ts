// The arithmetic of dragging a row to a new place in a list, kept apart from the component so it
// is tested without a browser.

/** A new list with the item at `from` taken out and put back at `to`. */
export function moveItem<T>(items: readonly T[], from: number, to: number): T[] {
  if (from < 0 || from >= items.length || to < 0 || to >= items.length) {
    throw new RangeError(`Cannot move from ${from} to ${to} in a list of ${items.length}.`);
  }

  const moved = [...items];
  const [item] = moved.splice(from, 1);
  moved.splice(to, 0, item as T);
  return moved;
}

/**
 * Where the dragged row lands: the number of other rows whose middle is above the dragged row's
 * middle. `middles` are the rows' vertical middles measured when the drag started, in list order;
 * `draggedMiddle` is the dragged row's middle now.
 */
export function dropIndex(middles: readonly number[], from: number, draggedMiddle: number): number {
  return middles.reduce((count, middle, index) => (index !== from && middle < draggedMiddle ? count + 1 : count), 0);
}

/**
 * How far a row that is not dragged moves to make room, as a multiple of the dragged row's slot:
 * -1 (up one slot), 1 (down one slot) or 0.
 */
export function shiftOf(index: number, from: number, to: number): -1 | 0 | 1 {
  if (from < to && index > from && index <= to) {
    return -1;
  }

  if (to < from && index >= to && index < from) {
    return 1;
  }

  return 0;
}
