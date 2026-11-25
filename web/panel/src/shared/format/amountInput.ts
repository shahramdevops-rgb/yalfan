import { caretAfterDigits } from './caret';
import { toLatinDigits, toPersianDigits } from './digits';

// The amount field keeps what was typed as plain Latin digits and shows it grouped in threes in
// Persian digits, as the amount is shown everywhere else (ADR 0002, ADR 0024).

/**
 * The most digits an amount field takes: a JavaScript number holds every whole number of 15
 * digits exactly, and no amount the workshop types comes near it.
 */
export const amountMaxDigits = 15;

/** The thousands separator Intl uses for fa-IR, so a typed amount looks like a shown one. */
const separator = '٬';

export interface AmountInput {
  /** Latin digits without leading zeros, or '' when nothing is typed: what is sent. */
  digits: string;
  /** What the field shows: Persian digits grouped in threes. */
  display: string;
  /** Where the caret goes in `display`, so it stays after the digit it was after. */
  caret: number;
}

/**
 * Reads what the field holds after a key press or a paste. Persian, Arabic and Latin digits are
 * taken; every other character (a separator, a space, a letter) is dropped. Digits past the
 * fifteenth are dropped too. The caret keeps its place among the digits, so a digit typed in the
 * middle of an amount does not throw the caret to the end.
 */
export function readAmountInput(text: string, caret: number): AmountInput {
  const latin = toLatinDigits(text);
  const typed = latin.replace(/\D/g, '');
  const typedBeforeCaret = latin.slice(0, caret).replace(/\D/g, '').length;

  const leadingZeros = typed.length - typed.replace(/^0+/, '').length;
  // A lone zero stays, so the field can say «۰» and the form can refuse it with its message.
  const dropped = typed.length > 0 && leadingZeros === typed.length ? leadingZeros - 1 : leadingZeros;
  const digits = typed.slice(dropped, dropped + amountMaxDigits);
  const digitsBeforeCaret = Math.min(Math.max(typedBeforeCaret - dropped, 0), digits.length);

  const display = groupDigits(digits);
  return { digits, display, caret: caretAfterDigits(display, digitsBeforeCaret, separator) };
}

/** Latin digits as the field shows them: "1200000" becomes "۱٬۲۰۰٬۰۰۰". */
export function groupDigits(digits: string): string {
  return toPersianDigits(digits.replace(/\B(?=(\d{3})+(?!\d))/g, separator));
}
