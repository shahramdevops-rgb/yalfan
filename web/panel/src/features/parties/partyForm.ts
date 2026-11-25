import { fieldCode, type Problem } from '../../shared/api/problem';

/** How many Jalali years back a worker's start date offers: a worker may have been here for decades. */
export const startYearsBack = 40;

/** The fields of the new contractor and new worker forms, by the names the API gives them. */
export const partyFields = ['fullName', 'mobileNumber', 'stageIds', 'startDate', 'notes', 'iban'] as const;
export type PartyField = (typeof partyFields)[number];

/** Codes the API answers for the whole request that belong under one field of the form. */
const fieldOfCode: Record<string, PartyField> = {
  'parties.full_name_required': 'fullName',
  'parties.full_name_too_long': 'fullName',
  'parties.mobile_taken': 'mobileNumber',
  'mobile.invalid': 'mobileNumber',
  'parties.stage_required': 'stageIds',
  'parties.stage_not_found': 'stageIds',
  'parties.stage_inactive': 'stageIds',
  'parties.start_date_in_future': 'startDate',
  'parties.notes_too_long': 'notes',
  'iban.invalid': 'iban',
};

/**
 * Where each refusal of a new contractor or worker is shown: the code of each field the API
 * named, or the request's own code under the field it is about (a taken mobile under the mobile).
 * What fits no field is shown under the form, as `form`.
 */
export function placeRefusal(problem: Problem): Partial<Record<PartyField | 'form', string>> {
  const placed: Partial<Record<PartyField | 'form', string>> = {};
  for (const field of partyFields) {
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
