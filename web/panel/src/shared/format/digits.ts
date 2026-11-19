// Persian, Arabic or Latin digits are a display choice; what the user types is normalized
// (docs/ARCHITECTURE.md, section 10).

const persianZero = 0x06f0;
const arabicZero = 0x0660;

/** Replaces Persian (۰–۹) and Arabic (٠–٩) digits with Latin ones; everything else stays. */
export function toLatinDigits(text: string): string {
  return text.replace(/[۰-۹٠-٩]/g, (digit) => {
    const code = digit.charCodeAt(0);
    return String(code - (code >= persianZero ? persianZero : arabicZero));
  });
}

/** Latin digits shown as Persian ones, keeping leading zeros: "0912" becomes "۰۹۱۲". */
export function toPersianDigits(text: string): string {
  return text.replace(/[0-9]/g, (digit) => String.fromCharCode(persianZero + Number(digit)));
}

/** Only the digits of what was typed, in Latin: "۰۹۱۲ ۱۲۳-۴۵۶۷" becomes "09121234567". */
export function digitsOnly(text: string): string {
  return toLatinDigits(text).replace(/\D/g, '');
}

const integer = new Intl.NumberFormat('fa-IR', { useGrouping: false });
const twoDigits = new Intl.NumberFormat('fa-IR', { minimumIntegerDigits: 2, useGrouping: false });

/** A countdown in Persian digits: 105 seconds becomes "۱:۴۵". */
export function formatCountdown(totalSeconds: number): string {
  const seconds = Math.max(0, Math.ceil(totalSeconds));
  return `${integer.format(Math.floor(seconds / 60))}:${twoDigits.format(seconds % 60)}`;
}
