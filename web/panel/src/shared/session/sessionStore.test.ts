import { describe, expect, it, vi } from 'vitest';
import { createSessionStore, REFRESH_SUPERSEDED, type RenewResult, type SessionResponse } from './sessionStore';

const session = (token: string): SessionResponse => ({
  accessToken: token,
  accessTokenExpiresAt: '2026-11-19T08:15:00Z',
  user: { id: '0193a000-0000-7000-8000-000000000001', fullName: 'Owner', mobileNumber: '09120000000', role: 'owner' },
});

const ok = (token: string): RenewResult => ({ ok: true, session: session(token) });
const failed = (code: string): RenewResult => ({ ok: false, code });
const noWait = () => Promise.resolve();

describe('session store', () => {
  it('starts loading, and a successful renewal signs in with the new token', async () => {
    const store = createSessionStore({ renew: () => Promise.resolve(ok('t1')), wait: noWait });
    expect(store.getState().status).toBe('loading');

    expect(await store.renew()).toBe(true);

    expect(store.getState()).toMatchObject({ status: 'signedIn', user: { fullName: 'Owner' } });
    expect(store.getAccessToken()).toBe('t1');
  });

  it('sends one renewal for callers that ask at the same time', async () => {
    const renew = vi.fn(() => Promise.resolve(ok('t1')));
    const store = createSessionStore({ renew, wait: noWait });

    const results = await Promise.all([store.renew(), store.renew(), store.renew()]);

    expect(results).toEqual([true, true, true]);
    expect(renew).toHaveBeenCalledTimes(1);
  });

  it('tries once more after "superseded", because another tab renewed first', async () => {
    const renew = vi.fn<() => Promise<RenewResult>>()
      .mockResolvedValueOnce(failed(REFRESH_SUPERSEDED))
      .mockResolvedValueOnce(ok('t2'));
    const wait = vi.fn(noWait);
    const store = createSessionStore({ renew, wait });

    expect(await store.renew()).toBe(true);

    expect(renew).toHaveBeenCalledTimes(2);
    expect(wait).toHaveBeenCalledOnce();
    expect(store.getAccessToken()).toBe('t2');
  });

  it('gives up after a second "superseded" and signs out', async () => {
    const renew = vi.fn(() => Promise.resolve(failed(REFRESH_SUPERSEDED)));
    const store = createSessionStore({ renew, wait: noWait });

    expect(await store.renew()).toBe(false);

    expect(renew).toHaveBeenCalledTimes(2);
    expect(store.getState().status).toBe('signedOut');
  });

  it('signs out when the refresh cookie is no longer valid', async () => {
    const store = createSessionStore({ renew: () => Promise.resolve(ok('t1')), wait: noWait });
    await store.renew();
    const listener = vi.fn();
    store.subscribe(listener);

    const expired = createSessionStore({ renew: () => Promise.resolve(failed('identity.refresh_invalid')), wait: noWait });
    expect(await expired.renew()).toBe(false);
    expect(expired.getState().status).toBe('signedOut');
    expect(expired.getAccessToken()).toBeNull();

    store.signOut();
    expect(listener).toHaveBeenCalledOnce();
    expect(store.getAccessToken()).toBeNull();
  });

  it('keeps an existing session when the API cannot be reached', async () => {
    const renew = vi.fn<() => Promise<RenewResult>>()
      .mockResolvedValueOnce(ok('t1'))
      .mockRejectedValueOnce(new TypeError('Failed to fetch'));
    const store = createSessionStore({ renew, wait: noWait });
    await store.renew();

    expect(await store.renew()).toBe(false);

    expect(store.getState().status).toBe('signedIn');
    expect(store.getAccessToken()).toBe('t1');
  });

  it('ends the start-up check signed out when the API cannot be reached', async () => {
    const store = createSessionStore({ renew: () => Promise.reject(new TypeError('Failed to fetch')), wait: noWait });

    expect(await store.renew()).toBe(false);

    expect(store.getState().status).toBe('signedOut');
  });
});
