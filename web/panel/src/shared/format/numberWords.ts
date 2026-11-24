// A whole number in Persian words, as amounts are said aloud: 12000 is «دوازده هزار». It reads
// the Toman line under every amount (ADR 0002 point 9, ADR 0024), so it is exact for any int64
// the API can send: it works on BigInt, never on a floating-point number.

const ones = ['', 'یک', 'دو', 'سه', 'چهار', 'پنج', 'شش', 'هفت', 'هشت', 'نه'];
const teens = ['ده', 'یازده', 'دوازده', 'سیزده', 'چهارده', 'پانزده', 'شانزده', 'هفده', 'هجده', 'نوزده'];
const tens = ['', '', 'بیست', 'سی', 'چهل', 'پنجاه', 'شصت', 'هفتاد', 'هشتاد', 'نود'];
const hundreds = ['', 'صد', 'دویست', 'سیصد', 'چهارصد', 'پانصد', 'ششصد', 'هفتصد', 'هشتصد', 'نهصد'];

const and = ' و ';
const thousand = 1_000n;
const million = 1_000_000n;
const billion = 1_000_000_000n;

/**
 * 0 is «صفر», 1000 «هزار» (not «یک هزار», as nobody says it), 1000000 «یک میلیون», and above a
 * billion the count of billions is itself read out: 2500000000000 is «دو هزار و پانصد میلیارد».
 * A negative number starts with «منفی».
 */
export function persianWords(value: bigint): string {
  if (value === 0n) {
    return 'صفر';
  }

  return value < 0n ? `منفی ${positive(-value)}` : positive(value);
}

function positive(value: bigint): string {
  if (value >= billion) {
    return scaled(value, billion, `${positive(value / billion)} میلیارد`);
  }

  if (value >= million) {
    return scaled(value, million, `${belowThousand(Number(value / million))} میلیون`);
  }

  if (value >= thousand) {
    const count = Number(value / thousand);
    return scaled(value, thousand, count === 1 ? 'هزار' : `${belowThousand(count)} هزار`);
  }

  return belowThousand(Number(value));
}

/** The words of the scale's count, then the rest after «و» when there is one. */
function scaled(value: bigint, scale: bigint, head: string): string {
  const rest = value % scale;
  return rest === 0n ? head : `${head}${and}${positive(rest)}`;
}

function belowThousand(value: number): string {
  const parts: string[] = [];
  const hundred = Math.floor(value / 100);
  const rest = value % 100;

  if (hundred > 0) {
    parts.push(hundreds[hundred]!);
  }

  if (rest >= 10 && rest < 20) {
    parts.push(teens[rest - 10]!);
  } else {
    if (rest >= 20) {
      parts.push(tens[Math.floor(rest / 10)]!);
    }

    if (rest % 10 > 0) {
      parts.push(ones[rest % 10]!);
    }
  }

  return parts.join(and);
}
