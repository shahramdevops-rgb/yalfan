import { describe, expect, it } from 'vitest';
import { readTypedNumber, typedDigit } from './typedNumber';

describe('typedDigit', () => {
  it('reads a Latin, a Persian and an Arabic digit alike', () => {
    expect(typedDigit('7')).toBe('7');
    expect(typedDigit('۷')).toBe('7');
    expect(typedDigit('٧')).toBe('7');
    expect(typedDigit('۰')).toBe('0');
  });

  it('is no digit for a letter or a named key', () => {
    expect(typedDigit('a')).toBeNull();
    expect(typedDigit('آ')).toBeNull();
    expect(typedDigit('Enter')).toBeNull();
    expect(typedDigit('ArrowDown')).toBeNull();
  });
});

const days = Array.from({ length: 31 }, (_, index) => index + 1);
const months = Array.from({ length: 12 }, (_, index) => index + 1);
const years = Array.from({ length: 11 }, (_, index) => 1395 + index);

describe('readTypedNumber', () => {
  it('picks a day as soon as it is whole, and the longer one after it', () => {
    expect(readTypedNumber('1', days)).toEqual({ kept: '1', pick: 1 });
    expect(readTypedNumber('15', days)).toEqual({ kept: '15', pick: 15 });
  });

  it('starts again from the last digit when the digits make no value', () => {
    expect(readTypedNumber('153', days)).toEqual({ kept: '3', pick: 3 });
    expect(readTypedNumber('45', days)).toEqual({ kept: '5', pick: 5 });
    expect(readTypedNumber('130', months)).toEqual({ kept: '' });
  });

  it('picks a month by its number', () => {
    expect(readTypedNumber('9', months)).toEqual({ kept: '9', pick: 9 });
    expect(readTypedNumber('12', months)).toEqual({ kept: '12', pick: 12 });
  });

  it('waits for a year to be whole, keeping what begins one', () => {
    expect(readTypedNumber('1', years)).toEqual({ kept: '1' });
    expect(readTypedNumber('14', years)).toEqual({ kept: '14' });
    expect(readTypedNumber('140', years)).toEqual({ kept: '140' });
    expect(readTypedNumber('1403', years)).toEqual({ kept: '1403', pick: 1403 });
  });

  it('keeps nothing when no value begins with the digits', () => {
    expect(readTypedNumber('0', months)).toEqual({ kept: '' });
    expect(readTypedNumber('7', years)).toEqual({ kept: '' });
  });
});
