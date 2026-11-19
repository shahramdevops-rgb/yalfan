import createClient from 'openapi-fetch';
import { createSessionStore, type RenewResult } from '../session/sessionStore';
import { createAuthFetch } from './authFetch';
import { toProblem } from './problem';
import type { paths } from './schema';

// The API is on the panel's own origin (ADR 0014), and the browser sends the refresh cookie
// with same-origin requests by itself.
const baseUrl = window.location.origin;

/** For the login routes, which answer without a session. */
export const publicApi = createClient<paths>({ baseUrl });

async function renewWithCookie(): Promise<RenewResult> {
  const { data, error } = await publicApi.POST('/panel/auth/refresh');
  return data ? { ok: true, session: data } : { ok: false, code: toProblem(error).code };
}

/** The one session of this tab. */
export const sessionStore = createSessionStore({ renew: renewWithCookie });

/** For everything behind the login: adds the token and renews it when it expires. */
export const api = createClient<paths>({ baseUrl, fetch: createAuthFetch(sessionStore) });
