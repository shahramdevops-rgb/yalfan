import { expect, it } from 'vitest';
import { errorMessage } from '../i18n/fa';
import { ProblemError } from './problem';
import { refusalMessage } from './refusal';

it('prefers the error the API gave the field', () => {
  const error = new ProblemError({
    code: 'validation.failed',
    errors: [{ field: 'name', code: 'validation.required' }],
  });

  expect(refusalMessage(error, 'name')).toBe(errorMessage('validation.required'));
});

it('falls back to the error of the whole request', () => {
  const error = new ProblemError({ code: 'catalog.name_taken' });

  expect(refusalMessage(error, 'name')).toBe(errorMessage('catalog.name_taken'));
  expect(refusalMessage(error)).not.toBe(errorMessage('errors.unknown'));
});
