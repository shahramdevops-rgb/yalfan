import { describe, expect, it } from 'vitest';
import { bagCount, bagsToAdd, existingPicks, newBags, pairsOf, toggleColour, toggleFabric, type Picks } from './bags';

const canvasBlackBrown: Picks = [{ fabricId: 'canvas', colourIds: ['black', 'brown'] }];

const bag = (fabric: string, colour: string) => ({ fabric: { id: fabric }, colour: { id: colour } });

describe('toggleFabric', () => {
  it('adds a fabric with no colours yet, at the end', () => {
    expect(toggleFabric(canvasBlackBrown, 'leather')).toEqual([
      ...canvasBlackBrown,
      { fabricId: 'leather', colourIds: [] },
    ]);
  });

  it('takes a ticked fabric out with its colours', () => {
    expect(toggleFabric(canvasBlackBrown, 'canvas')).toEqual([]);
  });
});

describe('toggleColour', () => {
  it('ticks and unticks a colour in one fabric only', () => {
    const picks: Picks = [...canvasBlackBrown, { fabricId: 'leather', colourIds: [] }];

    const ticked = toggleColour(picks, 'leather', 'black');
    expect(ticked[1]).toEqual({ fabricId: 'leather', colourIds: ['black'] });
    expect(ticked[0]).toBe(picks[0]);

    expect(toggleColour(ticked, 'canvas', 'black')[0]).toEqual({ fabricId: 'canvas', colourIds: ['brown'] });
  });
});

describe('newBags', () => {
  it('makes one bag per fabric and colour when the model has no sizes', () => {
    expect(newBags(canvasBlackBrown, [])).toEqual([
      { fabricId: 'canvas', colourId: 'black', size: null },
      { fabricId: 'canvas', colourId: 'brown', size: null },
    ]);
  });

  it('makes every fabric and colour in every size, the sizes of a colour together', () => {
    expect(newBags(canvasBlackBrown, ['کوچک', 'بزرگ']).map((b) => `${b.colourId} ${b.size}`)).toEqual([
      'black کوچک',
      'black بزرگ',
      'brown کوچک',
      'brown بزرگ',
    ]);
  });

  it('makes nothing for a fabric with no colour ticked', () => {
    expect(newBags([{ fabricId: 'leather', colourIds: [] }], ['کوچک'])).toEqual([]);
  });
});

describe('bagCount', () => {
  it('counts each fabric and colour once per size, or once without sizes', () => {
    const picks: Picks = [...canvasBlackBrown, { fabricId: 'leather', colourIds: ['black'] }];

    expect(bagCount(picks, 0)).toBe(3);
    expect(bagCount(picks, 3)).toBe(9);
    expect(bagCount([], 3)).toBe(0);
  });
});

describe('existingPicks', () => {
  it('lists each fabric and colour a model has a bag in once, whatever the sizes', () => {
    const bags = [bag('canvas', 'black'), bag('canvas', 'black'), bag('leather', 'black'), bag('canvas', 'brown')];

    expect(existingPicks(bags)).toEqual([
      { fabricId: 'canvas', colourIds: ['black', 'brown'] },
      { fabricId: 'leather', colourIds: ['black'] },
    ]);
  });
});

describe('bagsToAdd', () => {
  it('adds only the newly ticked fabrics and colours, in every size', () => {
    const picks: Picks = [{ fabricId: 'canvas', colourIds: ['black', 'brown'] }];

    expect(bagsToAdd(picks, [bag('canvas', 'black')], ['s', 'l'])).toEqual([
      { fabricId: 'canvas', colourId: 'brown', sizeId: 's' },
      { fabricId: 'canvas', colourId: 'brown', sizeId: 'l' },
    ]);
  });

  it('adds a bag without a size to a model that has none', () => {
    expect(bagsToAdd(canvasBlackBrown, [], [])).toEqual([
      { fabricId: 'canvas', colourId: 'black', sizeId: null },
      { fabricId: 'canvas', colourId: 'brown', sizeId: null },
    ]);
  });

  it('adds nothing when every tick already has a bag', () => {
    expect(bagsToAdd(canvasBlackBrown, [bag('canvas', 'black'), bag('canvas', 'brown')], [])).toEqual([]);
    expect(pairsOf([])).toEqual([]);
  });
});
