import { describe, expect, it } from 'vitest';
import { placeRefusal } from './partyForm';

describe('placeRefusal', () => {
  it('puts each field error the API named under its field', () => {
    const problem = {
      code: 'validation.failed',
      errors: [
        { field: 'mobileNumber', code: 'mobile.invalid' },
        { field: 'iban', code: 'iban.invalid' },
      ],
    };

    expect(placeRefusal(problem)).toEqual({ mobileNumber: 'mobile.invalid', iban: 'iban.invalid' });
  });

  it('puts a refusal of the whole request under the field it is about', () => {
    expect(placeRefusal({ code: 'parties.mobile_taken' })).toEqual({ mobileNumber: 'parties.mobile_taken' });
    expect(placeRefusal({ code: 'parties.stage_inactive' })).toEqual({ stageIds: 'parties.stage_inactive' });
  });

  it('shows anything else under the form', () => {
    expect(placeRefusal({ code: 'network.unreachable' })).toEqual({ form: 'network.unreachable' });
  });
});
