import { publicApi, sessionStore } from '../../shared/api/client';
import { NETWORK_UNREACHABLE, toProblem, type Problem } from '../../shared/api/problem';

export type ApiResult<T> = { ok: true; value: T } | { ok: false; problem: Problem };

/** Asks for a login code. The answer is the same for every number (ADR 0012). */
export async function requestLoginCode(mobileNumber: string): Promise<ApiResult<{ resendAfterSeconds: number }>> {
  return call(async () => {
    const { data, error } = await publicApi.POST('/panel/auth/otp', { body: { mobileNumber } });
    return data
      ? { ok: true, value: { resendAfterSeconds: data.resendAfterSeconds } }
      : { ok: false, problem: toProblem(error) };
  });
}

/**
 * Development only: the login code the API kept instead of sending an SMS, shown on the login
 * page until the SMS provider is connected (task 3.0, ADR 0012). The route exists only in a
 * Development API and is not in the contract, so this is a plain fetch; in a production build
 * `import.meta.env.DEV` is false and the call is dropped from the bundle.
 */
export async function developmentLoginCode(mobileNumber: string): Promise<string | undefined> {
  if (!import.meta.env.DEV) {
    return undefined;
  }

  try {
    const response = await fetch('/panel/auth/dev/otp', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ mobileNumber }),
    });
    const body = response.ok ? ((await response.json()) as { code?: unknown }) : undefined;
    return typeof body?.code === 'string' ? body.code : undefined;
  } catch {
    return undefined;
  }
}

/** Logs in with the code; on success the session store holds the new session. */
export async function login(mobileNumber: string, code: string): Promise<ApiResult<void>> {
  return call(async () => {
    const { data, error } = await publicApi.POST('/panel/auth/login', { body: { mobileNumber, code } });
    if (!data) {
      return { ok: false, problem: toProblem(error) };
    }

    sessionStore.signIn(data);
    return { ok: true, value: undefined };
  });
}

/** Ends the session on the API (the refresh cookie is revoked and deleted) and in this tab. */
export async function logout(): Promise<void> {
  try {
    await publicApi.POST('/panel/auth/logout');
  } finally {
    sessionStore.signOut();
  }
}

async function call<T>(send: () => Promise<ApiResult<T>>): Promise<ApiResult<T>> {
  try {
    return await send();
  } catch {
    // fetch itself failed: no network, or the API is down.
    return { ok: false, problem: { code: NETWORK_UNREACHABLE } };
  }
}
