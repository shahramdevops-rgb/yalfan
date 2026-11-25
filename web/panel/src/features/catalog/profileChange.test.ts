import { describe, expect, it, vi } from 'vitest';
import { createSerialWrite } from '../../shared/api/serialWrites';
import { profileChange, type ProductDetails } from './productsApi';

// The API answers what it was sent, as a save does; the bodies are kept to look at.
const sent: { path: string; body: Record<string, unknown> }[] = [];
vi.mock('../../shared/api/client', () => ({
  api: {
    PUT: (path: string, init: { params: { path: { id: string } }; body: Record<string, unknown> }) => {
      sent.push({ path, body: init.body });
      const { categoryId, ...rest } = init.body;
      const answer = { id: init.params.path.id, ...rest, category: categoryId ? { id: categoryId } : null };
      return new Promise((resolve) => setTimeout(() => resolve({ data: answer }), 10));
    },
  },
}));

describe('profileChange', () => {
  it('saves a model description and a category from one gesture without the second undoing the first', async () => {
    let stored = { id: 'm1', name: 'کیف آزمایشی', description: null, category: null } as unknown as ProductDetails;
    const write = createSerialWrite(
      { current: Promise.resolve() },
      () => stored,
      (value) => (stored = value),
    );

    await Promise.all([
      write(profileChange({ description: 'دو جیب' })),
      write(profileChange({ categoryId: 'shoulder' })),
    ]);

    expect(sent[1]?.body).toEqual({ name: 'کیف آزمایشی', description: 'دو جیب', categoryId: 'shoulder' });
  });
});
