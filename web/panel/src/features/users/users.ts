import { fieldCode, type Problem } from '../../shared/api/problem';
import type { LoginNumber, StaffRole, StaffUser } from './usersApi';

/** The two roles a user is given in the panel, the smaller first, as the new-user form offers them. */
export const staffRoles: readonly StaffRole[] = ['operator', 'manager'];

/**
 * Whether this number is the account's last one switched on. Its switch is locked, because the
 * API refuses to leave an account with no number to log in with (ADR 0026).
 */
export function isLastActiveNumber(user: StaffUser, numberId: string): boolean {
  const active = user.loginNumbers.filter((number) => number.isActive);
  return active.length === 1 && active[0]!.id === numberId;
}

/**
 * Why this number's switch cannot be moved, if it cannot: the last number on of an account stays
 * on, and a number of one's own account that is off is switched back on only from another owner
 * account, so a thief whose page on the stolen phone still works cannot switch it on (ADR 0026).
 */
export function numberLock(
  user: StaffUser,
  number: LoginNumber,
  currentUserId: string | undefined,
): 'last' | 'own' | undefined {
  if (isLastActiveNumber(user, number.id)) {
    return 'last';
  }

  return !number.isActive && user.id === currentUserId ? 'own' : undefined;
}

/**
 * Whether switching a number off logs the person at this screen out: the API ends every session
 * of the account whose number it is, including his own when it is his account.
 */
export function endsOwnSessions(user: StaffUser, currentUserId: string | undefined): boolean {
  return user.id === currentUserId;
}

/** The fields of the new-user form, by the names the API gives them. */
export const userFields = ['fullName', 'mobileNumber', 'role'] as const;
export type UserField = (typeof userFields)[number];

/** Codes the API answers for the whole request that belong under one field of the form. */
const fieldOfCode: Record<string, UserField> = {
  'identity.full_name_required': 'fullName',
  'identity.full_name_too_long': 'fullName',
  'identity.mobile_taken': 'mobileNumber',
  'mobile.invalid': 'mobileNumber',
  'identity.role_not_allowed': 'role',
};

/**
 * Where each refusal of a new user is shown: the code of each field the API named, or the
 * request's own code under the field it is about (a taken number under the number). What fits no
 * field is shown under the form, as `form`.
 */
export function placeUserRefusal(problem: Problem): Partial<Record<UserField | 'form', string>> {
  const placed: Partial<Record<UserField | 'form', string>> = {};
  for (const field of userFields) {
    const code = fieldCode(problem, field);
    if (code) {
      placed[field] = code;
    }
  }

  if (Object.keys(placed).length > 0) {
    return placed;
  }

  return { [fieldOfCode[problem.code] ?? 'form']: problem.code };
}
