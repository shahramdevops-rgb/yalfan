import type { components } from '../api/schema';

export type UserSummary = components['schemas']['UserSummary'];
export type SessionResponse = components['schemas']['SessionResponse'];

export type SessionState = { status: 'loading' } | { status: 'signedIn'; user: UserSummary } | { status: 'signedOut' };

/** What one call to /panel/auth/refresh gave: a new session, or the error code. */
export type RenewResult = { ok: true; session: SessionResponse } | { ok: false; code: string | undefined };

export const REFRESH_SUPERSEDED = 'identity.refresh_superseded';

export interface SessionStoreOptions {
  /** Asks the API for a new session with the refresh cookie. */
  renew: () => Promise<RenewResult>;
  /** Waits before the one retry after "superseded"; tests replace it. */
  wait?: (milliseconds: number) => Promise<void>;
}

/** Long enough for the other tab's response, with the new cookie, to arrive. */
const SUPERSEDED_RETRY_DELAY_MS = 1000;

const sleep = (milliseconds: number) => new Promise<void>((resolve) => setTimeout(resolve, milliseconds));

/**
 * The signed-in user and the access token. The token lives only in this closure, never in
 * localStorage, so an injected script cannot read it from storage (ADR 0012). After a reload
 * it is gone, and renew() gets a new one with the HttpOnly refresh cookie.
 */
export function createSessionStore({ renew: callRenew, wait = sleep }: SessionStoreOptions) {
  let state: SessionState = { status: 'loading' };
  let accessToken: string | null = null;
  let renewing: Promise<boolean> | null = null;
  const listeners = new Set<() => void>();

  function setState(next: SessionState) {
    state = next;
    listeners.forEach((listener) => listener());
  }

  function signIn(session: SessionResponse) {
    accessToken = session.accessToken;
    setState({ status: 'signedIn', user: session.user });
  }

  function signOut() {
    accessToken = null;
    setState({ status: 'signedOut' });
  }

  async function renewOnce(): Promise<boolean> {
    let result: RenewResult;
    try {
      result = await callRenew();
      if (!result.ok && result.code === REFRESH_SUPERSEDED) {
        // Another tab renewed with the same cookie a moment ago. Its response carries the new
        // cookie, so one more try after a short wait succeeds (ADR 0012).
        await wait(SUPERSEDED_RETRY_DELAY_MS);
        result = await callRenew();
      }
    } catch {
      // The API could not be reached. The cookie may still be good, so a session that exists
      // is kept; only the first check at start-up has to end somewhere, and it ends signed out.
      if (state.status === 'loading') {
        signOut();
      }
      return false;
    }

    if (result.ok) {
      signIn(result.session);
      return true;
    }

    signOut();
    return false;
  }

  return {
    getState: () => state,
    getAccessToken: () => accessToken,
    // Arrow functions, like every member here: React and the API client call them detached
    // from the store (useSyncExternalStore(store.subscribe, ...)).
    subscribe: (listener: () => void) => {
      listeners.add(listener);
      return () => {
        listeners.delete(listener);
      };
    },
    signIn,
    signOut,
    /**
     * One renewal at a time: callers that arrive while one is running share it, so a refresh
     * token that was just rotated is never sent a second time.
     */
    renew: (): Promise<boolean> => {
      renewing ??= renewOnce().finally(() => {
        renewing = null;
      });
      return renewing;
    },
  };
}

export type SessionStore = ReturnType<typeof createSessionStore>;
