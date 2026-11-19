// Every text the panel shows lives here, never inside a component (CLAUDE.md). The panel is
// Persian only, so one file and a small t() are enough; no i18n library (ADR 0014).

const messages = {
  'app.name': 'یلفان',
  'app.subtitle': 'پنل مدیریت کارگاه',
  'app.loading': 'در حال بارگذاری…',

  'login.title': 'ورود به پنل',
  'login.mobileLabel': 'شمارهٔ موبایل',
  'login.mobileHint': 'مثل ۰۹۱۲۱۲۳۴۵۶۷',
  'login.sendCode': 'دریافت کد',
  'login.codeLabel': 'کد پیامک‌شده',
  'login.codeSentTo': 'اگر شمارهٔ {mobile} در پنل ثبت شده باشد، یک کد ۶ رقمی برایش پیامک شد.',
  'login.submit': 'ورود',
  'login.resend': 'ارسال دوباره کد',
  'login.resendIn': 'ارسال دوباره تا {time} دیگر',
  'login.changeMobile': 'تغییر شماره',
  'login.codeFormat': 'کد باید ۶ رقم باشد.',

  'nav.dashboard': 'داشبورد',
  'nav.openMenu': 'باز کردن منو',
  'nav.closeMenu': 'بستن منو',

  'header.logout': 'خروج',
  'header.today': 'امروز {date}',

  'dashboard.greeting': 'سلام {name}، خوش آمدید.',
  'dashboard.empty': 'بخش‌های پنل در مراحل بعدی یکی‌یکی اینجا اضافه می‌شوند.',

  'notFound.title': 'این صفحه پیدا نشد.',
  'notFound.back': 'بازگشت به داشبورد',
  'error.title': 'مشکلی پیش آمد.',
  'error.reload': 'بارگذاری دوباره',

  'role.owner': 'مالک',
  'role.manager': 'مدیر کارگاه',
  'role.operator': 'اپراتور دفتر',

  'money.rial': 'ریال',
} as const;

// API error codes (ADR 0011) in the panel's words. A code that is missing here falls back to
// errors.unknown, so a new code on the API never shows up as raw English.
const errorMessages: Record<string, string> = {
  'identity.otp_invalid': 'کد درست نیست. دوباره امتحان کنید.',
  'identity.otp_expired': 'زمان این کد تمام شده است. کد تازه بگیرید.',
  'identity.refresh_invalid': 'زمان ورود شما تمام شده است. دوباره وارد شوید.',
  'mobile.invalid': 'شمارهٔ موبایل درست نیست. شمارهٔ ۱۱ رقمی را مثل ۰۹۱۲۱۲۳۴۵۶۷ بنویسید.',
  'validation.required': 'این خانه را پر کنید.',
  'validation.invalid_format': 'قالب این مقدار درست نیست.',
  'request.unauthenticated': 'زمان ورود شما تمام شده است. دوباره وارد شوید.',
  'request.forbidden': 'اجازهٔ این کار را ندارید.',
  'request.not_found': 'پیدا نشد.',
  'server.unexpected': 'خطای پیش‌بینی‌نشده‌ای رخ داد. دوباره امتحان کنید.',
  'network.unreachable': 'ارتباط با سرور برقرار نشد. اینترنت را بررسی کنید و دوباره امتحان کنید.',
  'errors.unknown': 'کار انجام نشد. دوباره امتحان کنید.',
};

export type MessageKey = keyof typeof messages;

/** A panel text, with {name} placeholders filled in. */
export function t(key: MessageKey, params?: Record<string, string | number>): string {
  const text: string = messages[key];
  if (!params) {
    return text;
  }

  return text.replace(/\{(\w+)\}/g, (placeholder, name: string) =>
    name in params ? String(params[name]) : placeholder,
  );
}

/** The panel's message for an API error code. */
export function errorMessage(code: string | undefined): string {
  return (code && errorMessages[code]) || errorMessages['errors.unknown']!;
}
