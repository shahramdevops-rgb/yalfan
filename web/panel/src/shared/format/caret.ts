/**
 * The place in a grouped field's text just after its `count`-th digit (0 is the start), where
 * `separator` is the character the grouping puts between the digits. A field that regroups its
 * digits while they are typed puts the caret here, so it stays after the digit it was after.
 */
export function caretAfterDigits(display: string, count: number, separator: string): number {
  if (count === 0) {
    return 0;
  }

  let seen = 0;
  for (let index = 0; index < display.length; index++) {
    if (display[index] !== separator) {
      seen++;
    }

    if (seen === count) {
      return index + 1;
    }
  }

  return display.length;
}
