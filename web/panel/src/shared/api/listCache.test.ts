import { expect, it } from 'vitest';
import { upsert } from './listCache';

const felt = { id: 'a', name: 'نمد', isActive: true };
const leather = { id: 'b', name: 'چرم', isActive: true };

it('puts a changed row in its own place', () => {
  expect(upsert([felt, leather], { ...felt, isActive: false })).toEqual([{ ...felt, isActive: false }, leather]);
});

it('adds a new row at the end', () => {
  expect(upsert([felt], leather)).toEqual([felt, leather]);
});

it('starts a list that is not loaded yet', () => {
  expect(upsert(undefined, felt)).toEqual([felt]);
});
