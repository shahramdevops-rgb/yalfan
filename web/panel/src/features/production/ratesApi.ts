import { api } from '../../shared/api/client';
import { unwrap } from '../../shared/api/request';
import type { components } from '../../shared/api/schema';

export type RateCard = components['schemas']['RateCardDetails'];
export type StageRate = components['schemas']['StageRate'];
export type RateEntry = components['schemas']['PieceRateEntry'];

export interface NewRate {
  productId: string;
  stageId: string;
  /** Rial for one piece, a whole number above zero (ADR 0021). */
  amount: number;
  /** A calendar day, "2026-11-25"; it may be in the past or the future. */
  startsOn: string;
}

/** The contractors' piece rates of a model (ADR 0021): append-only, so there is no edit and no delete. */
export const ratesApi = {
  /** Every active stage with the rate in force on the workshop's today, which the answer names. */
  card: (productId: string) => unwrap(api.GET('/panel/production/rates', { params: { query: { productId } } })),
  /** Every entry of the model, grouped by stage in the order of the work, newest first. */
  history: (productId: string) =>
    unwrap(api.GET('/panel/production/rates/history', { params: { query: { productId } } })),
  add: (rate: NewRate) => unwrap(api.POST('/panel/production/rates', { body: rate })),
};

/** The card and the history of a model live under this key, so one new rate refreshes both. */
export const ratesKey = (productId: string) => ['production', 'rates', productId] as const;
export const rateCardKey = (productId: string) => [...ratesKey(productId), 'card'] as const;
export const rateHistoryKey = (productId: string) => [...ratesKey(productId), 'history'] as const;
