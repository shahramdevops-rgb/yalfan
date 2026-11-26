import { describe, expect, it } from 'vitest';
import type { HistoryChange } from './historyApi';
import { describeSave, foldBagsAfter, leftToRight as ltr } from './historyLines';

const change = (
  part: HistoryChange['part'],
  action: HistoryChange['action'],
  label: string | null,
  fields: HistoryChange['fields'] = [],
): HistoryChange => ({ part, action, label, fields });

const texts = (changes: HistoryChange[]) => describeSave(changes).map((line) => line.text);

const bag = (number: number, action: HistoryChange['action'], fields: HistoryChange['fields']) =>
  change('bag', action, `101-${String(number)}`, fields);

describe('describeSave', () => {
  it('lists what a new model was made with under one line, then its sizes and bags', () => {
    const lines = describeSave([
      change('model', 'created', null, [
        { name: 'name', old: null, new: 'کیف دستی' },
        { name: 'category', old: null, new: 'کوله' },
      ]),
      change('size', 'created', 'کوچک', [{ name: 'name', old: null, new: 'کوچک' }]),
      bag(1, 'created', [
        { name: 'size', old: null, new: 'کوچک' },
        { name: 'fabric', old: null, new: 'برزنت' },
        { name: 'colour', old: null, new: 'مشکی' },
      ]),
    ]);

    expect(lines).toEqual([
      { text: 'مدل ساخته شد', details: ['نام: کیف دستی', 'دسته‌بندی: کوله'] },
      { text: 'سایز «کوچک» اضافه شد' },
      { text: `کیف ${ltr('۱۰۱-۱')} اضافه شد: برزنت، مشکی، کوچک` },
    ]);
  });

  it('shows a changed field from old to new, and nothing as a dash', () => {
    expect(
      texts([
        change('model', 'updated', null, [
          { name: 'name', old: 'کیف دستی', new: 'کیف دستی چرمی' },
          { name: 'category', old: null, new: 'کوله' },
        ]),
      ]),
    ).toEqual(['نام: کیف دستی ← کیف دستی چرمی', 'دسته‌بندی: — ← کوله']);
  });

  it('says a record or a bag was turned off or on', () => {
    expect(
      texts([
        change('contractor', 'updated', null, [{ name: 'active', old: 'true', new: 'false' }]),
        bag(3, 'updated', [{ name: 'active', old: 'false', new: 'true' }]),
      ]),
    ).toEqual(['کنتراتی غیرفعال شد', `کیف ${ltr('۱۰۱-۳')} فعال شد`]);
  });

  it('tells an IBAN set, replaced and removed apart, and never shows a number', () => {
    const iban = (old: string | null, now: string | null) =>
      texts([change('worker', 'updated', null, [{ name: 'iban', old, new: now }])]);

    expect(iban('false', 'true')).toEqual(['شبا ثبت شد']);
    expect(iban('true', 'true')).toEqual(['شبا عوض شد']);
    expect(iban('true', 'false')).toEqual(['شبا پاک شد']);
    // Written before ADR 0027: only that it changed is known.
    expect(iban(null, null)).toEqual(['شبا عوض شد']);

    const created = describeSave([change('contractor', 'created', null, [{ name: 'iban', old: null, new: 'true' }])]);
    expect(created[0]?.details).toEqual(['شبا ثبت شد']);
  });

  it('makes one line of sizes dragged into a new order, and names a renamed size by both names', () => {
    expect(
      texts([
        change('size', 'updated', 'کوچک', [{ name: 'position', old: '1', new: '2' }]),
        change('size', 'updated', 'بزرگ', [{ name: 'position', old: '2', new: '1' }]),
        change('size', 'updated', 'خیلی بزرگ', [{ name: 'name', old: 'بزرگ‌تر', new: 'خیلی بزرگ' }]),
      ]),
    ).toEqual(['ترتیب سایزها عوض شد', 'نام سایز «بزرگ‌تر» شد «خیلی بزرگ»']);
  });

  it('says a bag got the first size of its model', () => {
    expect(texts([bag(1, 'updated', [{ name: 'size', old: null, new: 'بزرگ' }])])).toEqual([
      `کیف ${ltr('۱۰۱-۱')} سایز «بزرگ» گرفت`,
    ]);
  });

  it('folds many bags of one save under one line, in their place', () => {
    const bags = Array.from({ length: foldBagsAfter + 1 }, (_, index) =>
      bag(index + 1, 'created', [
        { name: 'fabric', old: null, new: 'برزنت' },
        { name: 'colour', old: null, new: 'مشکی' },
      ]),
    );

    const lines = describeSave([
      change('model', 'created', null, [{ name: 'name', old: null, new: 'کوله' }]),
      ...bags,
      change('rate', 'created', 'برش', [{ name: 'amount', old: null, new: '10000' }]),
    ]);

    expect(lines.map((line) => line.text)).toEqual(['مدل ساخته شد', '۷ کیف اضافه شد', 'نرخ برش از ']);
    expect(lines[1]?.folded).toBe(true);
    expect(lines[1]?.details).toHaveLength(foldBagsAfter + 1);
    expect(lines[1]?.details?.[0]).toBe(`کیف ${ltr('۱۰۱-۱')} اضافه شد: برزنت، مشکی`);
  });

  it('keeps a few bags as lines of their own', () => {
    const bags = Array.from({ length: foldBagsAfter }, (_, index) =>
      bag(index + 1, 'updated', [{ name: 'active', old: 'true', new: 'false' }]),
    );

    const lines = describeSave(bags);

    expect(lines).toHaveLength(foldBagsAfter);
    expect(lines.some((line) => line.folded)).toBe(false);
  });

  it('shows a new rate with its stage, its first day and its amount', () => {
    expect(
      describeSave([
        change('rate', 'created', 'برش', [
          { name: 'amount', old: null, new: '1200000' },
          { name: 'startsOn', old: null, new: '2026-12-22' },
        ]),
      ]),
    ).toEqual([{ text: 'نرخ برش از ۱ دی ۱۴۰۵', amount: '1200000' }]);
  });

  it('says a stage was ticked or unticked, and shows a start date and a mobile as the panel does', () => {
    expect(
      texts([
        change('worker', 'updated', null, [
          { name: 'startDate', old: '2021-03-21', new: '2019-09-23' },
          { name: 'mobileNumber', old: '09121234567', new: '09351234567' },
        ]),
        change('stage', 'deleted', 'دوخت'),
        change('stage', 'created', 'بسته‌بندی'),
      ]),
    ).toEqual([
      'شروع کار: ۱ فروردین ۱۴۰۰ ← ۱ مهر ۱۳۹۸',
      `شمارهٔ موبایل: ${ltr('۰۹۱۲ ۱۲۳ ۴۵۶۷')} ← ${ltr('۰۹۳۵ ۱۲۳ ۴۵۶۷')}`,
      'مرحلهٔ «دوخت» برداشته شد',
      'مرحلهٔ «بسته‌بندی» اضافه شد',
    ]);
  });
});
