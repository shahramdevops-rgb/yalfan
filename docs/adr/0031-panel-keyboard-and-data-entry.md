# ADR 0031 — The panel's keyboard and data entry

- Status: accepted
- Date: 2026-11-27 (1405/09/06)
- Written in: roadmap task 1.7d (the fourth part of the design system)

## Context

The roadmap asked for data entry rules (Enter to the next field, the first field focused,
today's date by default, the last choices remembered, a summary before every money action) and
for a page used start to finish with the keyboard alone. ADR 0030 left two things to this task:
where the focus goes when an IBAN's small form closes, and after a dialog on an iPhone, where
Safari does not focus a tapped button.

What the panel did when this task began:
- The first field of every entry form already took the focus (a new model, contractor or worker,
  a rate, an IBAN).
- **Enter in any field sent the whole form**: Enter after a new contractor's name sent a half
  filled form and came back with three refusals.
- A refused save left the focus where it was; only the IBAN's field took it back.
- When a small form closed (an IBAN, a rate, a worker's start date), the focus fell to the start
  of the page, and a keyboard lost its place. Opening the start date's form lost it too, since
  its line gives way to the form.
- The date lists show Persian digits («۱۵»), which the browser's own type-ahead matches only to
  Persian digits typed, never to the numpad's Latin ones: a day was reached with up to thirty
  presses of an arrow.

On 1405/09/06 the developer accepted every proposal of the plan as written.

## Decision

1. **Enter goes forward** in the panel's forms (`FormCard`, `InsetForm`; `shared/ui/formKeys.ts`):
   from a one-line field, a list, a chip or a switch to the next field; from the last field to
   the form's main button; on a button it presses it. Why: Iranian office software has taught
   the operator Enter for Tab, the numpad's Enter is under his right hand, and no form is sent
   half filled by an Enter pressed from habit; the save is always a deliberate Enter on a button
   that says what it does.
   - **A row of chips is one field.** Enter leaves the row; Space ticks a chip, as it flips a
     switch; Tab still walks chip by chip. Otherwise Enters pressed in a row tick and untick. A
     row is the chips' common parent element; a ticked fabric's colours are a row of their own.
   - **The notes keep Enter for a new line**, and Tab leaves them, because notes run to several
     lines.
   - **Kept as they were:** the forms that add one name at a time (`AddNameForm`, a model's
     sizes, `InlineName`) add or save on Enter, because the field is emptied for the next name;
     Enter in an empty size field moves on. The login page, outside the frame, keeps one field
     per step and its own Enter.
   - A plain button between fields (a retry, «افزودن» beside the size field) is not a stop.
     When the main button cannot be pressed yet (the IBAN's «ثبت» before a digit), Enter stays.
   - Inside these forms, the phone keyboard's Enter key says «بعدی» (`enterKeyHint`).
   - One handler on the form element, with no package; a field that handles Enter itself says
     so with `preventDefault`.
2. **Where the focus goes.**
   - **A refused save** puts the focus on the first wrong field: the first field marked wrong,
     or the first control of a group (chips, a date) holding a `FieldError`. A refusal of the
     whole form (`Notice`) moves nothing. The forms pass their save's promise, so the focus
     moves once the refusal is drawn.
   - **A small form that closes** (saved, cancelled, Escape) gives the focus back to the line
     that opened it (`InsetForm`'s `returnFocus`): the IBAN, the stage's rate, the start date.
     Only when the focus was inside the form or fell with it, so a click elsewhere keeps the
     focus it gave. The line is read after the page has drawn itself, since the start date's
     line is drawn anew; a correction opened from a rate's history returns to the stage's rate.
   - **A small form whose line gives way to it** focuses its first field when it opens (the
     start date's day list; `JalaliDateField` has `autoFocus`).
   - **On an iPhone**, `Modal` gives the focus back to the button that was pressed, not to what
     had the focus (`shared/ui/focusReturn.ts`): a pointer press is remembered until the next
     key. Safari leaves the focus on the body, or on a field typed in before, which would open
     the phone's keyboard when a dialog closes.
   - **The first field is focused only on a page whose purpose is entry.** The new user form
     under the users and the add-name fields under the lists do not take it, because it would
     scroll the page past what it is for and open the keyboard of a phone.
3. **A number typed on a date list picks it**, with Latin, Persian or Arabic digits: «۱۵» the
   15th, «۹» Azar, «۱۴۰۳» the year. The digits are kept while they begin a value and for one
   second, as the browser's own type-ahead does with letters; letters still find a month by its
   name. Why: the operator's hand is on the numpad, and the lists stay lists, so a day still
   cannot be mistyped (ADR 0024).
4. **No summary dialog before saving a rate.** A rate is corrected from the same card by the
   operator himself (ADR 0024), and a question before a step that can be taken back trains
   pressing through it unread (ADR 0030). Instead the rate form warns, while the amount is
   typed, when the new rate is five times the one it is measured against or more («یک صفر اضافه
   نزده‌اید؟»), or a fifth or less («مبلغ به ریال است، نه تومان»): a zero too many is ten
   times, Toman typed into the Rial field a tenth, and a real raise or cut stays far inside both.
   It is a warning, not a refusal. A new rate is measured against today's rate; a correction
   against the rate before the entry it corrects, never that entry, which may be the very
   mistake being put right (found while building: 1,500,000 corrected to 150,000 warned «Toman»). **The summary before a money action** («به … مبلغ … ریال بابت
   …») is built with the first payment (2.6), where money leaves and only a reversing entry
   undoes it.
5. **A date starts on today, except one that is almost always in the past**, where a today left
   in by mistake costs: a worker's start date still starts empty (ADR 0025). The rate form
   starts on the API's today (ADR 0024).
6. **Only the start date of a new rate is remembered**, while the model's page is open: the next
   new rate opens on it, with its hint («… از آن روز اجرا می‌شود») saying so. A raise is entered
   stage after stage on one date, and eight stages times three lists is the repetition Phase 1
   has. A correction's date is its entry's and is not kept; leaving the page forgets it, and
   nothing is written to the browser. The other choices to remember (the last contractor, stage,
   date of a work order) come with 2.2, where the repetition is: a remembered choice that nothing
   repeats is a hidden default, and a stage left ticked on a new contractor would put him under
   the wrong stage in Phase 2.

## Consequences

- A new form is a `FormCard` or an `InsetForm`, and gets Enter, the focus on a refusal and the
  phone's «بعدی» with no code of its own; an `InsetForm` names the line it returns the focus to.
- A field that handles Enter itself calls `preventDefault`, or the form moves on as well.
- The behaviour tests (`screens/keyboard.spec.ts`, desktop width) hold the proof: a whole new
  contractor with the keyboard alone, no request sent before the Enter on its button; the
  refusals' focus; a new model's switch, list, sizes and rows of chips; the rate form by
  keyboard, its warning, the digits typed on the lists and the remembered date; the IBAN's and
  the start date's focus; and a tap as Safari makes it (a press and a click, never a focus).
  With the new behaviour switched off, all seven fail.
- **Not done here:**
  - Escape in a date list does not close the rate's or the start date's form, as before, since
    an open list takes its own Escape; Escape from any other field or button does.
  - An iPhone and the Windows Chrome's open list (whether Enter reaches the page while a list is
    open: either way the list takes the choice) were not tried by hand.
