import type { SessionStore } from '../session/sessionStore';

/** Routes that answer without a session; a 401 from them is an answer, not an expired token. */
const anonymousRoutes = new Set(['/panel/auth/otp', '/panel/auth/login', '/panel/auth/refresh', '/panel/auth/logout']);

/**
 * The fetch behind the typed API client. It adds the access token, and when the API answers
 * 401 because the 15-minute token expired, it renews the session once and sends the request
 * again. If renewing fails the store is signed out and the 401 is returned as it was.
 */
export function createAuthFetch(
  session: Pick<SessionStore, 'getAccessToken' | 'renew'>,
  send: (request: Request) => Promise<Response> = (request) => fetch(request),
) {
  return async function authFetch(request: Request): Promise<Response> {
    // Kept before sending: a request body can be read only once.
    const retry = request.clone();
    const response = await send(withToken(request, session.getAccessToken()));

    if (response.status !== 401 || anonymousRoutes.has(new URL(request.url).pathname)) {
      return response;
    }

    if (!(await session.renew())) {
      return response;
    }

    return send(withToken(retry, session.getAccessToken()));
  };
}

function withToken(request: Request, token: string | null): Request {
  if (token) {
    request.headers.set('Authorization', `Bearer ${token}`);
  }

  return request;
}
