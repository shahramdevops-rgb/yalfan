import { caretAfterDigits } from './caret';
import { toLatinDigits, toPersianDigits } from './digits';

// An Iranian IBAN (شبا) is IR and 24 digits (ADR 0019). The field shows IR fixed in front and
// takes the 24 digits; they are grouped as on a bank card, «IR71 0570 0299 …»: the two check
// digits complete the first group of four, then fours.

/** The digits after IR. */
export const ibanDigitCount = 24;

const separator = ' ';

export interface IbanInput {
  /** Up to 24 Latin digits: what is sent, with IR in front. */
  digits: string;
  /** What the field shows: Persian digits grouped as on a bank card. */
  display: string;
  /** Where the caret goes in `display`, so it stays after the digit it was after. */
  caret: number;
}

/**
 * Reads what the IBAN field holds after a key press or a paste. Persian, Arabic and Latin digits
 * are taken and everything else is dropped, so a whole IBAN pasted with its IR, spaces or dashes
 * leaves its 24 digits. Digits past the 24th are dropped.
 */
export function readIbanInput(text: string, caret: number): IbanInput {
  const latin = toLatinDigits(text);
  const digits = latin.replace(/\D/g, '').slice(0, ibanDigitCount);
  const digitsBeforeCaret = Math.min(latin.slice(0, caret).replace(/\D/g, '').length, digits.length);

  const display = groupIbanDigits(digits);
  return { digits, display, caret: caretAfterDigits(display, digitsBeforeCaret, separator) };
}

/** The digits after IR as the field shows them: "710570029971" becomes "۷۱ ۰۵۷۰ ۰۲۹۹ ۷۱". */
export function groupIbanDigits(digits: string): string {
  const groups = [digits.slice(0, 2), ...(digits.slice(2).match(/\d{1,4}/g) ?? [])].filter(Boolean);
  return toPersianDigits(groups.join(separator));
}

/** A whole IBAN from the API, "IR710570…", as the owner reads it: "IR۷۱ ۰۵۷۰ ۰۲۹۹ …". */
export function formatIban(iban: string): string {
  return `IR${groupIbanDigits(iban.replace(/^IR/i, ''))}`;
}

/** The last four digits of the masked IBAN the API sends ("IR** **** … **12 34"), in Persian: "۱۲۳۴". */
export function ibanLastFour(masked: string): string {
  return toPersianDigits(masked.replace(/\D/g, '').slice(-4));
}
