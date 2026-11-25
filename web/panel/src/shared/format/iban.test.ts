import { describe, expect, it } from 'vitest';
import { formatIban, groupIbanDigits, ibanDigitCount, ibanLastFour, readIbanInput } from './iban';
import { formatMobile } from './mobile';

/** The Iranian example of the SWIFT IBAN registry, the one the API's tests use. */
const iban = 'IR710570029971601460641001';

describe('groupIbanDigits', () => {
  it('groups as on a bank card: the check digits finish the first four with IR, then fours', () => {
    expect(groupIbanDigits('')).toBe('');
    expect(groupIbanDigits('7')).toBe('۷');
    expect(groupIbanDigits('71')).toBe('۷۱');
    expect(groupIbanDigits('710')).toBe('۷۱ ۰');
    expect(groupIbanDigits(iban.slice(2))).toBe('۷۱ ۰۵۷۰ ۰۲۹۹ ۷۱۶۰ ۱۴۶۰ ۶۴۱۰ ۰۱');
  });
});

describe('readIbanInput', () => {
  it('takes Persian, Arabic and Latin digits and drops everything else', () => {
    expect(readIbanInput('۷١x-0', 5).digits).toBe('710');
  });

  it('reads a whole pasted IBAN with its IR and spaces as its 24 digits', () => {
    const read = readIbanInput('IR71 0570 0299 7160 1460 6410 01', 32);
    expect(read.digits).toBe(iban.slice(2));
    expect(read.caret).toBe(read.display.length);
  });

  it('takes no digit past the 24th', () => {
    expect(readIbanInput('1'.repeat(30), 30).digits).toHaveLength(ibanDigitCount);
  });

  it('keeps the caret after the digit typed in the middle when the groups move', () => {
    // "۷۱ ۰۵۷۰" with a 9 typed after the 1: the text is "۷۱9 ۰۵۷۰", the caret after the 9.
    const read = readIbanInput('۷۱9 ۰۵۷۰', 3);
    expect(read.digits).toBe('7190570');
    expect(read.display).toBe('۷۱ ۹۰۵۷ ۰');
    expect(read.display.slice(0, read.caret)).toBe('۷۱ ۹');
  });

  it('keeps the caret after the last digit when a new group starts', () => {
    expect(readIbanInput('۷۱0', 3)).toEqual({ digits: '710', display: '۷۱ ۰', caret: 4 });
  });
});

describe('formatIban', () => {
  it('shows the whole number with IR and Persian digits, grouped as on a bank card', () => {
    expect(formatIban(iban)).toBe('IR۷۱ ۰۵۷۰ ۰۲۹۹ ۷۱۶۰ ۱۴۶۰ ۶۴۱۰ ۰۱');
  });
});

describe('ibanLastFour', () => {
  it('reads the last four digits of the masked form the API sends', () => {
    expect(ibanLastFour('IR** **** **** **** **** **10 01')).toBe('۱۰۰۱');
  });
});

describe('formatMobile', () => {
  it('groups a stored mobile as it is read aloud, in Persian digits', () => {
    expect(formatMobile('09121234567')).toBe('۰۹۱۲ ۱۲۳ ۴۵۶۷');
  });

  it('only puts anything else in Persian digits', () => {
    expect(formatMobile('0912')).toBe('۰۹۱۲');
  });
});
