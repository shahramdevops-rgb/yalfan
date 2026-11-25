import { api } from '../../shared/api/client';
import { unwrap } from '../../shared/api/request';
import type { components } from '../../shared/api/schema';
import type { BagToAdd, NewBag } from './bags';

export type ProductDetails = components['schemas']['ProductDetails'];
export type ProductListItem = components['schemas']['ProductListItem'];
export type ProductPage = components['schemas']['PagedListOfProductListItem'];

/** What the model's page saves together: it edits one and sends the other two as they are. */
export interface ProductProfile {
  name: string;
  description: string | null;
  categoryId: string | null;
}

export interface NewProduct extends ProductProfile {
  sizes: string[];
  variants: NewBag[];
}

const at = (id: string) => ({ params: { path: { id } } });
const atVariant = (id: string, variantId: string) => ({ params: { path: { id, variantId } } });

/**
 * The models (ADR 0016, ADR 0023). Every change on a model's page answers the whole model, so the
 * page shows exactly what was saved, the new codes included.
 */
export const productsApi = {
  search: (search: string, page: number) =>
    unwrap(api.GET('/panel/catalog/products', { params: { query: { search: search || undefined, page } } })),
  get: (id: string) => unwrap(api.GET('/panel/catalog/products/{id}', at(id))),
  create: (product: NewProduct) => unwrap(api.POST('/panel/catalog/products', { body: product })),
  update: (id: string, profile: ProductProfile) =>
    unwrap(api.PUT('/panel/catalog/products/{id}', { ...at(id), body: profile })),
  setActive: (id: string, isActive: boolean) =>
    unwrap(
      isActive
        ? api.POST('/panel/catalog/products/{id}/activate', at(id))
        : api.POST('/panel/catalog/products/{id}/deactivate', at(id)),
    ),
  addVariants: (id: string, variants: BagToAdd[]) =>
    unwrap(api.POST('/panel/catalog/products/{id}/variants', { ...at(id), body: { variants } })),
  setVariantActive: (id: string, variantId: string, isActive: boolean) =>
    unwrap(
      isActive
        ? api.POST('/panel/catalog/products/{id}/variants/{variantId}/activate', atVariant(id, variantId))
        : api.POST('/panel/catalog/products/{id}/variants/{variantId}/deactivate', atVariant(id, variantId)),
    ),
  addSize: (id: string, name: string) =>
    unwrap(api.POST('/panel/catalog/products/{id}/sizes', { ...at(id), body: { name } })),
  renameSize: (id: string, sizeId: string, name: string) =>
    unwrap(
      api.PUT('/panel/catalog/products/{id}/sizes/{sizeId}', { params: { path: { id, sizeId } }, body: { name } }),
    ),
  /** The whole list in its new order; the API refuses one made from an old copy. */
  reorderSizes: (id: string, sizeIds: string[]) =>
    unwrap(api.PUT('/panel/catalog/products/{id}/sizes/order', { ...at(id), body: { sizeIds } })),
};

/** Every search page lives under this key, so one change refreshes them all. */
export const productSearchesKey = ['catalog', 'products'] as const;
export const productSearchKey = (search: string, page: number) => [...productSearchesKey, search, page] as const;
export const productKey = (id: string) => ['catalog', 'product', id] as const;

/**
 * Saving one part of the profile, built from the model as the last change left it: the page sends
 * it through its queue of writes, which hands it that model (ADR 0025).
 */
export const profileChange =
  (change: Partial<ProductProfile>) =>
  (current: ProductDetails): Promise<ProductDetails> =>
    productsApi.update(current.id, profileOf(current, change));

/** The profile as it is now, with one part changed: the page saves one field at a time. */
export const profileOf = (product: ProductDetails, change: Partial<ProductProfile>): ProductProfile => ({
  name: product.name,
  description: product.description ?? null,
  categoryId: product.category?.id ?? null,
  ...change,
});
