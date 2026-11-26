import { expect, test, type Page } from '@playwright/test';
import { answerApi } from './api';
import * as sample from './sample';

interface Screen {
  /** The picture's file name, before the width: `models` becomes `models-phone.png`. */
  name: string;
  path: string;
  signedIn?: boolean;
  /**
   * Something from the sample data the page shows only once its data has loaded. Null only for a
   * page that loads none, which waits for its main heading instead.
   */
  shows: string | null;
}

// The main pages of the panel (ADR 0028). A page added to the panel is added here too.
const screens: Screen[] = [
  { name: 'login', path: '/login', signedIn: false, shows: null },
  { name: 'dashboard', path: '/', shows: sample.owner.fullName },
  { name: 'models', path: '/models', shows: sample.models.items[2]!.name },
  { name: 'model', path: `/models/${sample.model.id}`, shows: sample.model.name },
  // A fabric to tick: the lists of fabrics and colours have loaded.
  { name: 'model-new', path: '/models/new', shows: sample.fabrics[0]!.name },
  { name: 'lists', path: '/lists/fabrics', shows: sample.fabrics[0]!.name },
  { name: 'stages', path: '/stages', shows: sample.stages[0]!.name },
  { name: 'contractors', path: '/contractors', shows: sample.contractorList[0]!.fullName },
  { name: 'contractor', path: `/contractors/${sample.contractorDetails.id}`, shows: sample.contractorDetails.fullName },
  // A stage to tick: the stages have loaded.
  { name: 'contractor-new', path: '/contractors/new', shows: sample.stages[0]!.name },
  // Who made a save: the history itself has loaded, not only its heading.
  {
    name: 'contractor-history',
    path: `/contractors/${sample.contractorDetails.id}/history`,
    shows: sample.contractorHistory.saves[1]!.by!,
  },
  { name: 'workers', path: '/workers', shows: sample.workerList[0]!.fullName },
  { name: 'worker', path: `/workers/${sample.workerDetails.id}`, shows: sample.workerDetails.fullName },
  { name: 'users', path: '/users', shows: sample.users[2]!.fullName },
];

/** Loaded: what the page is about is on it, and no grey placeholder or splash is left. */
async function waitUntilLoaded(page: Page, screen: Screen) {
  const main =
    screen.shows === null
      ? page.getByRole('heading', { level: 1 })
      : page.getByText(screen.shows, { exact: true }).first();
  await expect(main).toBeVisible();
  await expect(page.locator('.animate-pulse')).toHaveCount(0);
  await page.waitForLoadState('networkidle');
  await page.evaluate(() => document.fonts.ready);
}

for (const screen of screens) {
  test(screen.name, async ({ page }) => {
    const pageErrors: string[] = [];
    page.on('pageerror', (error) => pageErrors.push(error.message));
    await page.clock.setFixedTime(sample.now);
    const unanswered = await answerApi(page, { signedIn: screen.signedIn ?? true });

    try {
      await page.goto(screen.path);
      await waitUntilLoaded(page, screen);
    } finally {
      // The cause, when there is one, is reported instead of its symptom (a page that never
      // showed its data), with the request or the error named.
      expect(unanswered, `requests the sample API has no answer for: ${unanswered.join(', ')}`).toEqual([]);
      expect(pageErrors, `errors thrown in the page: ${pageErrors.join(' | ')}`).toEqual([]);
    }

    await expect(page).toHaveScreenshot(`${screen.name}.png`, { fullPage: true });
  });
}
