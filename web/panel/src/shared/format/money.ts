import { t } from '../i18n/fa';

// Money is Rial everywhere: stored, in the API and on every screen, and every amount shown is
// labelled «ریال» (ADR 0002). Never Toman.

const rial = new Intl.NumberFormat('fa-IR', { maximumFractionDigits: 0 });

/**
 * An amount in Rial, in Persian digits with thousands separators and the «ریال» label:
 * 12500000 becomes "۱۲٬۵۰۰٬۰۰۰ ریال". A decimal string from the API is formatted exactly,
 * without passing through a floating-point number.
 */
export function formatRial(amount: number | `${number}`): string {
  return `${rial.format(amount)} ${t('money.rial')}`;
}
