# ADR 0024 — The rate card screen, the money display, the amount and date fields

- Status: accepted
- Date: 2026-11-25 (1405/09/04)
- Written in: roadmap task 1.6.3 (the third part of 1.6)

## Context

Task 1.6.3 builds the first screen that shows and takes money: a model's piece rates (ADR 0021).
With it come three parts every later screen reuses: how an amount is shown (ADR 0002 point 9),
the field where an amount is typed, and the field where a Jalali date is picked. The rates API of
task 1.5 already answers everything the screen needs (a rate card on the workshop's today, the
history of a model, a new entry), so **the API does not change**.

The developer answered the plan on 1405/09/04; the proposals he did not change were accepted as
written, as for 1.6.1 and 1.6.2:

1. **A Rial amount that is not a multiple of ten** (12,345 Rial) is not rounded on the line under
   it: «هزار و دویست و سی و چهار تومان و پنج ریال» (the proposal, not changed).
2. **The check of 1.6** ("the operator enters a real model with its rates without help"): the
   developer does the operator's work for now («کار های اپراتور رو خودم انجام میدم فعلا»), so
   he makes that check himself.

During the browser check he changed point 9 of ADR 0002: **under a Rial amount, only the words
in Toman stay; the Toman figure goes** («اون رو پاک کن و همون حروف به تومن بمونه»).

## Decision

### The money display (changes ADR 0002 point 9)

```
۱۲۰٬۰۰۰ ریال
دوازده هزار تومان
```

- **One figure on screen, in Rial; under it the amount in words, in Toman,** in a smaller,
  quieter line. Why: a second figure in Toman next to the Rial one gave the eye two numbers to
  choose from; the words still say the amount the way people say it aloud, so a Toman typed into
  a Rial field still shows at once (twelve thousand Toman, not a hundred and twenty thousand).
- **Never rounded:** 12,345 Rial reads «هزار و دویست و سی و چهار تومان و پنج ریال», and 5 Rial
  «پنج ریال». Why: the line is a reading aid for the figure above it and must not say another
  amount.
- **Words as they are spoken:** «هزار», not «یک هزار»; «یک میلیون»; above a billion the count of
  billions is read out («دو هزار و پانصد میلیارد»); «صفر تومان»; a negative amount starts with
  «منفی», for the ledgers of Phase 2. The words are worked out on `BigInt`, so any int64 the API
  sends is read exactly.
- One component (`Money`) draws every amount, so the next screens cannot forget the line.

### The amount field

- Typed in **Rial**, with «ریال» in the label and inside the field; the digits are **grouped in
  threes in Persian digits while they are typed**, so a typed amount looks like a shown one.
- Persian, Arabic and Latin digits are taken; everything else (a separator, a letter, a space) is
  dropped, and a pasted «1,250,000 ریال» is read as 1250000. Leading zeros are dropped, but a lone
  zero stays so the form can refuse it with its message.
- The **caret keeps its place among the digits** when the separators move under it, so a digit
  typed in the middle of an amount does not throw the caret to the end.
- **At most 15 digits**, because a JavaScript number holds every whole number of 15 digits
  exactly and no amount the workshop types comes near it.
- The words line follows every key. There is no "this looks like Toman" warning: that would be a
  business rule nobody gave, and the words line does that job.

### The Jalali date field

- **Three lists, day, month (by name) and year**, in the order a Persian date is read, with the
  day written out under them with its weekday («سه‌شنبه ۱ دی ۱۴۰۵»). Why: nothing is typed, so
  nothing can be mistyped; a phone opens its own picker for each list; no library is needed. A
  pop-up calendar is larger to build and harder to use on a phone.
- The day list follows the month and the year: **Esfand has its 30th only in a leap year**, and a
  31st becomes the 30th when the month changes.
- **Both directions go through the browser's own Persian calendar (Intl)**, the one `formatDay`
  shows dates with. Why: a separate algorithm or library can disagree with the browser's
  calendar in some years, and the day picked would then be shown back as another day.
- On a phone the three lists stay on one row; the month takes the width the day and year leave.

### The rate card on the model's page

- **A «نرخ کنترات» card on the model's page, right under the name,** not a page of its own.
  Why: after a new model is saved the operator lands on this page, and the rates are what the
  model still lacks; the sidebar stays short.
- **Each active stage, in the order of the work,** with the rate in force today, its start date,
  and «هنوز نرخ ندارد» when it has none. A deactivated stage is left out (ADR 0022).
- **A raise already entered for later shows on the card** («از ۱ دی ۱۴۰۵: ۱۵۰٬۰۰۰ ریال»). Why:
  someone who entered next month's raise and did not see it would think it was lost and enter it
  again.
- **A tap on the rate opens a small form** under the stage: the amount, the start date (today by
  default), «ثبت نرخ» and «انصراف»; Escape closes it too. This form has buttons, unlike the
  names edited in place, because two fields are saved together and saving on leaving a field
  would store half a rate. The amount starts empty with today's rate shown above it, because a
  pre-filled amount is too easily saved again unchanged.
- **A correction is a tap on an entry of the history:** the form opens on that entry's start
  date, and saving replaces it (ADR 0021). Whatever the date, the form says what saving will do:
  replace the rate of that date, start later while today's stays, or, for an old date, only join
  the history because a later rate has already started. Why: without this the operator cannot
  know that a correction is "the same date again".
- **The history** of each stage opens under it: newest first, each entry labelled «امروز اجرا
  می‌شود», «هنوز شروع نشده», «گذشته» or «اصلاح‌شده», with the day it was entered. It comes with
  the card in one request for the whole model, because it is a few rows a year and opening it
  should not wait for the network. When a saved rate shows neither as today's nor as the next
  one, its stage's history opens by itself, so the operator sees where it went.
- **"Today" is the API's** (the workshop's day in Tehran, in the card's answer), not the office
  PC's clock, which may be wrong. The years offered run from ten years back, for rates copied
  from the paper book, to one year ahead.
- **An inactive model** shows its rates and history but takes no new rate and no correction
  (ADR 0023).

## Not decided here

- **Who entered a rate** is not shown: the history shows when; who is the owner's change history
  (1.6.5).
- **A page with every model's rates side by side**, to compare or print: when the owner asks.

## Consequences

- ADR 0002 point 9, CLAUDE.md and ARCHITECTURE.md now say "the words in Toman" instead of "the
  Toman equivalent and its words".
- Every later screen with money (work orders, payslips, the ledgers, prices) uses `Money` and
  `AmountField`, and every date it takes uses `JalaliDateField`.
- The panel is checked in Chrome against a throwaway database, as for 1.6.1 and 1.6.2.
