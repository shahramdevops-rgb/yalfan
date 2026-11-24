import { t } from '../i18n/fa';
import { persianWords } from './numberWords';

// Money is Rial: stored, in the API, typed and shown, and every amount shown is labelled «ریال»
// (ADR 0002). Under every amount shown, a quieter line says it in words, in Toman (ADR 0002
// point 9, ADR 0024): Toman is only ever read, never stored, sent or typed.

/** What the API sends for an amount: a whole number of Rial, or the same as a decimal string. */
export type RialAmount = number | `${number}`;

const rial = new Intl.NumberFormat('fa-IR', { maximumFractionDigits: 0 });

/**
 * An amount in Rial, in Persian digits with thousands separators and the «ریال» label:
 * 12500000 becomes "۱۲٬۵۰۰٬۰۰۰ ریال". A decimal string from the API is formatted exactly,
 * without passing through a floating-point number.
 */
export function formatRial(amount: RialAmount): string {
  return `${rial.format(amount)} ${t('money.rial')}`;
}

/**
 * The line under a Rial amount: the amount in words, in Toman, as people say it aloud
 * («دوازده هزار تومان» under 120000 Rial). Only the words, with no Toman figure, so the screen
 * shows one figure, in Rial (decided by the developer on 1405/09/04, ADR 0024). Never rounded,
 * because the line must say exactly the amount above it: 12345 Rial is «هزار و دویست و سی و چهار
 * تومان و پنج ریال».
 */
export function tomanLine(amount: RialAmount): string {
  const value = toBigInt(amount);
  const negative = value < 0n;
  const magnitude = negative ? -value : value;
  const tomans = magnitude / 10n;
  const rials = magnitude % 10n;

  let words: string;
  if (rials === 0n) {
    words = `${persianWords(tomans)} ${t('money.toman')}`;
  } else if (tomans === 0n) {
    words = `${persianWords(rials)} ${t('money.rial')}`;
  } else {
    words = t('money.tomanAndRial', {
      toman: persianWords(tomans),
      rial: persianWords(rials),
    });
  }

  return negative ? t('money.negative', { words }) : words;
}

function toBigInt(amount: RialAmount): bigint {
  if (typeof amount === 'number' && !Number.isSafeInteger(amount)) {
    throw new RangeError(`Not a whole number of Rial that a number can hold exactly: ${amount}`);
  }

  // BigInt refuses a fraction or anything that is not a number, which is what is wanted here.
  return BigInt(amount);
}
