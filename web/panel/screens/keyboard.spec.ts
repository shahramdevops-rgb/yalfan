import { expect, test, type Page } from '@playwright/test';
import { t } from '../src/shared/i18n/fa';
import { onPage } from './open';
import * as sample from './sample';

// Behaviour tests of the keyboard (ADR 0031): Enter goes forward, a refusal puts the focus on the
// wrong field, a small form gives the focus back to its line, and an iPhone, where a tapped button
// is not focused, gets the focus back too. Behaviour without a picture, so one width is enough. The
// test names are one word each, which is how scripts/check.ps1 names a failed one.

test.skip(({ isMobile }) => isMobile, 'behaviour without a picture: one width is enough');

const [cutting, sewing, fitting] = sample.stages;
const contractor = sample.contractorDetails;

/** The bodies of the requests sent to one route, in the order they were sent. */
function recordBodies(page: Page, method: string, path: string): unknown[] {
  const bodies: unknown[] = [];
  page.on('request', (request) => {
    if (request.method() === method && new URL(request.url()).pathname === path) {
      bodies.push(request.postDataJSON());
    }
  });
  return bodies;
}

const chip = (page: Page, name: string) => page.getByRole('button', { name, exact: true });

test('keyboard-contractor', async ({ page }) => {
  const sent = recordBodies(page, 'POST', '/panel/parties/contractors');

  // A whole new contractor with the keyboard alone, from the first field to the save.
  await onPage(page, '/contractors/new', cutting!.name, async () => {
    const name = page.getByLabel(t('parties.fullName'));
    await expect(name).toBeFocused();
    // A phone's keyboard names its Enter key «بعدی», as it moves on.
    await expect(name).toHaveAttribute('enterkeyhint', 'next');
    await page.keyboard.type('حسن مرادی');
    await page.keyboard.press('Enter');

    await expect(page.getByLabel(t('parties.mobile'))).toBeFocused();
    await page.keyboard.type('09120000777');
    await page.keyboard.press('Enter');

    // The stages are one field: Tab walks the chips, Space ticks one, Enter leaves them all.
    await expect(chip(page, cutting!.name)).toBeFocused();
    await page.keyboard.press('Tab');
    await page.keyboard.press('Space');
    await expect(chip(page, sewing!.name)).toHaveAttribute('aria-pressed', 'true');
    await page.keyboard.press('Enter');
    await expect(chip(page, sewing!.name)).toHaveAttribute('aria-pressed', 'true');

    await expect(page.getByLabel(t('iban.field'))).toBeFocused();
    await page.keyboard.type('120000000000000000000001');
    await page.keyboard.press('Enter');

    // The notes keep Enter for a new line; Tab leaves them.
    await expect(page.getByLabel(t('parties.notes'))).toBeFocused();
    await page.keyboard.type('کوله می‌دوزد');
    await page.keyboard.press('Enter');
    await page.keyboard.type('چرخ دارد');
    await page.keyboard.press('Tab');

    // No Enter on the way sent the form: only the one on its button does.
    await expect(page.getByRole('button', { name: t('parties.contractors.save') })).toBeFocused();
    expect(sent).toEqual([]);
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(new RegExp(`/contractors/${contractor.id}$`));
    expect(sent).toEqual([
      {
        fullName: 'حسن مرادی',
        mobileNumber: '09120000777',
        stageIds: [sewing!.id],
        notes: 'کوله می‌دوزد\nچرخ دارد',
        iban: 'IR120000000000000000000001',
      },
    ]);
  });
});

test('keyboard-refused', async ({ page }) => {
  const sent = recordBodies(page, 'POST', '/panel/parties/contractors');

  await onPage(page, '/contractors/new', cutting!.name, async () => {
    const save = page.getByRole('button', { name: t('parties.contractors.save') });

    // Refused: the focus goes to the first wrong field, the name.
    await save.focus();
    await page.keyboard.press('Enter');
    await expect(page.getByText(t('parties.nameMissing'))).toBeVisible();
    await expect(page.getByLabel(t('parties.fullName'))).toBeFocused();

    // With the name typed, the first wrong one is the mobile.
    await page.keyboard.type('حسن مرادی');
    await save.focus();
    await page.keyboard.press('Enter');
    await expect(page.getByLabel(t('parties.mobile'))).toBeFocused();

    // Then the stages, a group with no field of its own: its first chip.
    await page.keyboard.type('09120000777');
    await save.focus();
    await page.keyboard.press('Enter');
    await expect(chip(page, cutting!.name)).toBeFocused();
    expect(sent).toEqual([]);
  });
});

test('keyboard-model', async ({ page }) => {
  // A switch, a list, the sizes typed one after another and the rows of chips of a new model.
  await onPage(page, '/models/new', sample.fabrics[0]!.name, async () => {
    await expect(page.getByLabel(t('newModel.name'))).toBeFocused();
    await page.keyboard.type('کیف کمری');
    await page.keyboard.press('Enter');

    await expect(page.getByLabel(t('model.category'))).toBeFocused();
    await page.keyboard.press('Enter');

    // Enter moves on from a switch; Space flips it.
    const hasSizes = page.getByRole('switch', { name: t('newModel.hasSizes') });
    await expect(hasSizes).toBeFocused();
    await page.keyboard.press('Space');
    await expect(hasSizes).toHaveAttribute('aria-checked', 'true');
    await page.keyboard.press('Enter');
    await expect(hasSizes).toHaveAttribute('aria-checked', 'true');

    // Enter adds a size and the field waits for the next; an empty one moves on.
    const firstSize = page.getByLabel(t('newModel.firstSize'));
    await expect(firstSize).toBeFocused();
    await page.keyboard.type('کوچک');
    await page.keyboard.press('Enter');
    const nextSize = page.getByLabel(t('newModel.nextSize'));
    await expect(nextSize).toBeFocused();
    await expect(nextSize).toHaveValue('');
    await page.keyboard.press('Enter');

    // The fabrics are one row, and a ticked fabric's colours the next one. Canvas («برزنت») comes
    // first in the Persian order of the active fabrics.
    const canvas = chip(page, sample.fabrics[0]!.name);
    await expect(canvas).toBeFocused();
    await page.keyboard.press('Space');
    await expect(canvas).toHaveAttribute('aria-pressed', 'true');
    await page.keyboard.press('Enter');
    const colourRow = page.getByText(t('bags.coloursOf', { fabric: sample.fabrics[0]!.name })).locator('..');
    const firstColour = colourRow.getByRole('button').first();
    await expect(firstColour).toBeFocused();
    await page.keyboard.press('Space');
    await expect(firstColour).toHaveAttribute('aria-pressed', 'true');

    // After the last row, Enter goes to the button, which says what saving will make.
    await page.keyboard.press('Enter');
    await expect(page.getByRole('button', { name: t('newModel.save') })).toBeFocused();
  });
});

test('keyboard-rate', async ({ page }) => {
  const sent = recordBodies(page, 'POST', '/panel/production/rates');
  const line = (stage: string) => page.getByRole('button', { name: t('rates.change', { stage }) });
  const form = page.getByRole('form');
  const amount = form.getByLabel(t('rates.form.amount'));
  const day = form.getByLabel(t('date.day'), { exact: true });
  const month = form.getByLabel(t('date.month'), { exact: true });
  const year = form.getByLabel(t('date.year'), { exact: true });
  const save = form.getByRole('button', { name: t('rates.form.save') });

  await onPage(page, `/models/${sample.model.id}`, sample.model.name, async () => {
    // Opened from its line by the keyboard, the amount first.
    await line(cutting!.name).focus();
    await page.keyboard.press('Enter');
    await expect(amount).toBeFocused();

    // A zero too many is caught while it is typed, against today's 150,000.
    const tooHigh = t('rates.form.muchHigher', { rate: t('rates.form.todaysRate') });
    await page.keyboard.type('1500000');
    await expect(page.getByText(tooHigh)).toBeVisible();
    await page.keyboard.press('Control+A');
    await page.keyboard.type('180000');
    await expect(page.getByText(tooHigh)).toBeHidden();

    // Enter through the three lists of today's date to the button, and Enter saves.
    for (const list of [day, month, year, save]) {
      await page.keyboard.press('Enter');
      await expect(list).toBeFocused();
    }

    expect(sent).toEqual([]);
    await page.keyboard.press('Enter');
    await expect(form).toBeHidden();
    // The focus is back on the line that opened it, not at the start of the page.
    await expect(line(cutting!.name)).toBeFocused();

    // A raise from 10 Dey 1405, the numbers typed on the lists, for the next stage.
    await line(sewing!.name).focus();
    await page.keyboard.press('Enter');
    await page.keyboard.type('460000');
    await page.keyboard.press('Enter');
    // Persian digits, as a Persian keyboard's top row types them. Playwright presses only the keys
    // of a US keyboard, so their key events are sent to the list as the browser would send them.
    for (const digit of ['۱', '۰']) {
      await day.dispatchEvent('keydown', { key: digit, bubbles: true, cancelable: true });
    }
    await expect(day).toHaveValue('10');
    await page.keyboard.press('Enter');
    await page.keyboard.type('10');
    await expect(month).toHaveValue('10');
    await page.keyboard.press('Enter');
    await expect(year).toHaveValue('1405');
    await page.keyboard.press('Enter');
    await page.keyboard.press('Enter');
    await expect(form).toBeHidden();

    // Written out, not worked out with the panel's own calendar.
    expect(sent).toEqual([
      { productId: sample.model.id, stageId: cutting!.id, amount: 180_000, startsOn: sample.today },
      { productId: sample.model.id, stageId: sewing!.id, amount: 460_000, startsOn: '2026-12-31' },
    ]);

    // The next new rate opens on the date of the last one, for a raise entered stage after stage.
    await line(fitting!.name).focus();
    await page.keyboard.press('Enter');
    await expect(day).toHaveValue('10');
    await expect(month).toHaveValue('10');
    await expect(form.getByText(t('rates.form.future'))).toBeVisible();

    // Escape closes it, and the focus is back on its line.
    await page.keyboard.press('Escape');
    await expect(form).toBeHidden();
    await expect(line(fitting!.name)).toBeFocused();
  });
});

test('keyboard-iban', async ({ page }) => {
  const sent = recordBodies(page, 'PUT', `/panel/parties/contractors/${contractor.id}/iban`);
  const line = page.getByRole('button', { name: t('iban.change') });
  const form = page.getByRole('form', { name: t('iban.changeTitle') });
  const field = form.getByLabel(t('iban.field'));

  await onPage(page, `/contractors/${contractor.id}`, contractor.fullName, async () => {
    // Escape closes the small form; the focus goes back to the IBAN's line.
    await line.focus();
    await page.keyboard.press('Enter');
    await expect(field).toBeFocused();
    await page.keyboard.press('Escape');
    await expect(form).toBeHidden();
    await expect(line).toBeFocused();

    // Typed, Enter to the button, Enter saves, and the focus is back on the line.
    await page.keyboard.press('Enter');
    await page.keyboard.type('120000000000000000000001');
    await page.keyboard.press('Enter');
    await expect(form.getByRole('button', { name: t('iban.save') })).toBeFocused();
    expect(sent).toEqual([]);
    await page.keyboard.press('Enter');
    await expect(form).toBeHidden();
    await expect(line).toBeFocused();
    expect(sent).toEqual([{ iban: 'IR120000000000000000000001' }]);
  });
});

test('iphone-focus', async ({ page }) => {
  const line = page.getByRole('button', { name: t('iban.change') });
  const form = page.getByRole('form', { name: t('iban.changeTitle') });
  const removeButton = form.getByRole('button', { name: t('iban.remove') });
  const question = page.getByRole('alertdialog', { name: t('iban.removeTitle') });

  /** A tap as Safari on an iPhone makes it: the button is pressed and clicked, never focused. */
  const tap = async (target: typeof line) => {
    await target.dispatchEvent('pointerdown');
    await target.dispatchEvent('click');
  };

  await onPage(page, `/contractors/${contractor.id}`, contractor.fullName, async () => {
    await tap(line);
    await expect(form.getByLabel(t('iban.field'))).toBeFocused();

    // The IBAN field keeps the focus through the tap on «پاک کردن شبا»; closing the question still
    // gives it to the button that asked, not to the field, which would open the phone's keyboard.
    await tap(removeButton);
    await expect(question).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(question).toBeHidden();
    await expect(removeButton).toBeFocused();

    // A tap on «انصراف» closes the form, and the focus is on the IBAN's line again.
    await tap(form.getByRole('button', { name: t('parties.form.cancel') }));
    await expect(form).toBeHidden();
    await expect(line).toBeFocused();
  });
});

test('keyboard-start-date', async ({ page }) => {
  const worker = sample.workerDetails;
  const line = page.getByRole('button', { name: t('parties.changeStartDate') });
  const form = page.getByRole('form', { name: t('parties.changeStartDate') });

  await onPage(page, `/workers/${worker.id}`, worker.fullName, async () => {
    // The date's line gives way to its form, so the focus starts in the form's day list.
    await line.focus();
    await page.keyboard.press('Enter');
    await expect(form.getByLabel(t('date.day'), { exact: true })).toBeFocused();

    // Enter through the lists to «ثبت», then Tab to «انصراف»: the line drawn again has the focus.
    for (let press = 0; press < 3; press++) {
      await page.keyboard.press('Enter');
    }
    await expect(form.getByRole('button', { name: t('parties.form.save') })).toBeFocused();
    await page.keyboard.press('Tab');
    await page.keyboard.press('Enter');
    await expect(form).toBeHidden();
    await expect(line).toBeFocused();
  });
});
