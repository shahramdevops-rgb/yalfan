// Every API error is an RFC 9457 ProblemDetails with a stable code (ADR 0011). The panel shows
// its own Persian text for the code (shared/i18n/fa.ts), never the API's wording.

export interface FieldProblem {
  /** The JSON name the panel sent, such as "mobileNumber" or "lines[0].quantity". */
  field: string;
  code: string;
  args?: Record<string, unknown> | null;
}

export interface Problem {
  code: string;
  traceId?: string;
  errors?: FieldProblem[];
}

/** The code used when the API could not be reached at all. */
export const NETWORK_UNREACHABLE = 'network.unreachable';

/**
 * A refused request as an exception, for code that must throw: a TanStack Query `queryFn` reports
 * failure only by throwing. It carries the problem, and `toProblem` reads it back.
 */
export class ProblemError extends Error {
  readonly problem: Problem;

  constructor(problem: Problem) {
    super(problem.code);
    this.name = 'ProblemError';
    this.problem = problem;
  }
}

/** Reads the ProblemDetails that openapi-fetch hands back as `error`, or a thrown ProblemError. */
export function toProblem(error: unknown): Problem {
  if (error instanceof ProblemError) {
    return error.problem;
  }

  if (typeof error === 'object' && error !== null && typeof (error as Problem).code === 'string') {
    return error as Problem;
  }

  return { code: 'errors.unknown' };
}

/** The code of the first error for one field, if the API refused that field. */
export function fieldCode(problem: Problem, field: string): string | undefined {
  return problem.errors?.find((error) => error.field === field)?.code;
}
