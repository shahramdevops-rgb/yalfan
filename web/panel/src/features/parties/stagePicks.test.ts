import { describe, expect, it } from 'vitest';
import type { Stage } from '../production/stagesApi';
import { canUntick, stageChoices, toggleStage } from './stagePicks';

// The stage list in the order of the work (ADR 0022); "finishing" was deactivated.
const cutting: Stage = { id: 'c', name: 'برش', isActive: true };
const sewing: Stage = { id: 's', name: 'دوخت', isActive: true };
const finishing: Stage = { id: 'f', name: 'پرداخت', isActive: false };
const packing: Stage = { id: 'p', name: 'بسته‌بندی', isActive: true };
const stages = [cutting, sewing, finishing, packing];

describe('stageChoices', () => {
  it('offers the active stages in the order of the work', () => {
    expect(stageChoices(stages, [])).toEqual([cutting, sewing, packing]);
  });

  it('keeps an inactive stage in its place while it is still ticked', () => {
    expect(stageChoices(stages, ['f'])).toEqual([cutting, sewing, finishing, packing]);
  });
});

describe('toggleStage', () => {
  it('ticks and unticks, keeping the order of the work whatever order they were pressed in', () => {
    expect(toggleStage(stages, ['p'], 'c')).toEqual(['c', 'p']);
    expect(toggleStage(stages, ['c', 's', 'p'], 's')).toEqual(['c', 'p']);
  });
});

describe('canUntick', () => {
  it('keeps the last stage of a contractor, who does at least one', () => {
    expect(canUntick('contractors', ['s'], 's')).toBe(false);
    expect(canUntick('contractors', ['s', 'p'], 's')).toBe(true);
  });

  it('lets a worker have no stage, such as a storekeeper', () => {
    expect(canUntick('workers', ['s'], 's')).toBe(true);
  });
});
