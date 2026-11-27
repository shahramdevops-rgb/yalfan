# ADR 0030 — The panel's overlays on the browser's own dialog; toasts; behaviour tests in Playwright

- Status: accepted
- Date: 2026-11-27 (1405/09/06)
- Written in: roadmap task 1.7c (the third part of the design system)

## Context

The panel had no overlay but its phone drawer, a hand-made `div` that neither closed on Escape
nor kept the focus inside it, so Tab walked on into the page hidden behind it. A contractor's or
worker's IBAN was removed with one click and no question, although the number is shown nowhere
again and must be typed anew from the card. Short confirmations («کپی شد», «… اضافه شد») were
written into the page beside the button and stayed there.

The roadmap planned a confirmation dialog, toasts, a searchable picker and the drawer «on
accessible primitives», proposing Radix UI, and left the library, and whether the panel gets
component tests (open since 1.6.4, ADR 0025), to this task. On 1405/09/06 the developer accepted
every proposal of the plan as written:
- 1.7c was split in three: 1.7c the overlays, 1.7d the keyboard and data entry, 1.7e the data
  table and print;
- the overlays are built on the browser's own `<dialog>`;
- the searchable picker waits for 2.1;
- behaviour tests run in Playwright, in the screenshots' image.

## Decision

1. **Every overlay is the browser's own modal `<dialog>`** (`shared/ui/Dialog.tsx`), opened with
   `showModal()`, with no package. Why: the browser puts it above everything, makes the rest of
   the page inert, so neither Tab nor a screen reader reaches what is behind it, and closes it on
   Escape. Those are the three things a hand-made overlay usually gets wrong, as the drawer did.
   Radix's dialog alone brings 15 direct dependencies, its toast 12 (ADR 0014's care with
   packages). Chrome and the iPhone's Safari have had it since 2022 (Safari 15.4).
   - `Modal` is the base. It exists only while open: drawing it opens it, and the page closes it
     by no longer drawing it, so the page's own state is the one truth. Escape and a tap on the
     dimmed page call the page's `onClose`; the browser is not left to close it on its own. When
     it closes, the focus goes back to what opened it. Escape stops at it, so a form around it
     that closes on Escape (as an IBAN's small form does) would not close too; this is a guard,
     since the IBAN's question is drawn beside its form, not inside it. A tap counts as outside
     only when the press both begins and ends on the dimmed page: a selection dragged out of the
     box, or a press slid onto one of its buttons, is not one (found by the review).
   - While one is open the page behind it does not scroll (`html:has(dialog:modal)` in
     `index.css`).
2. **`ConfirmDialog`: the question before a step the person cannot take back from the panel
   himself.**
   - The question says what will happen and which record it is (the IBAN's last four digits,
     the number being switched off). Its button is named by what it does («پاک کردن شبا»), and
     sits beside «انصراف».
   - **«انصراف» has the focus** when it opens, so an Enter pressed from habit takes nothing away;
     Escape and a tap outside cancel too.
   - It is only the question. On the confirm button the page closes it and runs the step with its
     own busy state and refusal, as before, so a refusal still shows where it did.
   - **Asked now:** removing an IBAN, and switching off a login number of one's own account,
     which logs the owner out of every device at once and is switched back on only by another
     owner account (ADR 0026).
   - **Not asked:** the other switches (a user, a number of another account, a model, a bag),
     because switching back undoes them. Adding bags and sizes also asks nothing, because its
     button already says what it makes (ADR 0023). Why so few: a question on every step trains
     the operator to press through it unread, and then it protects nothing when it matters, a
     payment in Phase 2.
3. **`Drawer`** is the phone's sidebar on the same base: it slides from the inline start, and
   Escape, a tap on the dimmed page, its close button or a link closes it. It also closes when
   the window grows to the desktop's width (a tablet turned sideways), where the sidebar shows
   itself, so it never stays over a page it leaves inert (found by the review; the old drawer
   was hidden there by CSS, which an open modal must not be).
4. **Toasts** (`shared/ui/toasts.ts`, `Toaster.tsx`), made by hand, because they are a list and
   a timer, about a hundred lines, with no focus to manage.
   - What they are for: something done that the page does not show by itself («کپی شد», a new
     user «… اضافه شد», whose card may be off the screen).
   - Never a refusal, which stays beside its field until it is put right; and never the only
     record of a money step.
   - How they behave: one region at the bottom of the screen, `role="status"`, always present
     and empty, because a screen reader announces only what is added to a region that already
     exists. Each message stays five seconds, and all wait while the pointer is on one; that
     pause ends by itself if the message under the pointer is pushed out by newer ones, since its
     pointer can then never leave it (found by the review). The same message again restarts its
     time instead of showing twice, and at most three are shown, the oldest going first.
5. **The panel's behaviour tests run in Playwright, inside the screenshots' pinned image**
   (`screens/overlays.spec.ts`), against the same sample API (ADR 0028), and not in Vitest with
   jsdom and Testing Library. Why: no new package, and a real Chromium, where focus, `inert` and
   the dialog behave as on the office PC; jsdom does not fully model the dialog or the focus.
   This answers the question of component tests left open by ADR 0025.
   - They check the drawer (the focus stays inside it through more Tabs than it has stops,
     Escape closes it and gives the focus back, the page behind does not scroll, a tap beside it,
     a link and a window grown to the desktop's width close it), the IBAN's question (Enter,
     Escape and a tap outside cancel and send nothing, a press slid in from outside does not,
     Escape leaves the small form open, only the question's own button sends the `DELETE`, the
     last four digits are written out in the test rather than worked out with the panel's own
     formatter), the own number's question, and a toast announced in the status region.
   - Each overlay's look is a picture too: the IBAN's question at both widths, the open drawer
     on a phone, the toast. That makes 32 pictures, and the 28 before stayed equal to the pixel.
   - The sample API answers the writes these tests send, without keeping them. The page-opening
     steps shared with the screenshots moved to `screens/open.ts`.
   - Vitest keeps testing logic (`toasts.test.ts`: time, pause, the same message, the oldest
     dropped).
6. **The searchable picker waits for 2.1**, the first screen that picks a model, and is reused
   in 2.2 for the contractor. Why: no Phase 1 screen picks a model or a contractor, a part
   nothing uses is seen by no picture and no test, and 2.0's prototypes may show that the five
   to fifteen contractors of one stage are better as buttons than behind a search box.

## Consequences

- A step that takes away what its person cannot bring back asks with `ConfirmDialog`; anything
  else does not. A new overlay is a `Modal`, never a positioned `div`.
- A short confirmation is a `toast()`, never a line left in the page; a refusal never is.
- A behaviour that only a browser shows (focus, keyboard, an overlay) is tested in
  `screens/*.spec.ts`; `npm run screens` and `check.ps1`'s «panel screens» step run them with the
  pictures, in about the same fifteen seconds.
- **Not done here:**
  - After the IBAN is removed or saved, its small form closes and the focus falls back to the
    page, not to the IBAN; that is for 1.7d, with the other keyboard rules.
  - The focus goes back to the element that had it when the dialog opened. Safari does not focus
    a button that is tapped, so on an iPhone there is nothing to go back to and the focus stays
    on the page; also for 1.7d.
  - Whether the back gesture of an Android phone closes the drawer was not tried; it depends on
    the browser, not on this code.
