import { NETWORK_UNREACHABLE, ProblemError, toProblem } from './problem';

/**
 * The data of an openapi-fetch answer. A refused request, or a network that failed before any
 * answer came, becomes a ProblemError, so TanStack Query reports it and `toProblem` reads it back.
 */
export async function unwrap<T>(request: Promise<{ data?: T; error?: unknown }>): Promise<T> {
  let answer: { data?: T; error?: unknown };
  try {
    answer = await request;
  } catch {
    // fetch itself failed: no network, or the API is down.
    throw new ProblemError({ code: NETWORK_UNREACHABLE });
  }

  if (answer.data === undefined) {
    throw new ProblemError(toProblem(answer.error));
  }

  return answer.data;
}
