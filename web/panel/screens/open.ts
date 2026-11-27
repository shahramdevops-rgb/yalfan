import { expect, type Page } from '@playwright/test';
import { answerApi } from './api';
import * as sample from './sample';

/**
 * Loaded: what the page is about is on it, and no grey placeholder or splash is left. `shows` is
 * something from the sample data the page shows only once its data has loaded; null only for a
 * page that loads none, which waits for its main heading instead.
 */
export async function waitUntilLoaded(page: Page, shows: string | null) {
  const main =
    shows === null ? page.getByRole('heading', { level: 1 }) : page.getByText(shows, { exact: true }).first();
  await expect(main).toBeVisible();
  await expect(page.locator('.animate-pulse')).toHaveCount(0);
  await page.waitForLoadState('networkidle');
  await page.evaluate(() => document.fonts.ready);
}

/**
 * Opens a page against the sample API at the frozen moment, as the screenshots do (ADR 0028), and
 * runs a behaviour test's steps on it (ADR 0030). A request the sample API has no answer for, or an
 * error thrown in the page, fails the test by name, in place of the symptom it caused.
 */
export async function onPage(page: Page, path: string, shows: string, steps: () => Promise<void>) {
  const pageErrors: string[] = [];
  page.on('pageerror', (error) => pageErrors.push(error.message));
  await page.clock.setFixedTime(sample.now);
  const unanswered = await answerApi(page, { signedIn: true });

  try {
    await page.goto(path);
    await waitUntilLoaded(page, shows);
    await steps();
  } finally {
    expect(unanswered, `requests the sample API has no answer for: ${unanswered.join(', ')}`).toEqual([]);
    expect(pageErrors, `errors thrown in the page: ${pageErrors.join(' | ')}`).toEqual([]);
  }
}

/** Every request the page sends to the API, as «METHOD path», to tell what a step sent or did not. */
export function recordRequests(page: Page): string[] {
  const sent: string[] = [];
  page.on('request', (request) => {
    const url = new URL(request.url());
    if (url.pathname.startsWith('/panel/')) {
      sent.push(`${request.method()} ${url.pathname}`);
    }
  });
  return sent;
}
