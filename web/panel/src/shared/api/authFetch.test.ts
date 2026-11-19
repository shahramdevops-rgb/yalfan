import { describe, expect, it, vi } from 'vitest';
import { createAuthFetch } from './authFetch';

const api = 'http://panel.test';

function fakeSession(tokens: string[], renewSucceeds = true) {
  let current = tokens.shift() ?? null;
  return {
    getAccessToken: () => current,
    renew: vi.fn(() => {
      if (renewSucceeds) {
        current = tokens.shift() ?? null;
      }
      return Promise.resolve(renewSucceeds);
    }),
  };
}

/** A fake API that accepts only the given token. */
function apiAccepting(validToken: string) {
  const seen: { path: string; authorization: string | null; body: string }[] = [];
  const send = vi.fn(async (request: Request) => {
    const authorization = request.headers.get('Authorization');
    seen.push({ path: new URL(request.url).pathname, authorization, body: await request.text() });
    return new Response(null, { status: authorization === `Bearer ${validToken}` ? 200 : 401 });
  });
  return { send, seen };
}

describe('authFetch', () => {
  it('sends the access token', async () => {
    const { send, seen } = apiAccepting('t1');
    const authFetch = createAuthFetch(fakeSession(['t1']), send);

    const response = await authFetch(new Request(`${api}/panel/auth/me`));

    expect(response.status).toBe(200);
    expect(seen[0]!.authorization).toBe('Bearer t1');
  });

  it('renews an expired token once and sends the request again, body included', async () => {
    const { send, seen } = apiAccepting('t2');
    const session = fakeSession(['t1', 't2']);
    const authFetch = createAuthFetch(session, send);

    const response = await authFetch(
      new Request(`${api}/panel/users`, { method: 'POST', body: '{"fullName":"Zahra"}' }),
    );

    expect(response.status).toBe(200);
    expect(session.renew).toHaveBeenCalledOnce();
    expect(seen.map((call) => call.authorization)).toEqual(['Bearer t1', 'Bearer t2']);
    expect(seen[1]!.body).toBe('{"fullName":"Zahra"}');
  });

  it('returns the 401 when renewing fails, without sending again', async () => {
    const { send } = apiAccepting('t2');
    const session = fakeSession(['t1'], false);
    const authFetch = createAuthFetch(session, send);

    const response = await authFetch(new Request(`${api}/panel/auth/me`));

    expect(response.status).toBe(401);
    expect(send).toHaveBeenCalledOnce();
  });

  it('never renews for a 401 from the login routes, where it means a wrong code', async () => {
    const { send } = apiAccepting('never');
    const session = fakeSession([]);
    const authFetch = createAuthFetch(session, send);

    const response = await authFetch(new Request(`${api}/panel/auth/login`, { method: 'POST', body: '{}' }));

    expect(response.status).toBe(401);
    expect(session.renew).not.toHaveBeenCalled();
  });
});
