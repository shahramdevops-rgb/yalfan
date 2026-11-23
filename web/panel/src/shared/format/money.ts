import { t } from '../i18n/fa';

// Money is Rial: stored, in the API, typed and shown, and every amount shown is labelled «ریال»
// (ADR 0002). Under every amount shown, a quieter line gives its Toman equivalent and its words
// (ADR 0002 point 9, built with the first screen that shows an amount in task 1.6).

const rial = new Intl.NumberFormat('fa-IR', { maximumFractionDigits: 0 });

/**
 * An amount in Rial, in Persian digits with thousands separators and the «ریال» label:
 * 12500000 becomes "۱۲٬۵۰۰٬۰۰۰ ریال". A decimal string from the API is formatted exactly,
 * without passing through a floating-point number.
 */
export function formatRial(amount: number | `${number}`): string {
  return `${rial.format(amount)} ${t('money.rial')}`;
}
