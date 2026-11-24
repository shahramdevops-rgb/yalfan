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
  'nav.lists': 'فهرست‌ها',
  'nav.stages': 'مراحل کار',
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

  'list.add': 'افزودن',
  'list.sending': 'در حال ثبت…',
  'list.columnName': 'نام',
  'list.columnActive': 'فعال',
  'list.inactive': 'غیرفعال',
  'list.renameHint': 'برای تغییر نام بزنید',
  'list.renameLabel': 'نام تازه به جای «{name}»',
  'list.renameButton': 'تغییر نام «{name}»',
  'list.activeLabel': 'فعال بودن «{name}»',
  'list.retry': 'دوباره امتحان کنید',

  'sortable.handle': 'جابه‌جا کردن «{name}»؛ با کلیدهای بالا و پایین هم می‌شود',
  'sortable.moved': '«{name}» به جای {place} رفت.',

  'lists.title': 'فهرست‌ها',
  'lists.hint':
    'برای تغییر نام، روی نام بزنید. مورد غیرفعال در انتخاب‌های تازه نمی‌آید، ولی هر چه قبلاً با آن ثبت شده دست نمی‌خورد.',
  'lists.tab.fabrics': 'پارچه‌ها',
  'lists.tab.colours': 'رنگ‌ها',
  'lists.tab.categories': 'دسته‌بندی‌ها',
  'lists.add.fabrics': 'پارچهٔ تازه',
  'lists.add.colours': 'رنگ تازه',
  'lists.add.categories': 'دسته‌بندی تازه',
  'lists.empty.fabrics': 'هنوز پارچه‌ای ثبت نشده است.',
  'lists.empty.colours': 'هنوز رنگی ثبت نشده است.',
  'lists.empty.categories': 'هنوز دسته‌بندی‌ای ثبت نشده است.',

  'stages.title': 'مراحل کار',
  'stages.hint':
    'مراحل همه‌جا به همین ترتیب دیده می‌شوند. برای جابه‌جا کردن، دستگیرهٔ کنار مرحله را بکشید. برای تغییر نام، روی نام بزنید.',
  'stages.add': 'مرحلهٔ تازه',
  'stages.empty': 'هنوز مرحله‌ای ثبت نشده است.',
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
  'validation.too_long': 'این متن بیش از اندازه بلند است.',
  'catalog.name_required': 'نام را بنویسید.',
  'catalog.name_too_long': 'این نام بیش از اندازه بلند است.',
  'catalog.name_taken': 'این نام قبلاً ثبت شده است.',
  'catalog.category_not_found': 'این دسته‌بندی پیدا نشد. صفحه را دوباره باز کنید.',
  'catalog.fabric_not_found': 'این پارچه پیدا نشد. صفحه را دوباره باز کنید.',
  'catalog.colour_not_found': 'این رنگ پیدا نشد. صفحه را دوباره باز کنید.',
  'catalog.category_inactive': 'این دسته‌بندی غیرفعال است. اول آن را در «فهرست‌ها» فعال کنید.',
  'catalog.fabric_inactive': 'این پارچه غیرفعال است. اول آن را در «فهرست‌ها» فعال کنید.',
  'catalog.colour_inactive': 'این رنگ غیرفعال است. اول آن را در «فهرست‌ها» فعال کنید.',
  'production.name_required': 'نام مرحله را بنویسید.',
  'production.name_too_long': 'این نام بیش از اندازه بلند است.',
  'production.name_taken': 'مرحله‌ای با این نام هست.',
  'production.stage_not_found': 'این مرحله پیدا نشد. صفحه را دوباره باز کنید.',
  'production.stage_inactive': 'این مرحله غیرفعال است. اول آن را در «مراحل کار» فعال کنید.',
  'production.stage_order_outdated':
    'فهرست مراحل در این فاصله عوض شده بود. فهرست تازه آمد؛ مرحله را دوباره جابه‌جا کنید.',
  'parties.stage_inactive': 'یکی از مراحل تیک‌خورده غیرفعال است. اول آن را در «مراحل کار» فعال کنید.',
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
