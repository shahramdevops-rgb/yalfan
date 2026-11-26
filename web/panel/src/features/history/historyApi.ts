import { api } from '../../shared/api/client';
import { unwrap } from '../../shared/api/request';
import type { components } from '../../shared/api/schema';

type Schemas = components['schemas'];

export type HistoryPage = Schemas['HistoryPage'];
export type HistorySave = Schemas['HistorySave'];
export type HistoryChange = Schemas['HistoryChange'];
export type HistoryField = Schemas['HistoryField'];
export type HistoryFieldName = Schemas['HistoryFieldName'];

/** The records with a change history: their list's address in the panel (ADR 0027). */
export type HistoryKind = 'models' | 'contractors' | 'workers';

export const historyKey = (kind: HistoryKind, id: string) => ['history', kind, id] as const;

const at = (id: string, before: string | undefined) => ({ params: { path: { id }, query: { before } } });

/**
 * The owner's change history of a record, twenty saves a page, newest first; `before` is the
 * `before` of the page already shown, for the next, older one. The API refuses anyone else.
 */
export const historyApi: Record<HistoryKind, (id: string, before?: string) => Promise<HistoryPage>> = {
  models: (id, before) => unwrap(api.GET('/panel/history/models/{id}', at(id, before))),
  contractors: (id, before) => unwrap(api.GET('/panel/history/contractors/{id}', at(id, before))),
  workers: (id, before) => unwrap(api.GET('/panel/history/workers/{id}', at(id, before))),
};
