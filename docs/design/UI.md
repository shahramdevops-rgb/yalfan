# The panel's UI rules

How screens of the staff panel (`web/panel`) are built, so every session builds them the same way.
Begun in task 1.7b (ADR 0029); 1.7c adds the overlays and the keyboard rules, 1.7d the data table
and printing. Read this before building or changing a screen.

Where things are:
- Colours: `web/panel/src/index.css`, the only place a colour value is written.
- Shared parts: `web/panel/src/shared/ui/`.
- The colour check: `web/panel/checks/colours.test.ts` (part of `npm test`).
- The screenshots of the main pages: `web/panel/screens/` (ADR 0028).

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
| `page` | The ground behind the cards |
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
| `brand` | The main button, a link, the focus ring, a switch turned on |
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
| `FormCard` | A whole page's form in a card (a new model). Give it `labelledBy` when it is one part among several on the page (the new user under the users) |
| `FormFields` | The short fields of a form, one under the other, no wider than they need (`sm:max-w-md`) |
| `FormSection` | A part of a long form with its own title |
| `FormActions` | The end of a form, under a thin line: what saving will do, the refusal (`Notice`), the button (`self-start`) |
| `InsetForm` | A small form opened inside a card by a tap (a new IBAN, a new rate), with «ثبت» and «انصراف» |
| `Field` | A label over something that is not one text field: chips, a name edited in place, a date. It is a group named by the label; never put a `TextField` in it, which has its own |
| `FieldError` | What is wrong with one field, under it |
| `Notice` | A sentence in a soft box: `warning` before going on, `danger` for a refused save |
| `StatusBadge` | A short state beside a name: «غیرفعال» (`muted`), «جاری» (`brand`), «از آینده» (`warning`) |
| `Button` | An action: `primary` (one per form), `secondary`, `danger` (a step that takes something away), `link` |
| `ButtonLink` | A link to another page that looks like a button («مدل جدید») |
| `TextLink` | A quiet link to another page («بازگشت به فهرست», «تاریخچه») |
| `TextField`, `TextAreaField` | Text typed with its label, hint and error |
| `AmountField`, `Money` | An amount typed, or shown: always Rial, with the Toman words under it (ADR 0002, ADR 0024). Never write an amount out by hand |
| `JalaliDateField` | A day picked from three lists (ADR 0024) |
| `IbanField` | An IBAN typed (ADR 0025) |
| `Chip` | A choice ticked by pressing it (a fabric, a stage, a role) |
| `Switch` | On and off, instead of two buttons |
| `InlineName` | A text edited where it stands: a tap, Enter or leaving saves, Escape puts it back |
| `NameList`, `SortableList`, `AddNameForm` | The simple lists: a name, a switch, a handle to drag; adding names one after another |
| `SearchField`, `Pager` | A long list's search box and its pages |
| `ListSkeleton`, `LoadError` | A list still loading; a list that did not load, with «تلاش دوباره» |

## Rules

- Every word comes from `src/shared/i18n/fa.ts` (the lint refuses Persian in a component).
- One `h1` per page; card titles are `h2`.
- A form checks its own fields and says what is wrong in Persian (`noValidate`). The rules for
  entering data by keyboard (the first field focused, Enter to the next one, the last choices
  remembered) come in 1.7c.
- A step that cannot be undone says what it will do on its button («افزودن ۶ کیف»; ADR 0023). The
  summary before every money action comes in 1.7c.
- The login page and the page shown after a crash are the two pages outside the frame; they keep
  their own centred layout and do not use `PageHeader`.

## Checking a change

- `npm test` runs the colour check with the other tests.
- `npm run screens` compares the main pages with their pictures, one changed pixel failing
  (ADR 0028). A change that should look the same must leave every picture as it is. A change
  meant to show is taken with `npm run screens:update`, looked at, and committed with its
  pictures. The pictures show neither the pointer nor the keyboard's focus.
