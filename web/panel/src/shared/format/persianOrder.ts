// The database's default collation has no Persian alphabetical order, so the catalog lists come
// in the order they were added and the panel sorts them here (ADR 0016).

const persian = new Intl.Collator('fa');

/** Sorted by name in Persian alphabetical order («آبی، برزنت، پارچه، چرم»), as a new list. */
export function byPersianName<T extends { name: string }>(items: readonly T[]): T[] {
  return [...items].sort((a, b) => persian.compare(a.name, b.name));
}
