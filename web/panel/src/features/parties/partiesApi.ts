import { api } from '../../shared/api/client';
import { unwrap } from '../../shared/api/request';
import type { components } from '../../shared/api/schema';
import type { Write as WriteOf } from '../../shared/api/serialWrites';

type Schemas = components['schemas'];

/** The two lists of people the workshop pays: outside contractors (ADR 0019) and in-house workers (ADR 0020). */
export type PartyKind = 'contractors' | 'workers';

export type ContractorDetails = Schemas['ContractorDetails'];
export type WorkerDetails = Schemas['WorkerDetails'];
/** A contractor or a worker as the profile page shows them; only a worker has a start date. */
export type PartyDetails = ContractorDetails | WorkerDetails;
export type PartyListItem = Schemas['ContractorListItem'] | Schemas['WorkerListItem'];
export type PartyPage = Schemas['PagedListOfContractorListItem'] | Schemas['PagedListOfWorkerListItem'];

/** What the profile request saves together: the page changes one part and sends the rest as it is. */
export interface PartyProfile {
  fullName: string;
  mobileNumber: string;
  stageIds: string[];
  notes: string | null;
  /** A worker's only; a contractor has none. */
  startDate?: string;
}

export interface NewParty extends PartyProfile {
  /** The 24 digits after IR, or null for none. */
  iban: string | null;
}

/** Sends one change of a person on their page, in turn with the page's other changes. */
export type Write = WriteOf<PartyDetails>;

/** The requests of one list. The IBAN has its own requests, so saving a name never touches it (ADR 0019). */
export interface PartyApi {
  search: (search: string, withInactive: boolean, page: number) => Promise<PartyPage>;
  get: (id: string) => Promise<PartyDetails>;
  create: (party: NewParty) => Promise<PartyDetails>;
  update: (id: string, profile: PartyProfile) => Promise<PartyDetails>;
  setActive: (id: string, isActive: boolean) => Promise<PartyDetails>;
  setIban: (id: string, iban: string) => Promise<PartyDetails>;
  removeIban: (id: string) => Promise<PartyDetails>;
  /** The whole number, for the owner only; the API refuses everyone else and logs each read. */
  wholeIban: (id: string) => Promise<string>;
}

const at = (id: string) => ({ params: { path: { id } } });

/** The list filter: only the active ones unless the inactive ones are asked for too. */
const activeFilter = (withInactive: boolean) => (withInactive ? undefined : true);

const contractors: PartyApi = {
  search: (search, withInactive, page) =>
    unwrap(
      api.GET('/panel/parties/contractors', {
        params: { query: { search: search || undefined, isActive: activeFilter(withInactive), page } },
      }),
    ),
  get: (id) => unwrap(api.GET('/panel/parties/contractors/{id}', at(id))),
  create: ({ fullName, mobileNumber, stageIds, notes, iban }) =>
    unwrap(api.POST('/panel/parties/contractors', { body: { fullName, mobileNumber, stageIds, notes, iban } })),
  update: (id, { fullName, mobileNumber, stageIds, notes }) =>
    unwrap(
      api.PUT('/panel/parties/contractors/{id}', { ...at(id), body: { fullName, mobileNumber, stageIds, notes } }),
    ),
  setActive: (id, isActive) =>
    unwrap(
      isActive
        ? api.POST('/panel/parties/contractors/{id}/activate', at(id))
        : api.POST('/panel/parties/contractors/{id}/deactivate', at(id)),
    ),
  setIban: (id, iban) => unwrap(api.PUT('/panel/parties/contractors/{id}/iban', { ...at(id), body: { iban } })),
  removeIban: (id) => unwrap(api.DELETE('/panel/parties/contractors/{id}/iban', at(id))),
  wholeIban: async (id) => (await unwrap(api.GET('/panel/parties/contractors/{id}/iban', at(id)))).iban,
};

const workers: PartyApi = {
  search: (search, withInactive, page) =>
    unwrap(
      api.GET('/panel/parties/workers', {
        params: { query: { search: search || undefined, isActive: activeFilter(withInactive), page } },
      }),
    ),
  get: (id) => unwrap(api.GET('/panel/parties/workers/{id}', at(id))),
  create: ({ fullName, mobileNumber, stageIds, notes, iban, startDate }) =>
    unwrap(
      api.POST('/panel/parties/workers', {
        body: { fullName, mobileNumber, stageIds, notes, iban, startDate: startDate ?? null },
      }),
    ),
  update: (id, { fullName, mobileNumber, stageIds, notes, startDate }) =>
    unwrap(
      api.PUT('/panel/parties/workers/{id}', {
        ...at(id),
        body: { fullName, mobileNumber, stageIds, notes, startDate: startDate ?? null },
      }),
    ),
  setActive: (id, isActive) =>
    unwrap(
      isActive
        ? api.POST('/panel/parties/workers/{id}/activate', at(id))
        : api.POST('/panel/parties/workers/{id}/deactivate', at(id)),
    ),
  setIban: (id, iban) => unwrap(api.PUT('/panel/parties/workers/{id}/iban', { ...at(id), body: { iban } })),
  removeIban: (id) => unwrap(api.DELETE('/panel/parties/workers/{id}/iban', at(id))),
  wholeIban: async (id) => (await unwrap(api.GET('/panel/parties/workers/{id}/iban', at(id)))).iban,
};

export const partiesApi: Record<PartyKind, PartyApi> = { contractors, workers };

/** Every search page of a list lives under this key, so one change refreshes them all. */
export const partySearchesKey = (kind: PartyKind) => ['parties', kind, 'search'] as const;
export const partySearchKey = (kind: PartyKind, search: string, withInactive: boolean, page: number) =>
  [...partySearchesKey(kind), search, withInactive, page] as const;
export const partyKey = (kind: PartyKind, id: string) => ['parties', kind, 'one', id] as const;

/** The start date, when the details are a worker's. */
export const startDateOf = (party: PartyDetails): string | undefined =>
  'startDate' in party ? party.startDate : undefined;

/** The profile as it is now, with one part changed: the page saves one field at a time. */
export const profileOf = (party: PartyDetails, change: Partial<PartyProfile>): PartyProfile => ({
  fullName: party.fullName,
  mobileNumber: party.mobileNumber,
  stageIds: party.stages.map((stage) => stage.id),
  notes: party.notes ?? null,
  startDate: startDateOf(party),
  ...change,
});
