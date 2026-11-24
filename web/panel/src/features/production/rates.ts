// What the rate card shows around the rate in force, read from the history the API sends. The
// API decides which entries are replaced (ADR 0021); these only sort what it answered.

export interface RateHistoryEntry {
  id: string;
  stage: { id: string };
  amount: number;
  /** A calendar day, "2026-11-25"; ISO days compare correctly as text. */
  startsOn: string;
  isReplaced: boolean;
}

export type EntryState = 'current' | 'future' | 'past' | 'replaced';

/** One stage's entries, newest first, as the API orders them. */
export const entriesOf = <T extends RateHistoryEntry>(history: readonly T[], stageId: string): T[] =>
  history.filter((entry) => entry.stage.id === stageId);

/**
 * The next change already entered for a stage: the earliest start date after `on` among the
 * entries that were not corrected. Shown on the card, so a raise entered for next month is seen
 * as saved and is not typed in again.
 */
export function upcomingRate<T extends RateHistoryEntry>(entries: readonly T[], on: string): T | undefined {
  return entries
    .filter((entry) => !entry.isReplaced && entry.startsOn > on)
    .reduce<T | undefined>((next, entry) => (next && next.startsOn <= entry.startsOn ? next : entry), undefined);
}

/** The entry that a new rate with this start date corrects, if one has the same start date (ADR 0021). */
export function correctedBy<T extends RateHistoryEntry>(entries: readonly T[], startsOn: string): T | undefined {
  return entries.find((entry) => !entry.isReplaced && entry.startsOn === startsOn);
}

/** The entry in force on `on`: of those not corrected, the latest start date by then. */
export function inForceOn<T extends RateHistoryEntry>(entries: readonly T[], on: string): T | undefined {
  return entries
    .filter((entry) => !entry.isReplaced && entry.startsOn <= on)
    .reduce<T | undefined>(
      (latest, entry) => (latest && latest.startsOn >= entry.startsOn ? latest : entry),
      undefined,
    );
}

/** How the history labels an entry: corrected, in force today, still to start, or over. */
export function entryState(entry: RateHistoryEntry, entries: readonly RateHistoryEntry[], on: string): EntryState {
  if (entry.isReplaced) {
    return 'replaced';
  }

  if (entry.startsOn > on) {
    return 'future';
  }

  return inForceOn(entries, on)?.id === entry.id ? 'current' : 'past';
}
