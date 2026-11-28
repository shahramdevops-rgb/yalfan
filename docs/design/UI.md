# The panel's UI rules

How screens of the staff panel (`web/panel`) are built, so every session builds them the same way.
Begun in task 1.7b (ADR 0029); 1.7c added the overlays (ADR 0030), 1.7d the keyboard and data
entry (ADR 0031), 1.7e the data table (ADR 0032). Printing comes with its first use, the work
order slip (2.9). Read this before building or changing a screen.

Where things are:
- Colours: `web/panel/src/index.css`, the only place a colour value is written.
- Shared parts: `web/panel/src/shared/ui/`.
- The colour check: `web/panel/checks/colours.test.ts` (part of `npm test`).
- The screenshots of the main pages and the behaviour tests: `web/panel/screens/` (ADR 0028,
  ADR 0030).
- The prototypes of Phase 2's daily flows, and the owner's choice among them: `docs/design/flows.md`
  (task 2.0).

## Who it is for

The owner (often on his phone), the workshop manager, and one office operator who is not
technical (CLAUDE.md). So: few fields per screen, workshop words, large targets (fields and
buttons `h-12`), safe defaults, and no button where a direct action does the job (a name is
edited by tapping it, a switch turns a thing on and off, a list is put in order by dragging;
docs/ARCHITECTURE.md section 9).

## Colours

Only these names, by what they are for. Tailwind's own palette is switched off: `text-stone-500`,
`bg-white`, `bg-[#fff]` or a class built from pieces (`bg-${tone}`) fails the colour check.

| Name | For |
|---|---|
| `page` | The ground behind the cards: the cream of the workshop's business card |
| `surface` | A card, a field, the sidebar and the header |
| `subtle` | A row under the pointer; a sunken box inside a card (the whole IBAN, a rate's history) |
| `fill` | A badge, a loading bar, a quiet button under the pointer |
| `fill-strong` | A stronger fill under the pointer |
| `fill-off` | A switch turned off, a disabled main button |
| `line-soft` | Between the rows of a list; the line above a form's button |
| `line` | The edge of a card, the header and the sidebar |
| `line-strong` | The edge of a field, a chip, a secondary button |
| `ink` | Names, amounts, what is typed: the main text |
| `ink-soft` | Text a little quieter than `ink` (a bag's fabric and colour, a secondary button) |
| `label` | A field's label, a menu item |
| `body` | A sentence that is not the point of the page (an empty list, a status line) |
| `muted` | A hint, the line under a name, the Toman words under an amount |
| `faint` | Inactive, disabled, a field's empty example |
| `brand` | The olive green of the workshop's logo: the main button, a link, the focus ring, a switch turned on |
| `brand-strong` | The main button under the pointer; a chosen item's text; a model's number |
| `brand-line` | The edge of the field being typed in; a chosen chip's edge |
| `brand-edge` | The frame of a small form opened inside a card (`InsetForm`) |
| `brand-glow` | The glow around the field being typed in |
| `brand-soft` | Behind a chosen item (the menu, a chip, the current rate) |
| `on-brand` | Text and marks on `brand` |
| `danger`, `danger-strong`, `danger-line`, `danger-edge`, `danger-soft` | A refusal, a wrong field (`danger-line`), a step that takes something away |
| `warning`, `warning-strong`, `warning-soft`, `warning-fill` | Something to notice before going on: an inactive record, a missing IBAN or rate, a raise to come |
| `success` | Something saved that the page does not show by itself |

- Each `-soft` ground takes its `-strong` text («این مدل غیرفعال است…» is `warning-strong` on
  `warning-soft`).
- Text on the `page` ground (outside a card) is `body` or darker, never `muted`: `muted` on the
  cream is 4.26 to 1, below the 4.5 text needs; inside a white card it is 4.81.
- A state is never told by colour alone: it always has a word (`StatusBadge`, `Notice`).
- A new colour is a new name, added on purpose to `index.css` and to this table, never written
  out in a page. There is no `info` colour yet (ADR 0029 point 4).
- No dark theme; Chrome's Auto Dark Mode is left alone (ADR 0014).

## Sizes and spacing

Tailwind's own scales, these steps:
- Page: at most `max-w-3xl` wide; `gap-6` between its parts (`gap-4` in a loading or error state).
- Page title `text-2xl font-bold` (`PageHeader`); card title `text-lg font-bold` (`CardHeader`);
  a smaller title inside a card `font-bold` (`h3`).
- Text `text-base`; hints and labels `text-sm`; badges and the line under an amount `text-xs`.
- Card: `rounded-2xl`, `p-4` on a phone and `p-6` from `sm`, `shadow-sm`, `ring-1 ring-line`.
- Fields and buttons: `h-12 rounded-lg`; chips `h-10 rounded-full`.
- Only logical sides (`ms-`, `pe-`, `start-`, `text-end`…); a physical one fails the lint
  (ADR 0015), because the store will also be left to right.

## Parts

Use a part when one fits; a page writes its own classes only for its own layout. No tool enforces
this (only the colours are checked by a test); the review does.

| Part | Use it for |
|---|---|
| `PageHeader` | The top of every page in the frame: the back link, the one `h1`, a sentence under it, the page's own button or link. On a record's own page it carries only the back link and «تاریخچه», and the record's name is the `h1` (`InlineName heading`) |
| `Card`, `CardHeader` | One part of a page in a white panel, with its title and hint. `gap` 4 for a list, 5 by default, 6 or 8 for a long form |
| `FormCard` | A whole page's form in a card (a new model). Give it `labelledBy` when it is one part among several on the page (the new user under the users). Pass the save itself (`onSubmit={submit}`), not `() => void submit()`: a refusal moves the focus once the save's promise has answered |
| `FormFields` | The short fields of a form, one under the other, no wider than they need (`sm:max-w-md`) |
| `FormSection` | A part of a long form with its own title |
| `FormActions` | The end of a form, under a thin line: what saving will do, the refusal (`Notice`), the button (`self-start`) |
| `InsetForm` | A small form opened inside a card by a tap (a new IBAN, a new rate), with «ثبت» and «انصراف». `returnFocus` is the line that opened it, which gets the focus back when it closes; its first field takes the focus when it opens |
| `Field` | A label over something that is not one text field: chips, a name edited in place, a date. It is a group named by the label; never put a `TextField` in it, which has its own |
| `FieldError` | What is wrong with one field, under it |
| `Notice` | A sentence in a soft box: `warning` before going on, `danger` for a refused save |
| `StatusBadge` | A short state beside a name: «غیرفعال» (`muted`), «جاری» (`brand`), «از آینده» (`warning`) |
| `Button` | An action: `primary` (one per form), `secondary`, `danger` (a step that takes something away), `link` |
| `ButtonLink` | A link to another page that looks like a button («مدل جدید») |
| `TextLink` | A quiet link to another page («بازگشت به فهرست», «تاریخچه») |
| `TextField`, `TextAreaField` | Text typed with its label, hint and error |
| `AmountField`, `Money` | An amount typed, or shown: always Rial, with the Toman words under it (ADR 0002, ADR 0024). Never write an amount out by hand |
| `JalaliDateField` | A day picked from three lists (ADR 0024); a number typed on a list picks it, in Latin or Persian digits (ADR 0031) |
| `IbanField` | An IBAN typed (ADR 0025) |
| `Chip` | A choice ticked by pressing it (a fabric, a stage, a role) |
| `Switch` | On and off, instead of two buttons |
| `InlineName` | A text edited where it stands: a tap, Enter or leaving saves, Escape puts it back |
| `NameList`, `SortableList`, `AddNameForm` | The simple lists: a name, a switch, a handle to drag; adding names one after another |
| `DataTable` | A list with several things to say about each row (a model's number, name, category and bags): a table from `sm` up, whose header stays at the top of the window, and a card per row on a phone. Each column says its `place` on the card (`lead`, `title`, `detail`, `end`), which also sets how it reads in the table. The whole row opens its page, but only the name is a link; an inactive row is greyed and marked (ADR 0032) |
| `PagedTable` | A searched, paged list in a `DataTable`, with its loading, error and empty states and the `Pager` under it: the models, the contractors, the workers |
| `SearchField`, `Pager` | A long list's search box and its pages |
| `ListSkeleton`, `LoadError` | A list still loading; a list that did not load, with «تلاش دوباره» |
| `ConfirmDialog` | The question before a step its person cannot take back from the panel: removing an IBAN, switching off one's own login number. Says what happens and to which record; its button says what it does; «انصراف» has the focus (ADR 0030) |
| `Drawer` | A panel sliding over the page from the inline start: the sidebar on a phone |
| `Modal` | The base of both, the browser's own `<dialog>`; a new overlay is built on it, never on a positioned `div` |
| `toast()` | A short message at the bottom that goes by itself, for something done that the page does not show («کپی شد»). `Toaster` is drawn once, in `App` |

## Rules

- Every word comes from `src/shared/i18n/fa.ts` (the lint refuses Persian in a component).
- One `h1` per page; card titles are `h2`.
- A form checks its own fields and says what is wrong in Persian (`noValidate`), and a refused
  save puts the focus on the first wrong field: one marked `aria-invalid`, or the first control
  of a group with a `FieldError` (ADR 0031).
- A step that cannot be undone says what it will do on its button («افزودن ۶ کیف»; ADR 0023).
  The summary dialog before a money action («به … مبلغ … ریال بابت …») comes with the first
  payment (2.6); a rate, corrected from its own card, has none, and its form warns of an amount
  five times the one it is measured against, or a fifth of it (ADR 0031).
- A list with columns is a `DataTable`, never a table or a list of links written in the page; a
  list edited where it stands (a model's bags: a switch and a refusal per row) stays a list. No
  sorting by a heading: each list has the workshop's order and a search box. The totals row comes
  with the first screen that adds up a column (2.9 or 2.10), printing with the work order slip
  (2.9; ADR 0032).

## The keyboard and data entry

The operator copies from paper with one hand on the numpad; a page must be usable from start to
finish with the keyboard alone (ADR 0031). `FormCard` and `InsetForm` do all of this with no code
in the page:
- **Enter goes forward**: from a field, a list, a chip or a switch to the next field, from the
  last field to the form's main button, and on a button it presses it. No form is sent by an
  Enter in the middle.
- **A row of chips is one field**: Enter leaves the row, Space ticks a chip (and flips a
  switch), Tab walks chip by chip. A row is the chips' common parent, so give each row its own
  element.
- **Notes keep Enter for a new line**; Tab leaves them.
- **A field that adds one thing at a time** (`AddNameForm`, a model's sizes, `InlineName`) adds on
  Enter; such a field inside a form calls `preventDefault` on that Enter, and lets an empty
  field's Enter move on.
- **The first field takes the focus** on a page whose purpose is entry (a new model) and in a
  small form opened by a tap; never on a page where the form is one part among others (the new
  user under the users, a name added under a list), where it would scroll past the page's point.
- **When a small form closes**, the focus goes back to the line that opened it (`returnFocus`);
  a dialog gives it back to the button that opened it, also on an iPhone.
- **A date starts on today**, except one that is almost always in the past, where a forgotten
  today costs (a worker's start date starts empty, ADR 0025).
- **A choice is remembered only where it repeats**, and only while the page is open: the start
  date of the last new rate on a model's page. Nothing is written to the browser's storage for
  it.
- **When to ask first** (`ConfirmDialog`): only for a step that takes away what its person cannot
  bring back from the panel himself. A switch is undone by switching back and asks nothing; a
  button that says what it makes asks nothing. A question on every step is pressed through
  unread, and then protects nothing when it matters (ADR 0030).
- **A short confirmation is a `toast()`**, never a line left in the page. A refusal is never a
  toast: it stays beside its field (`FieldError`) or at the end of the form (`Notice`) until it
  is put right.
- The login page and the page shown after a crash are the two pages outside the frame; they keep
  their own centred layout and do not use `PageHeader`.

## Checking a change

- `npm test` runs the colour check with the other tests.
- `npm run screens` compares the main pages with their pictures, one changed pixel failing
  (ADR 0028). A change that should look the same must leave every picture as it is. A change
  meant to show is taken with `npm run screens:update`, looked at, and committed with its
  pictures. The pictures show neither the pointer nor the keyboard's focus.
- What only a browser shows (the focus, the keyboard, an overlay opening and closing) is tested in
  `screens/*.spec.ts` with Playwright, in the same image and against the same sample API
  (`screens/overlays.spec.ts`, ADR 0030; `screens/keyboard.spec.ts`, ADR 0031;
  `screens/table.spec.ts`, ADR 0032); `npm run screens` runs them with the pictures. A list's rows
  are in the page twice (the table and the cards): find one by its role, or filter its text on
  the visible copy. A new entry form gets a keyboard test: its fields reached by Enter,
  nothing sent before the Enter on its button. Logic without a page stays in Vitest.
