import { toPersianDigits } from './digits';

/**
 * A stored mobile (09XXXXXXXXX) in Persian digits, grouped as it is read aloud: "09121234567" is
 * "۰۹۱۲ ۱۲۳ ۴۵۶۷". Anything else is only put in Persian digits.
 */
export function formatMobile(mobile: string): string {
  const match = /^(\d{4})(\d{3})(\d{4})$/.exec(mobile);
  return toPersianDigits(match ? `${match[1]} ${match[2]} ${match[3]}` : mobile);
}
