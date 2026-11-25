# ADR 0025 — The contractor and worker screens

- Status: accepted
- Date: 2026-11-25 (1405/09/04)
- Written in: roadmap task 1.6.4 (the fourth part of 1.6)

## Context

The contractor and worker APIs of 1.3 and 1.4 (ADR 0019, ADR 0020) already answer everything the
screens need: search by part of the name or of the mobile, an active filter, pages, add, save the
profile, deactivate and activate, and the IBAN requests with the whole number for the owner only.
Task 1.6.4 builds their panel screens. The API changes in one place only, a security header.

The developer accepted the plan on 1405/09/04 with one change: the in-house workers are called
**«کارگران ثابت»**, not «کارگران», because «کارگران» means every worker, contractors included
(«کارگران کله کارگر هاست»). The proposals he did not change were accepted as written.

## Decision

### Two lists, two words of the workshop

- **«کنتراتی‌ها» and «کارگران ثابت»**, two items in the sidebar after the models. «کنتراتی» is the
  workshop's word, already used on the rate card. Two lists and not one page with tabs, because
  from Phase 2 a contractor's page gets his work and account, and from Phase 4 a worker's page
  attendance and pay; the two never become alike.
- Pages: `/contractors`, `/contractors/new`, `/contractors/:id`, and the same under `/workers`. One
  set of components serves both lists with the kind as a parameter; only a worker has a start date,
  only a contractor must keep a stage.

### The list

- One search box (part of the name or of the mobile, Persian digits too), twenty per page, in
  Persian alphabetical order. The search, the page and the switch are in the address, as for the
  models, so a reload or the back button keeps them.
- **Only the active people, unless the switch «غیرفعال‌ها هم دیده شوند» is on**; then the inactive
  ones show greyed in their place. Why a switch and not three buttons (active, inactive, all):
  someone who left stays in the list for good, so the list would fill with them over the years,
  and the only reason to see them is to find someone who came back. (The models list keeps its
  inactive rows with no filter, as decided in 1.6.2.)
- Each row: the name, under it the stages, and the mobile in Persian digits, left to right
  («۰۹۱۲ ۱۲۳ ۴۵۶۷»). A tap opens the person's page.
- The search box and the pager were taken out of the models page into shared parts, so the three
  lists cannot drift apart. Two gaps of the models list, found here, are closed for all three: the
  search box now follows the address (the sidebar link to the same list used to clear the address
  while the old text stayed in the box and was sent again), and a page past the last one (an old
  link, or the list shrank) shows the last page instead of «nobody here».

### Adding a person

- **One form and one save**, as for a new model: name, mobile, stages, for a worker the start date,
  the optional IBAN and the optional notes. The person's page opens after saving.
- **A worker's start date starts empty and must be picked.** Why not today: most workers entered
  first are the twenty already working, all from the past, and a today left in by mistake would
  count the length of service and the pay from the wrong day. The date field gained an optional
  empty state for this (each list starts on «روز», «ماه», «سال»); the rate form is unchanged. The
  years run from forty years back (the plan said thirty; forty costs nothing and fits a worker of
  long service) to this year, newest first; a future day is refused by the API, with its message
  under the field.
- The form checks what it can before sending (a missing name, mobile, stage or date, an IBAN with
  fewer than 24 digits) and puts every refusal of the API under the field it is about, a taken
  mobile under the mobile.

### The person's page

- **No save button**, as on a model's page (decided 1405/09/03): the name, the mobile and the notes
  are edited where they stand; each stage tick is saved at once; a switch turns the person on and
  off. **Only a worker's start date opens a small form** with «ثبت» and «انصراف», because the three
  lists saved one by one would store half a date (the reason of the rate form, ADR 0024).
- **A contractor's last stage cannot be unticked** on his page, because every tick is saved at once
  and a contractor does at least one stage (ADR 0019); the hint says «دست‌کم یکی». The new-contractor
  form checks it on saving instead, so a wrong first tick can be taken back. A stage ticked before
  it was deactivated stays ticked, marked «غیرفعال» (ADR 0022).
- **The page's changes are sent one after another**, each built from the answer of the last
  (`useSerialWrites` in `shared/api`). Why: leaving the notes by tapping a stage starts two saves
  in one gesture, and the profile request carries every field; two at once would each send the
  other's field as it was, the page would show the later answer without the earlier change, and
  the next save would write that back to the database. The models page had the same gap
  (description and category), seen in the browser and in the audit, and now uses the same queue
  for every change it sends.
- **A taken mobile** says that the number belongs to someone else in the same list and to search
  for it, because entering the same person twice is the likely mistake and the search by mobile
  already finds him. The API does not say who has it.

### The IBAN

- **Its own card**, showing only the last four digits («•••• ۱۰۰۱»). A tap opens a small form: IR
  stands fixed in front and the 24 digits are typed, Persian digits too, grouped as on a bank card
  («IR۷۱ ۰۵۷۰ ۰۲۹۹ …») and left to right, so they can be read against the card or the paper book; a
  whole IBAN pasted with its IR is read as its digits. The check digits are checked by the API.
- **«پاک کردن شبا» is inside that form only, and only when an IBAN is set**, because it is rare
  and must not sit where a finger lands by chance.
- **Only the owner sees «نمایش کامل»**: the whole number, grouped, with a «کپی» button that copies
  it in Latin digits without spaces, the form a bank's app takes. Why a button: he pays by hand
  until Phase 15, and selecting 26 characters with a finger on a phone is how a digit gets lost.
  The number lives in that card's state only, never in the panel's query cache, and is gone when
  he leaves the page or the IBAN is changed. Nobody else sees the option; the API refuses them
  anyway and logs each read (ADR 0019).

### No browser cache for the panel API

- **Every answer under `/panel` carries `Cache-Control: no-store`**, refusals and errors included
  (set when the response starts, before the error handler). Why: nothing set a header before, so
  a whole IBAN or a person's details could sit in the browser's cache on the owner's phone; the
  panel keeps what it needs in memory and gains nothing from that cache. `/health` and the API
  documents are not under `/panel` and are unchanged.

## Not decided here

- **A stage filter on the contractor list** («who sews»): the first screen that needs it is the
  work order (2.2), where a contractor is picked by stage.
- **Calling a person from his page** (`tel:`): not asked for; one link if it is wanted.
- **Two users editing the same record** (found by the review of 1.6.4): the queue only orders
  the changes of one page. Each save sends the whole profile from that page's copy, and the
  contractor, worker and product rows have no version column, so the last save wins: the owner's
  page opened in the morning can put back a mobile the manager changed at noon, visible only in
  the audit. Whether that is accepted for three office users, or every edit carries the row's
  version and a stale one is refused (like `catalog.size_order_outdated`), is open question
  Q.13.
- **A test for the page key** (found by the second review): a person's page and a model's page
  are keyed by the record's id, so the owner's whole IBAN, an open form or a half-typed field
  never stays on the page when it goes straight to another record (seen in Chrome). No test
  guards that key, because the panel has no component tests: adding them means a new package
  (a DOM for Vitest and a component testing library, ADR 0014's package rule), which the
  developer decides.

## Consequences

- Phase 2 adds a contractor's work orders and account to his page, under these cards; Phase 4 and 5
  add a worker's attendance and pay terms to the worker's page.
- Every later list (customers, suppliers, users in 1.6.5) uses the shared search box, pager and
  address of the list.
- Every later IBAN (the payees of Phase 15) uses the same field and the owner-only display.
