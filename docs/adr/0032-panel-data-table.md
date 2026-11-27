# ADR 0032 — The panel's data table; the totals row and printing wait for their first use

- Status: accepted
- Date: 2026-11-27 (1405/09/06)
- Written in: roadmap task 1.7e (the fifth and last part of the design system)

## Context

The roadmap asked for a data table (a sticky header, a totals row, paging, an empty state, rows
as cards on a phone), a print stylesheet, and an A5/A4 layout for slips and statements, the
latter "used from 2.9".

What the panel had when this task began:
- Three searched, paged lists (the models, the contractors, the in-house workers), each with the
  same code written out again: the grey bars while loading, the error with «دوباره امتحان کنید»,
  the empty sentence, the dimmed rows while the next page loads, a page past the last one
  replaced by the last, and the pager. Each row was a link of two lines, laid out by its page.
- No screen that adds up a column.
- Nothing printed: the first printed paper is the work order slip of 2.9, whose content 2.0's
  prototypes decide. The office has no computer or printer yet (O.4), and whether it will print
  on A4, on A5 or on an 80 mm receipt roll is not known; each needs a different layout.

On 1405/09/06 the developer accepted every proposal of the plan as written.

## Decision

1. **`DataTable` (`shared/ui/DataTable.tsx`) shows a list with several things to say about each
   row.** From the `sm` width (640 px) up it is a real `<table>`; on a phone each row is a card.
   - **Both are drawn, and the width shows one** (`hidden sm:table`, `sm:hidden`), rather than one
     table turned into cards by CSS. Why: Safari stops reading a table as a table once its rows'
     `display` changes, and the cards can then keep the rows the phone already had. Cost: the
     rows are in the page twice, twenty at most; the hidden copy is `display: none`, so neither
     Tab nor a screen reader reaches it. A test that finds a row by its text filters on the
     visible copy. The cards' `<ul>` carries `role="list"`, because Safari stops reading a list
     as one when its bullets are taken away (found by the review).
   - **One list of columns gives both layouts.** Each column has a heading, a cell, and its
     `place`: `lead` (a model's number, bold in the workshop's colour), `title` (the name),
     `detail` (on a phone, the line under the name, the details joined with « · », an empty one
     left out), `end` (a mobile number, at the far end). The place also sets how the cell reads in
     the table, so a page writes no colour of its own. **Exactly one column is the `title`**: with
     none a row would have no link, so no way in by the keyboard, and with two, two stops. Any
     other number throws, the crash page shows, and the page's screenshot test fails (found by the
     review).
   - **The header stays at the top of the window** while the rows scroll under it (`sticky` on
     each `<th>`), because twenty rows are taller than a laptop's screen. No box around the table
     scrolls on its own: that would hold the header inside the box instead.
   - **The whole row opens its page, but only the name is a link.** One Tab per row, and a screen
     reader says the name, not every cell. A click elsewhere on the row is answered by the row
     (`onClick` on the `<tr>`), skipping a click the link already answered (or the page would be
     two steps of the back button), a click with a modifier, a click on a control, and a click
     that ends a selection of text in the row. Not a link stretched over the row with a positioned `::after`:
     a `<tr>` is not a positioning box in every browser, and where it is not, the stretched link
     of the last row would cover the whole page. Ctrl and middle click open a new tab from the
     name only. The row under the pointer, and the row whose name has the keyboard's focus, is
     shaded.
   - **An inactive row stays where it is, greyed, with «غیرفعال» beside its name** (ADR 0022), in
     the name's cell rather than in a column of its own, which would stand empty for every
     active row.
   - **No sorting by a heading.** Each list has the order the workshop works in (models newest
     first, people in Persian alphabetical order) and a search box that finds a row; a heading
     that sorts is one more control, and twenty rows a page sorted on the screen would only sort
     that page.
2. **`PagedTable` is a searched list's page in a `DataTable`**: what it says while loading, when
   it did not load and when it is empty, the last page's rows dimmed and marked `aria-busy` while
   the next loads (the busy mark found by the review), a page
   past the last one replaced by the last, and the `Pager` under it. The three lists moved onto
   it; each now gives only its columns, its address and its empty sentence.
3. **A model's bags stay a list.** Each of its rows is edited where it stands (a switch, and the
   refusal of that row under it), and it is not paged. The table is for a list whose rows lead to
   a record's page.
4. **The totals row waits for the first screen that adds up a column** (2.9 or 2.10). No screen
   of Phase 1 adds anything up, and a part nothing uses is seen by no picture and no test: the
   reason the searchable picker waits for 2.1 (ADR 0030). It will be a `<tfoot>`: the table's
   columns already say where each cell goes.
5. **Printing waits for 2.9, its first use**, and the owner is asked now (O.4) which printer the
   office will have and which paper: A4, A5 or an 80 mm roll. Why: the slip's layout depends
   wholly on the paper, and 2.0's prototypes have not yet shown what goes on it; a layout made
   now would be a guess. 2.9 builds the print stylesheet with the slip.
6. **The sample API pages its lists** twenty at a time, as the API does, and the models list has
   26 models, so the pager is in the models' pictures for the first time and the header can be
   seen staying put (ADR 0028).

## Consequences

- A new list with columns is a `DataTable` (a `PagedTable` when it is searched and paged), and
  gets the phone's cards, the header, the row's click, the inactive mark and the empty sentence
  with no code of its own.
- The phone pictures of the three lists did not change by one pixel when they moved onto the
  table, which shows the cards are the rows they were. Their desktop pictures changed on purpose.
- The behaviour tests (`screens/table.spec.ts`) hold the proof: the header at the top of the
  window after a scroll; one Tab per row, the focused row shaded, the pager by keyboard and Enter
  on a name; a click beside the name opening the model as one step of the back button, the name's
  own click also one step, a selection of text opening nothing; on a phone, no table, and the card
  one link. With each behaviour switched off, its test fails.
- **Not done here:**
  - The totals row (2.9 or 2.10) and printing (2.9), as above.
  - Safari and an iPhone were not tried by hand; the table shows from 640 px, so an iPhone held
    upright shows the cards.
