import { describe, expect, it } from 'vitest';
import { createSerialWrite, inTurn, type WriteQueue } from './serialWrites';

const later = <T>(value: T, ms: number) => new Promise<T>((resolve) => setTimeout(() => resolve(value), ms));

describe('inTurn', () => {
  it('starts a task only after the one before it has finished, even when that one is slower', async () => {
    const queue: WriteQueue = { current: Promise.resolve() };
    const log: string[] = [];

    const first = inTurn(queue, async () => {
      log.push('first started');
      await later(null, 30);
      log.push('first done');
      return 1;
    });
    const second = inTurn(queue, () => {
      log.push('second started');
      return Promise.resolve(2);
    });

    expect(await Promise.all([first, second])).toEqual([1, 2]);
    expect(log).toEqual(['first started', 'first done', 'second started']);
  });

  it('goes on after a failed task, and hands that failure only to its own caller', async () => {
    const queue: WriteQueue = { current: Promise.resolve() };

    const failed = inTurn(queue, () => Promise.reject(new Error('refused')));
    const next = inTurn(queue, () => Promise.resolve('saved'));

    await expect(failed).rejects.toThrow('refused');
    await expect(next).resolves.toBe('saved');
  });
});

describe('createSerialWrite', () => {
  it('builds each change from what the change before it saved, not from what the page drew', async () => {
    // A record of two fields, each change sending both, as a profile does.
    let stored = { notes: '', stages: ['sewing'] };
    const write = createSerialWrite(
      { current: Promise.resolve() },
      () => stored,
      (value) => (stored = value),
    );
    const saved: (typeof stored)[] = [];
    const save = (change: Partial<typeof stored>) => (current: typeof stored) => {
      const next = { ...current, ...change };
      saved.push(next);
      return later(next, 10);
    };

    // Both started in one gesture, before either answer came back.
    await Promise.all([write(save({ notes: 'has a helper' })), write(save({ stages: ['sewing', 'packing'] }))]);

    expect(saved[1]).toEqual({ notes: 'has a helper', stages: ['sewing', 'packing'] });
    expect(stored).toEqual({ notes: 'has a helper', stages: ['sewing', 'packing'] });
  });
});
