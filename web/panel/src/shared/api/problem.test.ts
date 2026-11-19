import { describe, expect, it } from 'vitest';
import { ProblemError, toProblem } from './problem';

describe('toProblem', () => {
  it('reads the problem back from a thrown ProblemError', () => {
    const problem = { code: 'identity.code_expired', traceId: 'abc' };

    expect(toProblem(new ProblemError(problem))).toBe(problem);
  });

  it('reads a ProblemDetails body as it is', () => {
    expect(toProblem({ code: 'validation.failed', errors: [] })).toEqual({ code: 'validation.failed', errors: [] });
  });

  it('gives the unknown code for anything else', () => {
    expect(toProblem(new TypeError('Failed to fetch'))).toEqual({ code: 'errors.unknown' });
    expect(toProblem(undefined)).toEqual({ code: 'errors.unknown' });
  });
});
