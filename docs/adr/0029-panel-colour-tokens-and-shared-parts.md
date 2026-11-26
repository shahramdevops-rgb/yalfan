# ADR 0029 — The panel's colours by role, Tailwind's palette off, and the shared page parts

- Status: accepted
- Date: 2026-11-27 (1405/09/06)
- Written in: roadmap task 1.7b (the second part of the design system)

## Context

After the 1.6 screens the panel had 376 raw colour classes in 39 files: ten shades of Tailwind's
`stone`, the brand's `teal` (behind five `brand-*` aliases), `red`, `amber` and `green`. Nothing
said which shade is for what, so the same role got different shades on different pages, and a
shade that does not exist (`ring-brand-200` on three small forms) silently fell back to the text
colour. The same pieces of page were written again and again: the «غیرفعال» badge five times,
the card's frame eight times by hand next to `Card`, `Field` twice, the back link nine times, the
«جدید» link twice, the soft warning or error box ten times. Three pages had no `h1`.

Phase 2 brings some forty screens, used every day. The developer asked for a design system before
it (1.7), and on 1405/09/06 accepted this task's plan with every proposal as written, while
saying he leaves the look to the session.

## Decision

1. **Colours are named by their role, not by their shade**, in `web/panel/src/index.css`:
   grounds (`page`, `surface`, `subtle`, `fill`, `fill-strong`, `fill-off`), lines (`line-soft`,
   `line`, `line-strong`), text (`ink`, `ink-soft`, `label`, `body`, `muted`, `faint`), the
   workshop's colour (`brand`, `brand-strong`, `brand-line`, `brand-edge`, `brand-glow`,
   `brand-soft`, `on-brand`), and the states `danger-*`, `warning-*` and `success`. The table
   of what each one is for is in `docs/design/UI.md`. Why: a page says what a colour is for
   (`text-muted`, `bg-danger-soft`), and a colour is changed in one line for every page.
2. **Each role took the exact value it had**, Tailwind's own value written in the file with its
   old name in a comment. Why: the 28 screenshots of ADR 0028 then prove the move of 376 classes
   changed nothing; they passed with no pixel changed before any part was touched. Merging the
   six greys of text into fewer, or a new colour, is later a change of value here, one line, and
   is seen in the pictures on its own. `brand-edge` (teal-200) is the one new value: the frame
   of the three small forms always meant a light brand line.
3. **Tailwind's palette is switched off** (`--color-*: initial`), so `text-stone-500` makes no
   CSS. **`web/panel/checks/colours.test.ts`** gives every colour class in `src` to Tailwind's
   own compiler, as the build does, and fails on each one that makes nothing, naming the file:
   a raw colour typed from habit, and a misspelt role (`hover:bg-subtel`), which would otherwise
   silently lose its colour under the pointer or the keyboard's focus, where no screenshot looks.
   A class is found under any variant, brackets included (`hover:`, `data-[state=on]:`, `@md:`,
   `[&>p]:`). It also fails on a colour written out (`bg-[#fff]`, `#fff`, `rgb(…)`) and on a
   class built from pieces (`` `bg-${tone}` ``, `'bg-' + tone`), which Tailwind never finds. Why
   a test and not a lint rule: one check covers both mistakes, and a lint rule cannot know which
   names exist.
   - The test lives in `web/panel/checks/` with the Node project (`tsconfig.node.json`), because
     the app's own project has no Node types and should not get them.
4. **No `info` colour yet**, although the roadmap listed one: no page shows information in a
   colour of its own, and a colour that nothing uses is never seen in a picture. The first page
   that needs it adds it here and in `UI.md`.
5. **Spacing, type sizes, corners and shadows stay Tailwind's own scales**, already defined in
   one place; `UI.md` says which steps the panel uses (a page title `text-2xl`, a field `h-12`,
   a card `rounded-2xl`).
6. **Shared parts for what was repeated** (in `web/panel/src/shared/ui`):
   - `PageHeader`: the back link, the page's one `h1`, a sentence under it, the page's own button
     or link. On a record's own page (a model, a contractor, a worker), whose name is edited
     where it stands, the name itself is the `h1` (`InlineName heading`), because moving it out
     of the card would part it from its «فعال» switch; it looks exactly as before. Its button is
     then read by the name alone, so the page's title is not «تغییر نام …» (found by the review).
   - `StatusBadge`: a state beside a name, always a word and never a colour alone.
   - `ButtonLink` (a link that looks like a `Button`, from the same classes, `buttonStyle.ts`)
     and `TextLink` (the quiet link, now with the brand's focus ring).
   - `Notice` (the soft warning or refusal box; a refusal is announced at once), and the form
     layout: `Field`, `FieldError`, `FormFields`, `FormSection`, `FormActions`, `InsetForm`, with
     `FormCard` and `CardHeader` beside `Card`.
   - `Field` is now a group named by its label (`role="group"`), for every use: a name, a mobile
     or a date edited where it stands is a button that shows only the value («۰۹۱۲ …»), and the
     group's name tells a screen reader what that value is. A text field inside one would be
     announced twice, so a text field takes `TextField`, not `Field`.
   - A `FormCard` that is one part among several on its page is named by its title
     (`labelledBy`), which makes it a named form: the new user under the user cards. A page's
     only form, under the page's `h1`, needs no name.
7. **Three pictures changed on purpose** while the screens moved onto the parts, each to look
   like the rest: the history page's header (back link above a `text-2xl` title, like every
   page), the new-user form's end (a thin line, then the button, like the other two forms), and
   the dashboard card's padding on a phone (16 px, like every card). The other 23 stayed equal
   to the pixel.
8. **Left as they are:** the login page and the page shown after a crash, the two pages outside
   the panel's frame, keep their own centred layout.

## Consequences

- A new screen uses the roles and the parts. Only the colours are enforced by a tool: a raw
  colour fails `npm test` and `check.ps1`. Using a part instead of writing the same classes
  again is a rule of `UI.md`, checked by the review, not by a tool.
- A new colour is a new role: a line in `index.css` and a row in `UI.md`, chosen on purpose.
- Prettier's Tailwind plugin knows the roles (it reads `index.css`), so it sorts them like any
  other class; a misspelt one is sorted first, which is also a hint.
- The overlays (dialog, toasts, picker), the keyboard rules, the data table and printing are
  1.7c and 1.7d.
