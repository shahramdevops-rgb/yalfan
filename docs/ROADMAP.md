# Yalfan — Roadmap

Status: **planned** on 1405/08/24 (2026-11-15). No code exists yet. Read `docs/ARCHITECTURE.md`
first.

How to use this file:
- Each task (for example `2.3`) is sized for one working session.
- A task is finished only when its "Done when" check passes and its tests are green.
- Before each phase starts, its open questions (listed under the phase) are answered by the
  developer and the workshop owner. A phase does not start on guesses.
- From Phase 1 on, every phase ends with its panel screens, so each feature is usable in the
  Persian panel as soon as its API is done.
- Nothing is pushed or released without the developer asking for it.

## Milestones

| Milestone | Phases | What the workshop gets |
|---|---|---|
| **Go-live 1** | 0–3 | Contractor work orders and accounts leave paper. The biggest pain first |
| **Go-live 2** | 4–5 | Live attendance board and payroll for in-house workers |
| **Go-live 3** | 6–9 | Stock, cash and cheques, sales entered in the panel, real profit per model |
| **Go-live 4** | 10 | Contractors see their own account on their phone |
| **Store** | 11–13 | Three-language catalog, retail orders, wholesale and custom orders |
| **Complete** | 14 | Hardening, performance, security review |

Order rule: management before selling. The store is the easy part and is built last; its
infrastructure needs (translations, currency, one catalog) are laid down from Phase 0 and 1 so it
does not force a rewrite.

## The owner's track (no code, can start today)

These are not developer tasks, but phases wait for them.

| # | Task | Needed by |
|---|---|---|
| O.1 | Register `yalfan.ir` (IRNIC handle in the owner's name) and `yalfan.com`. Both are free today and anyone can take them | Now |
| O.2 | Write down the real production stages and which contractors do which stage | Phase 1 |
| O.3 | Decide how contractors are paid (per bag, per stage, mixed) and list today's rates | Phase 2 |
| O.4 | Buy a simple laptop or PC for the office operator; hire or name the operator | Phase 3 |
| O.5 | Collect from the paper books: each contractor's balance, work still out with contractors | Phase 3 |
| O.6 | Buy a server (ParsPack or another provider) once the developer names the size | Phase 3 |
| O.7 | Buy the attendance device **only after** task 4.0 names the model | Phase 4 |
| O.8 | Decide shift hours, allowed lateness, overtime and leave rules | Phase 4 |
| O.9 | Decide how each in-house worker is paid (fixed, piecework, mixed) | Phase 5 |
| O.10 | List materials and count the stock on hand; write the material recipe of each model | Phase 6 |
| O.11 | An SMS provider account and sender line | Phase 10 |
| O.12 | Photograph the products properly | Phase 11 |
| O.13 | Apply for eNamad and a payment gateway in the owner's name (slow; start early in Phase 11) | Phase 12 |
| O.14 | Choose shipping: post or courier for retail, freight for wholesale, and who pays | Phase 12 |

---

## Phase 0 — Foundation

Goal: an empty but real system: solution, database, login, an empty Persian panel, tests, CI.

- **0.1 Solution skeleton.** `Yalfan.Domain/Application/Infrastructure/Api`, central package
  versions, `global.json`, ADR 0001. *Done when:* the solution builds and an empty test project runs.
- **0.2 Database and Docker.** PostgreSQL in Docker Compose, `AppDbContext`, snake_case naming,
  first migration, health check. *Done when:* `docker compose up` gives a healthy API with a database.
- **0.3 Common building blocks.** `Entity`, `Result`, `Error` codes, `Money` (amount + currency),
  `Quantity` (amount + unit), `IClock`. ADRs for money unit and date storage. *Done when:* unit
  tests cover Money and Quantity arithmetic and rounding.
- **0.4 Request pipeline.** Use-case handlers, FluentValidation filter, `Result` → ProblemDetails
  with error codes, Serilog. *Done when:* a sample endpoint returns a coded validation error.
- **0.5 Staff auth.** Identity, JWT with refresh, roles Owner / Manager / Operator, password policy
  and lockout, seeded Owner. *Done when:* integration tests cover login, refresh, lockout, role denial.
- **0.6 Audit.** Interceptor that records user, time, old and new values for every change.
  *Done when:* changing a record in a test produces a readable audit entry.
- **0.7 Integration test base.** Real PostgreSQL per test run, authenticated test clients per role.
  *Done when:* the auth tests run on it in CI.
- **0.8 Panel shell.** `web/panel`: React, Vite, Tailwind, RTL, Persian font, login, sidebar,
  typed API client generated from OpenAPI, Jalali dates, money formatting. *Done when:* the Owner
  logs in and sees an empty dashboard.
- **0.9 CI.** Build, tests, lint and format checks on every push. *Done when:* a failing test
  fails the pipeline.

## Phase 1 — Master data

Goal: everything the later phases point at exists and can be edited by the operator.

Open questions: O.2; how many models and colours; do colours change the material recipe.

- **1.1 Catalog core.** Product (model code), Variant (colour → SKU), Category,
  `ProductTranslation` with locale (Persian filled, others empty). *Done when:* a product with
  three colours can be created and read back through the API.
- **1.2 Stages and routes.** Stage list and the ordered route of stages for each product.
  *Done when:* a product's route can be defined and reordered.
- **1.3 Contractors.** Profile, phone, which stages they do, active flag. *Done when:* CRUD plus
  search by name and phone work, with audit.
- **1.4 Workers.** In-house worker profile, job, start date, active flag. *Done when:* same as 1.3.
- **1.5 Rate cards.** Rate per product × stage, with a start date; history kept. *Done when:* the
  rate valid on a given date can be queried and old rates are never overwritten.
- **1.6 Panel screens.** Products, stages, contractors, workers, rates. *Done when:* the operator
  can enter a real model with its route and rates without help.

## Phase 2 — Contractor work and accounts

Goal: the paper notebook for contractors is replaced. This is the heart of the system.

Open questions: O.3; is material handed over tracked from the start or only in Phase 6; are
rejected pieces deducted or only recorded; who approves QC.

- **2.1 Production orders.** Model, colours and quantities, due date, status. *Done when:* an order
  can be opened, changed while unstarted, and closed.
- **2.2 Work orders: issue.** A stage of a production order given to a contractor: quantity, date,
  expected return date, rate snapshot. ADR on snapshots. *Done when:* changing the rate card
  afterwards does not change an issued work order.
- **2.3 Work orders: delivery and QC.** One or many deliveries; accepted, rework, rejected
  quantities. *Done when:* totals can never exceed the issued quantity, proven by tests.
- **2.4 Ledger core.** Append-only entries per party, calculated balance, reversal instead of
  delete. ADR on money. *Done when:* a reversed entry leaves the balance as before and both
  entries stay visible.
- **2.5 Pay lines.** Accepted quantity × snapshot rate posts to the contractor's ledger; rework
  and rejects handled by the decided rule. *Done when:* the sample from the brief (100 issued, 92
  accepted) gives the expected payable.
- **2.6 Advances and payments.** Advance, payment, deduction; method (cash, transfer, cheque as
  plain text for now). *Done when:* balance = earned − advances − payments in every test case.
- **2.7 Settlement.** Close a set of work orders for one contractor into a settlement with a
  number and a remaining balance. *Done when:* a settled work order cannot be changed, only reversed.
- **2.8 Production progress.** Per production order: ordered, at each stage, accepted, finished.
  *Done when:* the "300 ordered, 220 sewn, 180 ready" view comes from one query.
- **2.9 Panel screens: work orders.** Issue, receive, QC in as few clicks as possible; printable
  work order slip. *Done when:* the operator issues and receives a work order in under a minute.
- **2.10 Panel screens: contractor account.** Statement, balance, payments, settlement, printable
  receipt. *Done when:* a contractor's full statement prints on one page.
- **2.11 Contractor reports.** Owed per contractor, work not returned and overdue, reject and
  rework rate. *Done when:* each report matches hand-calculated test data.

## Phase 3 — First release

Goal: the workshop really uses Phases 1–2.

Open questions: O.4, O.5, O.6; server in Iran, size and provider; panel address
(`panel.yalfan.ir`).

- **3.1 Production images.** Dockerfiles, production compose, reverse proxy with HTTPS, secrets
  outside the repository. ADR on deployment. *Done when:* the stack runs from images on a clean machine.
- **3.2 Server setup.** Hardening, firewall, non-root deploy user, written runbook.
  *Done when:* a second person could repeat the setup from the document.
- **3.3 Backups.** Nightly dump, copy off the server, a tested restore. *Done when:* a restore
  into an empty database is done and timed.
- **3.4 Opening balances.** Screen and import for each contractor's balance and work still out,
  dated on go-live day. *Done when:* system balances equal the paper books on that day.
- **3.5 Go-live.** Real models, contractors and rates entered; operator trained; a short Persian
  how-to. *Done when:* one full week of real work orders is in the system and paper is kept only
  as a safety copy.
- **3.6 First-week fixes.** Whatever the operator trips over. *Done when:* the operator's list is
  empty.

## Phase 4 — Attendance, live

Goal: the owner sees on his phone who is in the workshop right now and who came late.

Open questions: O.7, O.8; fingerprint or face; one door or more; a wall screen or phone only.

- **4.0 Device spike.** Pick candidate devices available in Iran, test the push protocol against
  a throwaway endpoint, write ADR 0007 with the chosen model and the fallback. **No device is
  bought before this.** *Done when:* a real punch from a real or borrowed device reaches a local
  endpoint, or the fallback agent is chosen with reasons.
- **4.1 Device ingest.** Device registration and secret, `/device/*` endpoint, raw `Punch` stored
  idempotently, device heartbeat. *Done when:* replaying the same punches creates no duplicates.
- **4.2 Shifts and rules.** Shift hours, grace period, working days, holidays. *Done when:* the
  rules can express the workshop's real week.
- **4.3 Attendance day.** In, out, late, early leave, overtime, absent, calculated from punches
  and rules; recalculated when a late punch arrives. *Done when:* tests cover missing out-punch,
  double punch, punch after midnight, offline batch arriving late.
- **4.4 Corrections and leave.** Manual correction on top of raw punches, leave requests, all
  audited. *Done when:* a correction never changes a raw punch.
- **4.5 Live board.** SignalR hub and panel page: who is in, who is late, count inside, last time
  the device was heard. *Done when:* a punch changes the board on another screen within seconds.
- **4.6 Attendance reports.** Daily and monthly per worker, lateness ranking. *Done when:* the
  monthly sheet matches hand-calculated test data.
- **4.7 Install on site.** Device on the wall, workers enrolled, a week of side-by-side checking.
  *Done when:* a week of device data matches what really happened.

## Phase 5 — Payroll for in-house workers

Goal: month-end pay for the 20 workers comes out of the system.

Open questions: O.9; insurance and legal deductions in or out; pay period (monthly or weekly).

- **5.1 Pay setup.** Per worker: fixed salary, hourly, piecework or mixed; overtime and lateness
  factors, with history. *Done when:* the terms valid on any date can be queried.
- **5.2 In-house work records.** Work orders for in-house workers or teams using the Phase 2
  model. *Done when:* an in-house stage is recorded with the same screens as a contractor's.
- **5.3 Advances.** Advance against pay, posted to the worker's ledger. *Done when:* advances
  reduce the next payslip.
- **5.4 Payslip calculation.** Period, attendance, piecework, overtime, deductions, advances.
  *Done when:* five hand-calculated workers match to the last Toman.
- **5.5 Pay and close.** Approve, pay, post to the ledger, lock the period. *Done when:* a locked
  period can only be corrected in the next one.
- **5.6 Panel screens.** Payroll run, payslip print, worker statement. *Done when:* a month's
  payroll is run end to end by the operator.

## Phase 6 — Stock and materials

Goal: fabric, zips and fittings are followed from purchase to finished bag.

Open questions: O.10; units (metre, piece, roll); do contractors receive raw material or cut
pieces; how waste is judged.

- **6.1 Materials and units.** Material, unit, category, minimum level. *Done when:* CRUD and
  search work.
- **6.2 Stock ledger.** `StockMovement`, warehouses, calculated on-hand, reversal only. ADR 0004.
  *Done when:* stock can never go negative without an explicit, audited override.
- **6.3 Suppliers and purchases.** Purchase receipt raises stock and posts the debt to the
  supplier's ledger with a cost snapshot. *Done when:* one purchase moves stock and money together
  or not at all.
- **6.4 Recipes (BOM).** Materials per product and per colour. *Done when:* the material need of
  a production order is calculated.
- **6.5 Issue to contractors.** A contractor is a warehouse; material handed over with a work
  order, returned, or written off as waste. *Done when:* each contractor's material in hand and
  shortfall are reported.
- **6.6 Finished goods.** Accepted final-stage pieces enter finished stock per SKU. *Done when:*
  finished stock equals accepted minus sold in tests.
- **6.7 Stock count.** Count sheet, differences posted as adjustments. *Done when:* a count
  corrects on-hand and leaves an audit trail.
- **6.8 Panel screens and reports.** Stock on hand, low stock, material flow, waste per
  contractor. *Done when:* the opening stock (O.10) is entered and matches the count.

## Phase 7 — Cash, cheques, expenses

Goal: every Toman in or out sits in a named account.

- **7.1 Cash and bank accounts.** Accounts, transfers between them; every payment names its
  account. *Done when:* account balances are calculated from entries.
- **7.2 Cheques.** Received and issued, due date, status changes, bounced cheque reopens the debt.
  *Done when:* every status change posts the right ledger entries, proven by tests.
- **7.3 Expenses.** Categories, recurring expenses (rent, power). *Done when:* monthly expense
  totals per category are correct.
- **7.4 Reminders.** Daily job: cheques due soon, debts overdue. *Done when:* the list for a
  given day is right.
- **7.5 Panel screens.** Accounts, cheque book view by due date, expenses. *Done when:* the
  operator records a received cheque and its clearing.

## Phase 8 — Sales in the panel

Goal: in-person and phone sales, which are most sales today, are recorded, so profit is real.

Open questions: wholesale price tiers; credit limits; return policy.

- **8.1 Customers and price lists.** Retail and wholesale customers, price lists with tiers,
  minimum quantities and currency. *Done when:* the right price is found for a customer and quantity.
- **8.2 Sales orders.** One order model with a channel; price snapshot; reserves finished stock.
  ADR 0009. *Done when:* an order cannot sell stock that is not there without an override.
- **8.3 Proforma and invoice.** Numbered, printable, Persian. *Done when:* a proforma becomes an
  order without retyping.
- **8.4 Delivery and customer ledger.** Delivery reduces stock; the customer's debt, payments and
  cheques sit in the ledger. *Done when:* a customer statement is correct after partial payment.
- **8.5 Returns.** Returned goods come back into stock or scrap; the ledger is corrected.
  *Done when:* a return reverses money and stock together.
- **8.6 Order to production.** A sales order can raise a production order and stay linked.
  *Done when:* the order shows its production progress.
- **8.7 Panel screens.** Quick sale, order list, customer account. *Done when:* a walk-in sale is
  entered in under a minute.

## Phase 9 — Dashboard and reports

Goal: the owner opens one page and knows the state of the business.

- **9.1 Cost per bag.** Material cost + stage wages + a share of overhead per model. *Done when:*
  three models match hand-calculated costs.
- **9.2 Profit per model and per order.** *Done when:* totals reconcile with the ledger.
- **9.3 Owner dashboard.** Sales this month, production cost, work in progress, owed to
  contractors, owed by customers, cheques due, who is in the workshop. *Done when:* every number
  links to the report behind it.
- **9.4 Operational reports.** Late orders, sales per customer, worker and contractor performance
  (quantity, quality, on-time). *Done when:* each matches test data.
- **9.5 Export.** Excel and print for the main reports. *Done when:* exports open correctly with
  Persian text.

## Phase 10 — Contractor panel and SMS

Goal: a contractor checks his own account on his phone instead of calling the office.

Open questions: O.11; which events send an SMS; who pays for SMS volume.

- **10.1 SMS sending.** Provider adapter, templates, queue, delivery status. *Done when:* a test
  message is delivered and its status recorded.
- **10.2 OTP login.** Phone + code for contractors, rate limits, lock on abuse. *Done when:*
  brute-force and resend-flood tests fail safely.
- **10.3 Contractor API.** `/contractor/*`: own work orders, deliveries, balance, payments.
  *Done when:* tests prove one contractor can never read another's data.
- **10.4 Contractor PWA.** `web/contractor`: three or four large, simple screens, installable.
  *Done when:* a contractor with no training finds his balance.
- **10.5 Notices.** SMS on work accepted, payment made, work overdue. Each kind can be turned off.
  *Done when:* each event sends exactly one message.

## Phase 11 — Store: three-language catalog

Goal: the brand is visible again on the web, in three languages, with real products.

Open questions: O.12; third language confirmed as English; where the store is served from for
foreign visitors (ADR 0008); show prices to everyone or wholesale prices only after login.

- **11.1 Store API.** Public catalog endpoints with locale and currency, caching. *Done when:* a
  product is returned in the asked locale with a clean fallback to Persian.
- **11.2 Store shell.** `web/store` on Next.js: locale in the URL, RTL and LTR from one stylesheet,
  message files, `hreflang`, sitemap. *Done when:* the same page renders correctly in fa, ar and en.
- **11.3 Catalog pages.** Home, category, product page with colours, images, stock state.
  *Done when:* pages are server-rendered and pass a basic SEO and accessibility check.
- **11.4 Images.** Upload, resizing, modern formats, alt text per locale. *Done when:* a product
  page loads fast on a mobile connection.
- **11.5 Store admin in the panel.** Which products show, order, photos, translations, prices.
  *Done when:* the operator publishes a product without the developer.
- **11.6 Showcase release.** The catalog goes live on `yalfan.ir` without online payment.
  This is also what eNamad is applied for with (O.13). *Done when:* the site is public and indexed.

## Phase 12 — Store: retail orders

Goal: a retail buyer in Iran orders and pays online.

Open questions: O.13, O.14; return policy text; guest checkout or account only.

- **12.1 Customer accounts.** Phone + code login, addresses, order history. *Done when:* a customer
  sees only his own orders.
- **12.2 Cart and checkout.** Cart, shipping cost, stock reservation, order created with channel
  "store retail". *Done when:* two buyers cannot buy the last piece.
- **12.3 Payment gateway.** Iranian gateway adapter, verification, failed and abandoned payments
  release stock. *Done when:* the gateway's sandbox covers success, failure and double callback.
- **12.4 Fulfilment.** Packing list, shipment, tracking code, status SMS. *Done when:* an order
  goes from paid to delivered in the panel.
- **12.5 Foreign visitors.** In ar and en, "pay" is replaced by "request a proforma", which lands
  in the panel as a lead. *Done when:* no payment button is reachable in ar or en.

## Phase 13 — Store: wholesale and custom orders

Goal: shops and foreign buyers order in bulk, and bulk orders drive production.

- **13.1 Wholesale accounts.** Application, approval in the panel, price tier. *Done when:* an
  approved buyer sees wholesale prices and minimum quantities.
- **13.2 Wholesale ordering.** Quantity grid per colour, proforma, order on approval.
  *Done when:* a 500-piece order is placed in a few steps.
- **13.3 Wholesale panel.** Own orders, production status, invoices, balance. *Done when:* a buyer
  sees how far his order is.
- **13.4 Custom production requests.** Colours, quantity, own logo, files; quote from the panel.
  *Done when:* an accepted quote becomes a sales order and a production order.
- **13.5 Translation pass.** Real Arabic and English content for the products that sell abroad.
  *Done when:* the chosen products are complete in all three languages.

## Phase 14 — Hardening

- **14.1 Security review.** Auth, permissions, uploads, rate limits, dependency audit.
  *Done when:* the review's findings are fixed or accepted in writing.
- **14.2 Performance.** Realistic data volume, slow-query review, indexes, page weight on mobile
  internet. *Done when:* the main screens stay fast with three years of data.
- **14.3 Monitoring.** Uptime check, error alerts, disk and backup alerts. *Done when:* a failed
  backup sends an alert.
- **14.4 Documentation.** Deployment runbook, business rules, operator guide in Persian.
  *Done when:* the documents match the running system.

## Later ideas (not planned)

- An LLM assistant over the reports and the catalog (the architecture leaves room, section 11).
- QR labels on work-order batches, scanned on return.
- A wall screen in the workshop for the live board.
- A panel view for workers to see their own attendance and payslip.

## Open questions, all in one place

| # | Question | Blocks |
|---|---|---|
| Q.1 | Real stages of making a bag, and which are done outside | Phase 1 |
| Q.2 | Contractor pay method and rates; what happens to rejects | Phase 2 |
| Q.3 | Stored money unit: Toman or Rial | 0.3 |
| Q.4 | Do contractors receive raw material or cut pieces | Phase 2 / 6 |
| Q.5 | Shift hours, grace period, overtime, leave rules | Phase 4 |
| Q.6 | In-house pay types; legal deductions in scope or not | Phase 5 |
| Q.7 | Wholesale price tiers, credit and return policy | Phase 8 |
| Q.8 | Third language is English (assumed) | Phase 11 |
| Q.9 | Where the store is served from for foreign visitors | Phase 11 |
| Q.10 | Number of models and colours today | Phase 1 |
