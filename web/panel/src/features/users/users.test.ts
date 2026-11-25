import { describe, expect, it } from 'vitest';
import { endsOwnSessions, isLastActiveNumber, numberLock, placeUserRefusal } from './users';
import type { StaffUser } from './usersApi';

function owner(...numbers: [string, boolean][]): StaffUser {
  return {
    id: 'owner-1',
    fullName: 'مالک',
    role: 'owner',
    isActive: true,
    isOwnerAccount: true,
    loginNumbers: numbers.map(([id, isActive]) => ({ id, mobileNumber: `0912000000${id.length}`, isActive })),
    createdAt: '2026-11-26T08:00:00Z',
  };
}

describe('isLastActiveNumber', () => {
  it('locks the only number switched on', () => {
    const user = owner(['a', false], ['b', true]);

    expect(isLastActiveNumber(user, 'b')).toBe(true);
    expect(isLastActiveNumber(user, 'a')).toBe(false);
  });

  it('locks nothing while two numbers are on', () => {
    const user = owner(['a', true], ['b', true]);

    expect(isLastActiveNumber(user, 'a')).toBe(false);
    expect(isLastActiveNumber(user, 'b')).toBe(false);
  });

  it('locks the single number of an account that has one', () => {
    expect(isLastActiveNumber(owner(['a', true]), 'a')).toBe(true);
  });
});

describe('numberLock', () => {
  it("locks a number of one's own account that is off, but not another account's", () => {
    const user = owner(['a', false], ['b', true]);
    const off = user.loginNumbers[0]!;

    expect(numberLock(user, off, 'owner-1')).toBe('own');
    expect(numberLock(user, off, 'developer')).toBeUndefined();
  });

  it('locks the last number on whoever looks', () => {
    const user = owner(['a', false], ['b', true]);
    const on = user.loginNumbers[1]!;

    expect(numberLock(user, on, 'owner-1')).toBe('last');
    expect(numberLock(user, on, 'developer')).toBe('last');
  });

  it('leaves a number free while another is on', () => {
    const user = owner(['a', true], ['b', true]);

    expect(numberLock(user, user.loginNumbers[0]!, 'owner-1')).toBeUndefined();
  });
});

describe('endsOwnSessions', () => {
  it('is true only for the account of the person at the screen', () => {
    const user = owner(['a', true], ['b', true]);

    expect(endsOwnSessions(user, 'owner-1')).toBe(true);
    expect(endsOwnSessions(user, 'developer')).toBe(false);
    expect(endsOwnSessions(user, undefined)).toBe(false);
  });
});

describe('placeUserRefusal', () => {
  it('puts each field error under its field', () => {
    const placed = placeUserRefusal({
      code: 'validation.failed',
      errors: [
        { field: 'fullName', code: 'validation.required' },
        { field: 'mobileNumber', code: 'mobile.invalid' },
      ],
    });

    expect(placed).toEqual({ fullName: 'validation.required', mobileNumber: 'mobile.invalid' });
  });

  it('puts a taken number under the number', () => {
    expect(placeUserRefusal({ code: 'identity.mobile_taken' })).toEqual({ mobileNumber: 'identity.mobile_taken' });
  });

  it('puts what fits no field under the form', () => {
    expect(placeUserRefusal({ code: 'network.unreachable' })).toEqual({ form: 'network.unreachable' });
  });
});
