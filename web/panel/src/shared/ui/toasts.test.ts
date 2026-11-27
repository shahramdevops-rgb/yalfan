import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { createToastStore, toastsAtOnce, toastShownFor } from './toasts';

const messages = (store: ReturnType<typeof createToastStore>) => store.getSnapshot().map((toast) => toast.message);

describe('toasts', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('shows a message and takes it away after its time', () => {
    const store = createToastStore();
    store.show('کپی شد');
    expect(messages(store)).toEqual(['کپی شد']);

    vi.advanceTimersByTime(toastShownFor - 1);
    expect(messages(store)).toEqual(['کپی شد']);
    vi.advanceTimersByTime(1);
    expect(messages(store)).toEqual([]);
  });

  it('tells its readers of every change', () => {
    const store = createToastStore();
    const listener = vi.fn();
    const unsubscribe = store.subscribe(listener);

    store.show('کپی شد');
    vi.advanceTimersByTime(toastShownFor);
    expect(listener).toHaveBeenCalledTimes(2);

    unsubscribe();
    store.show('دوباره');
    expect(listener).toHaveBeenCalledTimes(2);
  });

  it('shows the same message pressed twice once, for longer', () => {
    const store = createToastStore();
    store.show('کپی شد');
    vi.advanceTimersByTime(toastShownFor - 1000);
    store.show('کپی شد');
    expect(messages(store)).toEqual(['کپی شد']);

    vi.advanceTimersByTime(toastShownFor - 1);
    expect(messages(store)).toEqual(['کپی شد']);
    vi.advanceTimersByTime(1);
    expect(messages(store)).toEqual([]);
  });

  it('keeps only the newest few, dropping the oldest first', () => {
    const store = createToastStore();
    const sent = Array.from({ length: toastsAtOnce + 2 }, (_, index) => `پیام ${index}`);
    sent.forEach((message) => store.show(message));
    expect(messages(store)).toEqual(sent.slice(-toastsAtOnce));

    // A dropped message's timer is gone with it: nothing is left behind to fire.
    vi.advanceTimersByTime(toastShownFor);
    expect(messages(store)).toEqual([]);
    expect(vi.getTimerCount()).toBe(0);
  });

  it('keeps every message while the pointer is on one, and starts their time again after', () => {
    const store = createToastStore();
    store.show('کپی شد');
    vi.advanceTimersByTime(toastShownFor - 1);
    store.pause(store.getSnapshot()[0]!.id);
    vi.advanceTimersByTime(toastShownFor * 3);
    expect(messages(store)).toEqual(['کپی شد']);

    // A message shown during the pause waits too.
    store.show('افزوده شد');
    vi.advanceTimersByTime(toastShownFor);
    expect(messages(store)).toEqual(['کپی شد', 'افزوده شد']);

    store.resume();
    vi.advanceTimersByTime(toastShownFor - 1);
    expect(messages(store)).toHaveLength(2);
    vi.advanceTimersByTime(1);
    expect(messages(store)).toEqual([]);
  });

  it('ends the pause when the message under the pointer is pushed out by newer ones', () => {
    // Its pointer can no longer leave it, so waiting for that would keep every message for good.
    const store = createToastStore();
    store.show('یک');
    store.pause(store.getSnapshot()[0]!.id);
    Array.from({ length: toastsAtOnce }, (_, index) => `پیام ${index}`).forEach((message) => store.show(message));
    expect(messages(store)).not.toContain('یک');

    vi.advanceTimersByTime(toastShownFor);
    expect(messages(store)).toEqual([]);
  });

  it('ends the pause when the message under the pointer is taken away', () => {
    const store = createToastStore();
    store.show('یک');
    store.show('دو');
    const first = store.getSnapshot()[0]!.id;
    store.pause(first);
    store.dismiss(first);

    vi.advanceTimersByTime(toastShownFor);
    expect(messages(store)).toEqual([]);
  });

  it('takes a message away at once when asked', () => {
    const store = createToastStore();
    store.show('یک');
    store.show('دو');
    store.dismiss(store.getSnapshot()[0]!.id);
    expect(messages(store)).toEqual(['دو']);
  });
});
