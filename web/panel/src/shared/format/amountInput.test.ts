import { describe, expect, it } from 'vitest';
import { amountMaxDigits, groupDigits, readAmountInput } from './amountInput';

describe('groupDigits', () => {
  it('groups in threes with the separator Intl uses, in Persian digits', () => {
    expect(groupDigits('')).toBe('');
    expect(groupDigits('120')).toBe('۱۲۰');
    expect(groupDigits('1200')).toBe('۱٬۲۰۰');
    expect(groupDigits('120000000')).toBe('۱۲۰٬۰۰۰٬۰۰۰');
  });

  it('matches how an amount is shown, so typed and shown look alike', () => {
    expect(groupDigits('1250000')).toBe(new Intl.NumberFormat('fa-IR').format(1250000));
  });
});

describe('readAmountInput', () => {
  it('takes Persian, Arabic and Latin digits and drops everything else', () => {
    expect(readAmountInput('۱۲٠3x ,4', 8).digits).toBe('12034');
  });

  it('regroups after a key and keeps the caret after the digit just typed', () => {
    // "۱۲۰٬۰۰۰" with a 5 typed after the 2: the text is "۱۲5۰٬۰۰۰", the caret after the 5.
    const read = readAmountInput('۱۲5۰٬۰۰۰', 3);
    expect(read.digits).toBe('1250000');
    expect(read.display).toBe('۱٬۲۵۰٬۰۰۰');
    expect(read.display.slice(0, read.caret)).toBe('۱٬۲۵');
  });

  it('keeps the caret after the last digit when a new separator appears before it', () => {
    // "۱۲۳" and a 4 typed at its end: four characters become five.
    const read = readAmountInput('۱۲۳4', 4);
    expect(read.display).toBe('۱٬۲۳۴');
    expect(read.caret).toBe(5);
  });

  it('keeps the caret at the start when the first digit is deleted and a separator goes', () => {
    // Backspace after the 1 of "۱٬۲۳۴" leaves "٬۲۳۴" with the caret at the start.
    expect(readAmountInput('٬۲۳۴', 0)).toEqual({ digits: '234', display: '۲۳۴', caret: 0 });
  });

  it('drops leading zeros but keeps a lone zero, so the form can refuse it with its message', () => {
    expect(readAmountInput('0012', 4)).toEqual({ digits: '12', display: '۱۲', caret: 2 });
    expect(readAmountInput('000', 3)).toEqual({ digits: '0', display: '۰', caret: 1 });
    expect(readAmountInput('', 0)).toEqual({ digits: '', display: '', caret: 0 });
  });

  it('takes no digit past the fifteenth, which a JavaScript number still holds exactly', () => {
    const read = readAmountInput('1'.repeat(amountMaxDigits + 3), amountMaxDigits + 3);
    expect(read.digits).toHaveLength(amountMaxDigits);
    expect(Number.isSafeInteger(Number('9'.repeat(amountMaxDigits)))).toBe(true);
  });

  it('reads a pasted amount with its separators', () => {
    expect(readAmountInput('1,250,000 ریال', 14).digits).toBe('1250000');
  });
});
