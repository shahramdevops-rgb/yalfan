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
  'nav.models': 'مدل‌ها',
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
  'money.toman': 'تومان',
  'money.tomanAndRial': '{toman} تومان و {rial} ریال',
  'money.negative': 'منفی {words}',

  'date.day': 'روز',
  'date.month': 'ماه',
  'date.year': 'سال',

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

  'models.title': 'مدل‌ها',
  'models.new': 'مدل تازه',
  'models.back': 'بازگشت به مدل‌ها',
  'models.search': 'جست‌وجو با شماره یا بخشی از نام',
  'models.empty': 'هنوز مدلی ثبت نشده است. اولین مدل را با «مدل تازه» ثبت کنید.',
  'models.notFound': 'مدلی با «{search}» پیدا نشد.',
  'models.bagCount': '{count} کیف',
  'models.separator': ' · ',
  'models.pages': 'صفحه‌های فهرست مدل‌ها',
  'models.previous': 'قبلی',
  'models.next': 'بعدی',
  'models.pageOf': 'صفحهٔ {page} از {pages}',

  'newModel.title': 'مدل تازه',
  'newModel.name': 'نام مدل',
  'newModel.sizes': 'سایزها',
  'newModel.hasSizes': 'این مدل سایز دارد',
  'newModel.noSizesHint': 'اگر این مدل در چند اندازه دوخته می‌شود، کلید را روشن کنید.',
  'newModel.sizesHint':
    'سایزها را به ترتیب، یکی‌یکی بنویسید. هر پارچه و رنگی که انتخاب کنید، در همهٔ سایزها ثبت می‌شود.',
  'newModel.firstSize': 'نام اولین سایز',
  'newModel.nextSize': 'نام سایز بعدی',
  'newModel.removeSize': 'برداشتن سایز «{size}»',
  'newModel.sizeDuplicate': 'این سایز را قبلاً نوشته‌اید.',
  'newModel.sizesMissing': 'دست‌کم یک سایز بنویسید، یا کلید «این مدل سایز دارد» را خاموش کنید.',
  'newModel.bags': 'پارچه‌ها و رنگ‌ها',
  'newModel.bagsHint':
    'پارچه‌هایی را که این مدل با آن‌ها دوخته می‌شود بزنید، بعد زیر هر پارچه رنگ‌هایش را. اگر هنوز معلوم نیست، بعداً در صفحهٔ مدل هم می‌شود اضافه کرد.',
  'newModel.noBagsYet': 'هنوز کیفی انتخاب نشده است.',
  'newModel.bagCount': 'با این انتخاب، {count} کیف ثبت می‌شود و هر کدام کد خودش را می‌گیرد.',
  'newModel.save': 'ثبت مدل',

  'bags.fabrics': 'پارچه‌ها',
  'bags.coloursOf': 'رنگ‌های «{fabric}»',
  'bags.noFabrics': 'هنوز پارچه‌ای در «فهرست‌ها» ثبت نشده است.',
  'bags.noColours': 'هنوز رنگی در «فهرست‌ها» ثبت نشده است.',
  'bags.lockedHint': 'از قبل ثبت شده است. برای کنار گذاشتن، کلید کیفش را در فهرست بالا خاموش کنید.',
  'bags.inactive': 'غیرفعال',

  'model.number': 'شمارهٔ مدل',
  'model.category': 'دسته‌بندی',
  'model.noCategory': 'بدون دسته‌بندی',
  'model.inactiveOption': '{name} (غیرفعال)',
  'model.description': 'توضیح',
  'model.descriptionEmpty': 'توضیحی ندارد. برای نوشتن بزنید.',
  'model.activeLabel': 'فعال بودن مدل «{name}»',
  'model.inactiveNote':
    'این مدل غیرفعال است: در انتخاب‌های تازه نمی‌آید و کیف، سایز یا نرخ تازه نمی‌گیرد. هر چه قبلاً ثبت شده دست نمی‌خورد.',
  'model.notFound': 'این مدل پیدا نشد.',
  'model.sizes': 'سایزها',
  'model.sizesHint': 'برای تغییر نام، روی سایز بزنید. برای جابه‌جا کردن، دستگیرهٔ کنارش را بکشید.',
  'model.noSizes': 'این مدل سایز ندارد.',
  'model.addSize': 'سایز تازه',
  'model.firstSizeHint': 'کیف‌هایی که تا حالا ثبت شده‌اند همین سایز حساب می‌شوند و کدشان عوض نمی‌شود.',
  'model.nextSizeHint': 'هر کیف فعال این مدل در سایز تازه هم ثبت می‌شود.',
  'model.bags': 'کیف‌ها',
  'model.noBags': 'هنوز کیفی برای این مدل ثبت نشده است.',
  'model.bagActiveLabel': 'فعال بودن کیف {sku}',
  'model.addBags': 'افزودن کیف',
  'model.addBagsHint': 'پارچه و رنگ تازه را انتخاب کنید. آنچه از قبل ثبت شده، تیک‌خورده و قفل است.',
  'model.addBagsSizesHint':
    'پارچه و رنگ تازه را انتخاب کنید؛ هر کدام در همهٔ سایزها ثبت می‌شود. آنچه از قبل ثبت شده، تیک‌خورده و قفل است.',
  'model.addBagsNone': 'افزودن کیف',
  'model.addBagsButton': 'افزودن {count} کیف',
  'model.inactiveNoAdd': 'برای افزودن، اول مدل را فعال کنید.',

  'rates.title': 'نرخ کنترات',
  'rates.hint':
    'مزد هر تکه در هر مرحله، به ریال. برای همهٔ پارچه‌ها، رنگ‌ها و سایزهای این مدل و برای همهٔ کنتراتی‌ها یکی است. برای نرخ تازه، روی نرخ بزنید.',
  'rates.inactiveNoAdd': 'این مدل غیرفعال است و نرخ تازه نمی‌گیرد. برای ثبت نرخ، اول مدل را فعال کنید.',
  'rates.noStages': 'هیچ مرحلهٔ فعالی نیست. مراحل را در «مراحل کار» ثبت یا فعال کنید.',
  'rates.none': 'هنوز نرخ ندارد',
  'rates.noneTap': 'برای ثبت نرخ بزنید',
  'rates.since': 'از {date}',
  'rates.upcoming': 'از {date}:',
  'rates.change': 'ثبت نرخ تازه برای «{stage}»',
  'rates.history': 'تاریخچه ({count})',
  'rates.state.current': 'امروز اجرا می‌شود',
  'rates.state.future': 'هنوز شروع نشده',
  'rates.state.past': 'گذشته',
  'rates.state.replaced': 'اصلاح‌شده',
  'rates.enteredOn': 'ثبت: {date}',
  'rates.correct': 'اصلاح این نرخ',
  'rates.form.title': 'نرخ تازهٔ «{stage}»',
  'rates.form.correctTitle': 'اصلاح نرخ «{stage}»',
  'rates.form.current': 'نرخ امروز:',
  'rates.form.onThatDay': 'نرخ ثبت‌شده در همین تاریخ:',
  'rates.form.amount': 'مزد هر تکه',
  'rates.form.startsOn': 'از تاریخ',
  'rates.form.save': 'ثبت نرخ',
  'rates.form.cancel': 'انصراف',
  'rates.form.amountZero': 'نرخ باید بیشتر از صفر باشد. مرحله‌ای که مزد ندارد، نرخ هم نمی‌گیرد.',
  'rates.form.corrects': 'این نرخ جای نرخ همین تاریخ را می‌گیرد و آن یکی در تاریخچه «اصلاح‌شده» می‌ماند.',
  'rates.form.future': 'این نرخ از آن روز اجرا می‌شود و تا آن روز نرخ امروز می‌ماند.',
  'rates.form.futureNoRate': 'این نرخ از آن روز اجرا می‌شود.',
  'rates.form.superseded':
    'از {date} نرخ دیگری شروع شده که بعد از این تاریخ است؛ پس این نرخ امروز اجرا نمی‌شود و فقط در تاریخچه می‌ماند.',
  'rates.form.past': 'تاریخ گذشته است. کاری که قبلاً به کنتراتی داده شده، نرخ خودش را نگه می‌دارد.',
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
  'validation.too_small': 'این عدد باید بیشتر از صفر باشد.',
  'catalog.name_required': 'نام را بنویسید.',
  'catalog.name_too_long': 'این نام بیش از اندازه بلند است.',
  'catalog.name_taken': 'این نام قبلاً ثبت شده است.',
  'catalog.category_not_found': 'این دسته‌بندی پیدا نشد. صفحه را دوباره باز کنید.',
  'catalog.fabric_not_found': 'این پارچه پیدا نشد. صفحه را دوباره باز کنید.',
  'catalog.colour_not_found': 'این رنگ پیدا نشد. صفحه را دوباره باز کنید.',
  'catalog.category_inactive': 'این دسته‌بندی غیرفعال است. اول آن را در «فهرست‌ها» فعال کنید.',
  'catalog.fabric_inactive': 'این پارچه غیرفعال است. اول آن را در «فهرست‌ها» فعال کنید.',
  'catalog.colour_inactive': 'این رنگ غیرفعال است. اول آن را در «فهرست‌ها» فعال کنید.',
  'catalog.description_too_long': 'این توضیح بیش از اندازه بلند است.',
  'catalog.size_name_duplicate': 'این مدل سایزی با همین نام دارد.',
  'catalog.variant_exists': 'این کیف از قبل ثبت شده است. صفحه را دوباره باز کنید.',
  'catalog.size_required': 'این مدل سایز دارد؛ هر کیف باید یکی از سایزهایش باشد.',
  'catalog.size_not_allowed': 'این مدل سایز ندارد.',
  'catalog.size_not_found': 'این سایز پیدا نشد. صفحه را دوباره باز کنید.',
  'catalog.product_not_found': 'این مدل پیدا نشد.',
  'catalog.variant_not_found': 'این کیف پیدا نشد. صفحه را دوباره باز کنید.',
  'catalog.product_inactive': 'این مدل غیرفعال است. اول آن را فعال کنید.',
  'catalog.too_many_sizes': 'یک مدل بیش از بیست سایز نمی‌گیرد.',
  'catalog.size_order_outdated':
    'سایزهای این مدل در این فاصله عوض شده بود. فهرست تازه آمد؛ سایز را دوباره جابه‌جا کنید.',
  'production.name_required': 'نام مرحله را بنویسید.',
  'production.name_too_long': 'این نام بیش از اندازه بلند است.',
  'production.name_taken': 'مرحله‌ای با این نام هست.',
  'production.stage_not_found': 'این مرحله پیدا نشد. صفحه را دوباره باز کنید.',
  'production.stage_inactive': 'این مرحله غیرفعال است. اول آن را در «مراحل کار» فعال کنید.',
  'production.stage_order_outdated':
    'فهرست مراحل در این فاصله عوض شده بود. فهرست تازه آمد؛ مرحله را دوباره جابه‌جا کنید.',
  'production.product_not_found': 'این مدل پیدا نشد. صفحه را دوباره باز کنید.',
  'production.product_inactive': 'این مدل غیرفعال است. اول آن را در صفحهٔ مدل فعال کنید.',
  'production.rate_not_positive': 'نرخ باید بیشتر از صفر باشد.',
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
