import { api } from '../../shared/api/client';
import { unwrap } from '../../shared/api/request';
import type { components } from '../../shared/api/schema';

export type Stage = components['schemas']['StageSummary'];

const at = (id: string) => ({ params: { path: { id } } });

/** The shared stage list (ADR 0018), in the order of the work (ADR 0022). */
export const stagesApi = {
  list: () => unwrap(api.GET('/panel/production/stages')),
  create: (name: string) => unwrap(api.POST('/panel/production/stages', { body: { name } })),
  rename: (id: string, name: string) => unwrap(api.PUT('/panel/production/stages/{id}', { ...at(id), body: { name } })),
  setActive: (id: string, isActive: boolean) =>
    unwrap(
      isActive
        ? api.POST('/panel/production/stages/{id}/activate', at(id))
        : api.POST('/panel/production/stages/{id}/deactivate', at(id)),
    ),
  /** The whole list in its new order; the API refuses one made from an old copy. */
  reorder: (stageIds: string[]) => unwrap(api.PUT('/panel/production/stages/order', { body: { stageIds } })),
};

export const stagesKey = ['production', 'stages'] as const;
