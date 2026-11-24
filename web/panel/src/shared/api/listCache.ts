/** The cached list with `item` put in place of the row with its id, or added at the end. */
export function upsert<T extends { id: string }>(items: readonly T[] | undefined, item: T): T[] {
  const list = items ?? [];
  return list.some((row) => row.id === item.id)
    ? list.map((row) => (row.id === item.id ? item : row))
    : [...list, item];
}
