import { describe, expect, it } from 'vitest';
import { dropIndex, moveItem, shiftOf } from './sortable';

describe('moveItem', () => {
  const stages = ['cutting', 'sewing', 'packing', 'handles'];

  it('moves an item down and up without changing the others', () => {
    expect(moveItem(stages, 3, 2)).toEqual(['cutting', 'sewing', 'handles', 'packing']);
    expect(moveItem(stages, 0, 2)).toEqual(['sewing', 'packing', 'cutting', 'handles']);
    expect(stages).toEqual(['cutting', 'sewing', 'packing', 'handles']);
  });

  it('keeps the list when the item stays in place', () => {
    expect(moveItem(stages, 1, 1)).toEqual(stages);
  });

  it('refuses a place outside the list', () => {
    expect(() => moveItem(stages, 0, 4)).toThrow(RangeError);
    expect(() => moveItem(stages, -1, 0)).toThrow(RangeError);
  });
});

describe('dropIndex', () => {
  // Four rows 50 pixels apart: their middles.
  const middles = [25, 75, 125, 175];

  it('stays in place until the middle of the dragged row passes the middle of a neighbour', () => {
    expect(dropIndex(middles, 1, 75)).toBe(1);
    expect(dropIndex(middles, 1, 124)).toBe(1);
    expect(dropIndex(middles, 1, 126)).toBe(2);
  });

  it('goes to the ends of the list', () => {
    expect(dropIndex(middles, 2, -100)).toBe(0);
    expect(dropIndex(middles, 0, 1000)).toBe(3);
  });

  it('moves up past a row whose middle it crosses', () => {
    expect(dropIndex(middles, 3, 74)).toBe(1);
  });
});

describe('shiftOf', () => {
  it('moves the rows between the old and new place up when dragging down', () => {
    expect([0, 1, 2, 3].map((index) => shiftOf(index, 0, 2))).toEqual([0, -1, -1, 0]);
  });

  it('moves them down when dragging up', () => {
    expect([0, 1, 2, 3].map((index) => shiftOf(index, 3, 1))).toEqual([0, 1, 1, 0]);
  });

  it('moves nothing while the row is over its own place', () => {
    expect([0, 1, 2].map((index) => shiftOf(index, 1, 1))).toEqual([0, 0, 0]);
  });
});
