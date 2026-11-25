import { describe, expect, it, vi } from 'vitest';
import { createSerialWrite } from '../../shared/api/serialWrites';
import { profileChange, type PartyDetails } from './partiesApi';

// The API answers what it was sent, as a save does; the bodies are kept to look at.
const sent: { path: string; body: Record<string, unknown> }[] = [];
vi.mock('../../shared/api/client', () => ({
  api: {
    PUT: (path: string, init: { params: { path: { id: string } }; body: Record<string, unknown> }) => {
      sent.push({ path, body: init.body });
      const answer = {
        id: init.params.path.id,
        ...init.body,
        stages: (init.body.stageIds as string[]).map((id) => ({ id, name: id, isActive: true })),
      };
      return new Promise((resolve) => setTimeout(() => resolve({ data: answer }), 10));
    },
  },
}));

describe('profileChange', () => {
  it('saves a contractor note and a stage from one gesture without the second undoing the first', async () => {
    let stored = {
      id: 'c1',
      fullName: 'حسن حسینی',
      mobileNumber: '09131000007',
      notes: null,
      stages: [{ id: 'sewing', name: 'دوخت', isActive: true }],
    } as unknown as PartyDetails;
    const write = createSerialWrite(
      { current: Promise.resolve() },
      () => stored,
      (value) => (stored = value),
    );

    await Promise.all([
      write(profileChange('contractors', { notes: 'شاگرد دارد' })),
      write(profileChange('contractors', { stageIds: ['sewing', 'packing'] })),
    ]);

    expect(sent[1]?.body).toMatchObject({ notes: 'شاگرد دارد', stageIds: ['sewing', 'packing'] });
  });
});
