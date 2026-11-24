// The same Persian name typed on different keyboards: an Arabic one types «ي» and «ك» where a
// Persian one types «ی» and «ک». The API normalizes names the same way before it checks them
// (PersianText.Normalize), so the panel can catch a repeated name before sending it.

/** Trimmed, runs of white space as one space, and the Arabic yeh and kaf as the Persian ones. */
export function normalizePersianName(name: string): string {
  return name.trim().replace(/\s+/g, ' ').replace(/[يى]/g, 'ی').replace(/ك/g, 'ک');
}

/** Whether two names are the same name once normalized. */
export const samePersianName = (a: string, b: string) => normalizePersianName(a) === normalizePersianName(b);
