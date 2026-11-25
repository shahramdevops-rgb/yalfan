import { useQueryClient, type QueryKey } from '@tanstack/react-query';
import { useRef } from 'react';

/** The end of the line of writes waiting their turn. */
export interface WriteQueue {
  current: Promise<unknown>;
}

/**
 * Runs `task` once every task queued before it has finished, whether it succeeded or failed, and
 * answers its result. A failed task does not stop the ones after it.
 */
export function inTurn<T>(queue: WriteQueue, task: () => Promise<T>): Promise<T> {
  const next = queue.current.then(task);
  queue.current = next.catch(() => undefined);
  return next;
}

/** Sends one change of a record and answers the record as the API saved it. */
export type Write<T> = (send: (current: T) => Promise<T>) => Promise<T>;

/**
 * Writes of one record in turn: each `send` gets the record as `read` gives it when its turn
 * comes, which is what the last write stored, not what the page drew when the change was made.
 */
export function createSerialWrite<T>(queue: WriteQueue, read: () => T, store: (value: T) => void): Write<T> {
  return (send) =>
    inTurn(queue, async () => {
      const value = await send(read());
      store(value);
      return value;
    });
}

/**
 * The changes of one record on its page, sent one after another, each built from the record as
 * the last change left it, and each answer put in the query cache. Why: a page that saves every
 * field at once sends the whole record (a profile, a model's name, description and category), and
 * leaving one field by touching another starts two saves in one gesture. Sent together, each
 * would carry the other's field as it was; the page then shows the later answer, which lacks the
 * earlier change, and the next save writes that back to the database (ADR 0025).
 */
export function useSerialWrites<T>(key: QueryKey, saved?: (value: T) => void): Write<T> {
  const queryClient = useQueryClient();
  const queue = useRef<Promise<unknown>>(Promise.resolve());

  // The queue is reached only when a change is sent, never while the page draws.
  return (send) =>
    createSerialWrite<T>(
      queue,
      // The page draws only once the record is loaded, so a change never comes before it.
      () => queryClient.getQueryData<T>(key) as T,
      (value) => {
        queryClient.setQueryData(key, value);
        saved?.(value);
      },
    )(send);
}
