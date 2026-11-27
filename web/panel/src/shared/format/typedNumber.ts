/** Persian (۰–۹) and Arabic (٠–٩) digits, which a Persian keyboard types in place of Latin ones. */
const persianZero = 0x06f0;
const arabicZero = 0x0660;

/** The digit a key types, as a Latin digit, whether Latin, Persian or Arabic; null for any other key. */
export function typedDigit(key: string): string | null {
  if (key.length !== 1) {
    return null;
  }

  if (key >= '0' && key <= '9') {
    return key;
  }

  const code = key.charCodeAt(0);
  for (const zero of [persianZero, arabicZero]) {
    if (code >= zero && code <= zero + 9) {
      return String(code - zero);
    }
  }

  return null;
}

/**
 * A number typed digit by digit into a list (a day, a month, a year), as a list's own type-ahead
 * does with letters (ADR 0031). `typed` is what was typed so far, with the new digit at its end.
 * Answers what is kept for the next digit, and the value to pick, if one matches whole:
 * - «۱» then «۵» picks 1, then 15;
 * - «۱۴۰۳» picks nothing until the last digit, since 1, 14 and 140 begin a year;
 * - a digit that makes no value starts again from itself: 15 then «۳» picks 3.
 */
export function readTypedNumber(typed: string, values: readonly number[]): { kept: string; pick?: number } {
  for (const attempt of [typed, typed.slice(-1)]) {
    const whole = values.find((value) => String(value) === attempt);
    if (whole !== undefined) {
      return { kept: attempt, pick: whole };
    }

    if (values.some((value) => String(value).startsWith(attempt))) {
      return { kept: attempt };
    }
  }

  return { kept: '' };
}
