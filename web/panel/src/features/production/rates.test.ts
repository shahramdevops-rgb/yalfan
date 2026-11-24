import { describe, expect, it } from 'vitest';
import { correctedBy, entriesOf, entryState, inForceOn, upcomingRate, type RateHistoryEntry } from './rates';

const today = '2026-11-25';
const sewing = 'sewing';

const entry = (id: string, startsOn: string, amount: number, isReplaced = false): RateHistoryEntry => ({
  id,
  stage: { id: sewing },
  amount,
  startsOn,
  isReplaced,
});

// Newest first, as the API sends them: a raise from 1 Dey, a mistyped rate from 1 Azar corrected
// by a later entry with the same date, and an old rate from the paper book.
const raise = entry('raise', '2026-12-22', 150_000);
const correction = entry('correction', '2026-11-22', 120_000);
const typo = entry('typo', '2026-11-22', 12_000, true);
const old = entry('old', '2025-03-21', 100_000);
const history = [raise, correction, typo, old];

describe('the rate card around today', () => {
  it('finds the rate in force, skipping the corrected entry and the raise to come', () => {
    expect(inForceOn(history, today)).toBe(correction);
    expect(inForceOn(history, '2026-01-01')).toBe(old);
    expect(inForceOn(history, '2025-01-01')).toBeUndefined();
  });

  it('shows the raise already entered for later', () => {
    expect(upcomingRate(history, today)).toBe(raise);
    expect(upcomingRate(history, '2026-12-22')).toBeUndefined();
  });

  it('shows the nearest of several changes to come, never a corrected one', () => {
    const later = entry('later', '2027-03-21', 180_000);
    const correctedRaise = entry('wrong', '2026-12-01', 1_400_000, true);
    expect(upcomingRate([later, raise, correctedRaise, correction], today)).toBe(raise);
  });

  it('names the entry a new rate with the same start date corrects', () => {
    expect(correctedBy(history, '2026-11-22')).toBe(correction);
    expect(correctedBy(history, '2026-11-23')).toBeUndefined();
  });

  it('keeps one stage apart from the others', () => {
    const cutting = { ...entry('cut', '2026-11-01', 50_000), stage: { id: 'cutting' } };
    expect(entriesOf([...history, cutting], sewing)).toEqual(history);
  });
});

describe('entryState', () => {
  it('labels each entry of the history', () => {
    expect(entryState(raise, history, today)).toBe('future');
    expect(entryState(correction, history, today)).toBe('current');
    expect(entryState(typo, history, today)).toBe('replaced');
    expect(entryState(old, history, today)).toBe('past');
  });

  it('calls a rate that starts today the current one', () => {
    const fromToday = entry('today', today, 130_000);
    expect(entryState(fromToday, [fromToday, ...history], today)).toBe('current');
  });
});
