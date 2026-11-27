import { expect, test, type Page } from '@playwright/test';
import { t } from '../src/shared/i18n/fa';
import { onPage } from './open';
import * as sample from './sample';

// Behaviour tests of the data table (ADR 0032): the header stays at the top of the window, the
// whole row opens its page while only the name is a link, one Tab per row, and the pager by
// keyboard. The models list has 26 models, twenty to a page. The test names are one word each,
// which is how scripts/check.ps1 names a failed one.

const [first, second] = sample.modelList;
const firstOfPageTwo = sample.modelList[20]!;
const modelPage = new RegExp(`/models/${sample.model.id}$`);

const row = (page: Page, name: string) => page.getByRole('row').filter({ has: page.getByRole('link', { name }) });

test('table-header', async ({ page, isMobile }) => {
  test.skip(isMobile, 'the phone has cards, not a table');

  await onPage(page, '/models', first!.name, async () => {
    const header = page.getByRole('columnheader', { name: t('list.columnName'), exact: true });
    await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));

    // The first rows have gone above the window; the header has not.
    await expect.poll(async () => (await row(page, first!.name).boundingBox())?.y ?? 0).toBeLessThan(0);
    await expect(header).toBeInViewport();
    expect((await header.boundingBox())?.y).toBe(0);
  });
});

test('table-keyboard', async ({ page, isMobile }) => {
  test.skip(isMobile, 'behaviour without a picture: one width is enough');

  await onPage(page, '/models', first!.name, async () => {
    await page.getByLabel(t('models.search')).focus();

    // One Tab per row: the name is the row's only stop, and the row shows it has the focus.
    await page.keyboard.press('Tab');
    const firstLink = page.getByRole('link', { name: first!.name, exact: true });
    await expect(firstLink).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByRole('link', { name: second!.name, exact: true })).toBeFocused();
    const background = (name: string) =>
      row(page, name)
        .getByRole('cell')
        .last()
        .evaluate((cell) => getComputedStyle(cell).backgroundColor);
    expect(await background(second!.name)).not.toBe(await background(first!.name));

    // Past the twentieth row, the next page; «قبلی» is disabled on the first, so it is no stop.
    for (let tab = 0; tab < 19; tab++) {
      await page.keyboard.press('Tab');
    }
    await expect(page.getByRole('button', { name: t('pager.next') })).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/\/models\?page=2$/);
    await expect(page.getByRole('link', { name: firstOfPageTwo.name, exact: true })).toBeVisible();
    await expect(page.getByText(t('pager.pageOf', { page: '۲', pages: '۲' }))).toBeVisible();

    // Back to the first page; Enter on a name opens its model.
    await page.goBack();
    await expect(firstLink).toBeVisible();
    await firstLink.focus();
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(modelPage);
  });
});

test('table-click', async ({ page, isMobile }) => {
  test.skip(isMobile, 'the phone has cards, whose whole card is the link');

  await onPage(page, '/models', first!.name, async () => {
    // Selecting a category's text with the mouse opens nothing.
    const category = row(page, first!.name).getByRole('cell').nth(2);
    const box = (await category.boundingBox())!;
    await page.mouse.move(box.x + box.width - 4, box.y + box.height / 2);
    await page.mouse.down();
    await page.mouse.move(box.x + 4, box.y + box.height / 2, { steps: 5 });
    await page.mouse.up();
    expect(await page.evaluate(() => window.getSelection()?.toString())).not.toBe('');
    await expect(page).toHaveURL(/\/models$/);

    // A click on the row beside the name opens the model, as one step of the back button.
    await row(page, first!.name).getByRole('cell').last().click();
    await expect(page).toHaveURL(modelPage);
    await page.goBack();
    await expect(page).toHaveURL(/\/models$/);

    // A click on the name itself is the link's, also one step.
    await page.getByRole('link', { name: first!.name, exact: true }).click();
    await expect(page).toHaveURL(modelPage);
    await page.goBack();
    await expect(page).toHaveURL(/\/models$/);
  });
});

test('table-phone', async ({ page, isMobile }) => {
  test.skip(!isMobile, 'the cards are for a phone');

  await onPage(page, '/models', first!.name, async () => {
    // No table on a phone; each row is a card, the whole card one link.
    await expect(page.getByRole('table')).toBeHidden();
    const card = page.getByRole('link', { name: new RegExp(first!.name) });
    await expect(card).toHaveCount(1);
    await card.tap();
    await expect(page).toHaveURL(modelPage);
  });
});
