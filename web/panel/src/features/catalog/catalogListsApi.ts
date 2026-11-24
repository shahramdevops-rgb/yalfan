import { api } from '../../shared/api/client';
import { unwrap } from '../../shared/api/request';
import type { components } from '../../shared/api/schema';

export type CatalogListItem = components['schemas']['CatalogListItem'];

/** The three lists a model's variants and its category are picked from (ADR 0016). */
export const catalogKinds = ['fabrics', 'colours', 'categories'] as const;
export type CatalogKind = (typeof catalogKinds)[number];

export const isCatalogKind = (value: string | undefined): value is CatalogKind =>
  catalogKinds.includes(value as CatalogKind);

interface ListEndpoints {
  list: () => Promise<CatalogListItem[]>;
  create: (name: string) => Promise<CatalogListItem>;
  rename: (id: string, name: string) => Promise<CatalogListItem>;
  setActive: (id: string, isActive: boolean) => Promise<CatalogListItem>;
}

const at = (id: string) => ({ params: { path: { id } } });

// Written out per list rather than built from the kind, so every call keeps its own type from
// the contract; the three have the same shape today, but nothing forces them to stay alike.
export const catalogLists: Record<CatalogKind, ListEndpoints> = {
  fabrics: {
    list: () => unwrap(api.GET('/panel/catalog/fabrics')),
    create: (name) => unwrap(api.POST('/panel/catalog/fabrics', { body: { name } })),
    rename: (id, name) => unwrap(api.PUT('/panel/catalog/fabrics/{id}', { ...at(id), body: { name } })),
    setActive: (id, isActive) =>
      unwrap(
        isActive
          ? api.POST('/panel/catalog/fabrics/{id}/activate', at(id))
          : api.POST('/panel/catalog/fabrics/{id}/deactivate', at(id)),
      ),
  },
  colours: {
    list: () => unwrap(api.GET('/panel/catalog/colours')),
    create: (name) => unwrap(api.POST('/panel/catalog/colours', { body: { name } })),
    rename: (id, name) => unwrap(api.PUT('/panel/catalog/colours/{id}', { ...at(id), body: { name } })),
    setActive: (id, isActive) =>
      unwrap(
        isActive
          ? api.POST('/panel/catalog/colours/{id}/activate', at(id))
          : api.POST('/panel/catalog/colours/{id}/deactivate', at(id)),
      ),
  },
  categories: {
    list: () => unwrap(api.GET('/panel/catalog/categories')),
    create: (name) => unwrap(api.POST('/panel/catalog/categories', { body: { name } })),
    rename: (id, name) => unwrap(api.PUT('/panel/catalog/categories/{id}', { ...at(id), body: { name } })),
    setActive: (id, isActive) =>
      unwrap(
        isActive
          ? api.POST('/panel/catalog/categories/{id}/activate', at(id))
          : api.POST('/panel/catalog/categories/{id}/deactivate', at(id)),
      ),
  },
};

export const catalogListKey = (kind: CatalogKind) => ['catalog', kind] as const;
