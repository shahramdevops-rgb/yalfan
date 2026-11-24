import { errorMessage } from '../i18n/fa';
import { fieldCode, toProblem } from './problem';

/**
 * The panel's message for a refused request: the error of `field` when the API named one for
 * it (a validation error on the name, say), otherwise the error of the whole request.
 */
export function refusalMessage(error: unknown, field?: string): string {
  const problem = toProblem(error);
  return errorMessage((field && fieldCode(problem, field)) || problem.code);
}

/**
 * Runs a request and hands its answer to `done`. Answers the message to show when it is
 * refused, or nothing, which is what the list screens' fields expect.
 */
export async function attempt<T>(send: () => Promise<T>, done: (value: T) => void, field?: string) {
  try {
    done(await send());
    return undefined;
  } catch (error) {
    return refusalMessage(error, field);
  }
}
