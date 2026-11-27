import { expect, test } from '@playwright/test';
import { formatMobile } from '../src/shared/format/mobile';
import { t } from '../src/shared/i18n/fa';
import { onPage, recordRequests } from './open';
import * as sample from './sample';

// Behaviour tests of the overlays (ADR 0030): what the browser's own dialog promises, checked in a
// real Chromium against the sample API, with a picture of each overlay's look. The test names are
// one word each, which is how scripts/check.ps1 names a failed one.

const contractor = sample.contractorDetails;
// Written out, not worked out with the panel's own formatter: a formatter that showed more digits
// must fail here (sample contractor 2's IBAN ends in 2002).
const contractorLastFour = '•••• ۲۰۰۲';

test('drawer', async ({ page }, testInfo) => {
  test.skip(testInfo.project.name !== 'phone', 'the drawer is the sidebar of a phone');

  await onPage(page, '/', sample.owner.fullName, async () => {
    const menuButton = page.getByRole('button', { name: t('nav.openMenu') });
    const drawer = page.getByRole('dialog', { name: t('nav.menu') });

    await menuButton.click();
    await expect(drawer).toBeVisible();
    await expect(page).toHaveScreenshot('menu.png');

    // More Tabs than it has stops: the focus never reaches the page behind it. (Past the last stop
    // it may leave the page for the browser's own bar, which leaves the body focused.)
    for (let press = 0; press < 12; press++) {
      await page.keyboard.press('Tab');
      const inside = await drawer.evaluate(
        (element) => document.activeElement === document.body || element.contains(document.activeElement),
      );
      expect(inside, `the focus left the drawer after ${press + 1} Tabs`).toBe(true);
    }

    // The page behind it stays still.
    expect(await page.evaluate(() => getComputedStyle(document.documentElement).overflow)).toBe('hidden');

    await page.keyboard.press('Escape');
    await expect(drawer).toBeHidden();
    await expect(menuButton).toBeFocused();
    expect(await page.evaluate(() => getComputedStyle(document.documentElement).overflow)).not.toBe('hidden');

    // A tap on the dimmed page beside it, at the inline end (the left, in Persian), closes it.
    await menuButton.click();
    await page.touchscreen.tap(20, 400);
    await expect(drawer).toBeHidden();

    // A link goes to its page, and the drawer does not stay open over it.
    await menuButton.click();
    await drawer.getByRole('link', { name: t('nav.contractors') }).click();
    await expect(page).toHaveURL(/\/contractors$/);
    await expect(drawer).toBeHidden();

    // Turned sideways to the desktop's width, where the sidebar shows itself, it closes.
    await menuButton.click();
    await expect(drawer).toBeVisible();
    await page.setViewportSize({ width: 1280, height: 800 });
    await expect(drawer).toBeHidden();
    expect(await page.evaluate(() => getComputedStyle(document.documentElement).overflow)).not.toBe('hidden');
  });
});

test('iban-remove', async ({ page }) => {
  const sent = recordRequests(page);
  const deletes = () => sent.filter((request) => request.startsWith('DELETE'));

  await onPage(page, `/contractors/${contractor.id}`, contractor.fullName, async () => {
    await page.getByRole('button', { name: t('iban.change') }).click();
    const ibanForm = page.getByRole('form', { name: t('iban.changeTitle') });
    const removeButton = ibanForm.getByRole('button', { name: t('iban.remove') });
    const question = page.getByRole('alertdialog', { name: t('iban.removeTitle') });

    // It asks first, naming whose IBAN and which one, with «انصراف» focused.
    await removeButton.click();
    await expect(question).toBeVisible();
    await expect(question).toContainText(t('iban.removeQuestion', { name: contractor.fullName }));
    await expect(question).toContainText(contractorLastFour);
    await expect(question.getByRole('button', { name: t('dialog.cancel') })).toBeFocused();
    await expect(page).toHaveScreenshot('iban-remove.png');

    // An Enter pressed from habit cancels; the focus goes back to the button that asked.
    await page.keyboard.press('Enter');
    await expect(question).toBeHidden();
    await expect(removeButton).toBeFocused();

    // Escape cancels the question only: the IBAN's small form stays open.
    await removeButton.click();
    await page.keyboard.press('Escape');
    await expect(question).toBeHidden();
    await expect(ibanForm).toBeVisible();

    // A tap on the dimmed page cancels too.
    await removeButton.click();
    await page.mouse.click(5, 5);
    await expect(question).toBeHidden();

    // A press that begins on the dimmed page and ends on a button inside is not a tap outside.
    await removeButton.click();
    const confirmBox = (await question.getByRole('button', { name: t('iban.remove') }).boundingBox())!;
    await page.mouse.move(5, 5);
    await page.mouse.down();
    await page.mouse.move(confirmBox.x + confirmBox.width / 2, confirmBox.y + confirmBox.height / 2);
    await page.mouse.up();
    await expect(question).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(question).toBeHidden();
    expect(deletes()).toEqual([]);

    // Only the question's own button removes it.
    await removeButton.click();
    await question.getByRole('button', { name: t('iban.remove') }).click();
    await expect(page.getByText(t('iban.none'))).toBeVisible();
    expect(deletes()).toEqual([`DELETE /panel/parties/contractors/${contractor.id}/iban`]);
  });
});

test('own-number', async ({ page }, testInfo) => {
  test.skip(testInfo.project.name !== 'desktop', 'one width is enough for behaviour without a picture');
  const sent = recordRequests(page);
  const owner = sample.users[0]!;
  const number = owner.loginNumbers[0]!;
  const deactivate = `POST /panel/users/${owner.id}/numbers/${number.id}/deactivate`;

  await onPage(page, '/users', sample.users[2]!.fullName, async () => {
    const numberSwitch = page.getByRole('switch', {
      name: t('users.numberLabel', { mobile: formatMobile(number.mobileNumber) }),
    });
    const question = page.getByRole('alertdialog', { name: t('users.ownNumberOffTitle') });

    // A number of one's own account asks first, and «انصراف» leaves it on.
    await numberSwitch.click();
    await expect(question).toBeVisible();
    await question.getByRole('button', { name: t('dialog.cancel') }).click();
    await expect(question).toBeHidden();
    await expect(numberSwitch).toHaveAttribute('aria-checked', 'true');
    expect(sent).not.toContain(deactivate);

    // Confirmed, it is switched off and this page goes to the login (ADR 0026).
    await numberSwitch.click();
    await question.getByRole('button', { name: t('users.ownNumberOff') }).click();
    await expect(page).toHaveURL(/\/login$/);
    expect(sent).toContain(deactivate);
  });
});

test('toast', async ({ page }, testInfo) => {
  test.skip(testInfo.project.name !== 'desktop', 'one width is enough for one message');

  await onPage(page, '/users', sample.users[2]!.fullName, async () => {
    const form = page.getByRole('form', { name: t('users.new') });
    await form.getByLabel(t('users.fullName')).fill(sample.newUser.fullName);
    await form.getByLabel(t('users.mobile')).fill(sample.newUser.loginNumbers[0]!.mobileNumber);
    await form.getByRole('button', { name: t('users.add') }).click();

    // Said once, in the region screen readers listen to.
    const message = page.getByRole('status').getByText(t('users.added', { name: sample.newUser.fullName }));
    await expect(message).toBeVisible();
    await expect(message).toHaveScreenshot('toast.png');
  });
});
