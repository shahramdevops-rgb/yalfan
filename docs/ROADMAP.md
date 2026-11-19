# Yalfan — Roadmap

Status: **Phase 0 in progress.** Planned on 1405/08/24 (2026-11-15); tasks 0.1 to 0.3 done the
same day, 0.4 and 0.5 on 1405/08/27, 0.6 to 0.8 on 1405/08/28. Next task: 0.9. Read
`docs/ARCHITECTURE.md` first.

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
| **Go-live 4** | 10 | Workers and contractors see their own account on their phone |
| **Store** | 11–13 | Three-language catalog, retail orders, wholesale and custom orders |
| **Complete** | 14 | Hardening, performance, security review |
| **Payouts** | 15 | Workers and contractors are paid through the bank with one approval, no hand transfers |

Order rule: management before selling. The store is the easy part and is built last; its
infrastructure needs (translations, currency, one catalog) are laid down from Phase 0 and 1 so it
does not force a rewrite.

## The owner's track (no code, can start today)

Decided 1405/08/24: **the developer does all of these himself**, the same way as for the gym
project, because the owner is not technical (he does not know what IRNIC is). The owner only
supplies what is needed: identity documents, decisions, information from the paper books, money
for purchases. Ownership: the **domains and the SMS panel are in the owner's name**, because they
are the brand's assets and must stay his; the **server is in the developer's name**, because the
developer runs and maintains it. Phases wait for these.

| # | Task | Needed by |
|---|---|---|
| O.1 | Register `yalfan.ir` (IRNIC handle in the owner's name) and `yalfan.com`. Both are free today and anyone can take them | Now |
| O.2 | Write down the real production stages and which contractors do which stage | Phase 1 |
| O.3 | Decide how contractors are paid (per bag, per stage, mixed) and list today's rates | Phase 2 |
| O.4 | Buy a simple laptop or PC for the office operator; hire or name the operator | Phase 3 |
| O.5 | Collect from the paper books: each contractor's balance, work still out with contractors | Phase 3 |
| O.6 | Buy a server (ParsPack or another provider) in the developer's name: Iranian VPS, 2 cores, 4 GB, ~80 GB SSD, upgradable to 4 cores and 8 GB for Phase 11. Criteria in docs/ARCHITECTURE.md 13.1; confirmed in task 3.1 | Phase 3 |
| O.7 | Buy the attendance device **only after** task 4.0 names the model | Phase 4 |
| O.8 | Decide shift hours, allowed lateness, overtime and leave rules | Phase 4 |
| O.9 | Decide how each in-house worker is paid (fixed, piecework, mixed) | Phase 5 |
| O.9b | Open a separate bank account used only for payouts; documents for a bank or provider contract if 15.0 says go | Phase 15 |
| O.10 | List materials and count the stock on hand; write the material recipe of each model | Phase 6 |
| O.11 | An SMS provider account and a **service line** (خط خدماتی) with a login-code template, in the owner's name. Moved earlier on 1405/08/27: staff log in with SMS codes (ADR 0012), so the panel cannot go live without it. The developer gets the service line early instead of waiting for the store | Phase 3 (task 3.0) |
| O.12 | Photograph the products properly | Phase 11 |
| O.13 | Apply for eNamad and a payment gateway in the owner's name (slow; start early in Phase 11) | Phase 12 |
| O.14 | Choose shipping: post or courier for retail, freight for wholesale, and who pays | Phase 12 |

---

## Phase 0 — Foundation

Goal: an empty but real system: solution, database, login, an empty Persian panel, tests, CI.

- **0.1 Solution skeleton.** `Yalfan.Domain/Application/Infrastructure/Api`, central package
  versions, `global.json`, ADR 0001. *Done when:* the solution builds and an empty test project runs.
  **Done 1405/08/24:** build has 0 warnings, 1 test passes. `Yalfan.Api.IntegrationTests` was
  left to 0.7 because it needed a database and auth to test anything; it was created in 0.4
  instead, because the request pipeline can be tested without a database.
- **0.2 Database and Docker.** PostgreSQL in Docker Compose, `AppDbContext`, snake_case naming,
  first migration, health check. *Done when:* `docker compose up` gives a healthy API with a database.
  **Done 1405/08/24:** both containers report healthy; `/health` gives 200, and 503 while the
  database is stopped. Checked by hand: automated database tests start in 0.7. `IAppDbContext`
  is added with the first entity (0.5), because an interface with no members protects nothing.
- **0.3 Common building blocks.** `Entity`, `Result`, `Error` codes, `Money` (amount + currency),
  `Quantity` (amount + unit), `IClock`. ADRs for money unit and date storage. *Done when:* unit
  tests cover Money and Quantity arithmetic and rounding.
  **Done 1405/08/24:** 71 unit tests pass; ADR 0002 (Rial everywhere) and ADR 0005. Only the
  unit `piece` is fixed in code; the real units are asked in Phase 6.
- **0.4 Request pipeline.** Use-case handlers, FluentValidation filter, `Result` → ProblemDetails
  with error codes, Serilog. *Done when:* a sample endpoint returns a coded validation error.
  **Done 1405/08/27:** 94 tests pass (23 new), build has 0 warnings; ADR 0011. Validation runs as
  a decorator in Application rather than an endpoint filter, so jobs and later callers cannot
  skip it. The sample use case lives only in the test project. Seq runs in compose on port 5342.
  `Yalfan.Api.IntegrationTests` exists now, without a database; 0.7 adds PostgreSQL to it.
- **0.5 Staff auth.** Identity, JWT with refresh, roles Owner / Manager / Operator, password policy
  and lockout, seeded Owner. *Done when:* integration tests cover login, refresh, lockout, role denial.
  **Done 1405/08/27:** 172 tests pass, build has 0 warnings; ADR 0012. Changed during the task by
  the developer: **no passwords**, staff log in with mobile + SMS code, so "lockout" became the
  code limits (5 wrong tries, 2-minute wait, 5 codes an hour), and ASP.NET Identity was replaced
  by a small `User` entity. Only the owner adds users, and only managers and operators. SMS goes
  to the log until the real provider (3.0). The PostgreSQL test base (Testcontainers) was brought
  forward from 0.7.
- **0.6 Audit.** Interceptor that records user, time, old and new values for every change.
  *Done when:* changing a record in a test produces a readable audit entry.
  **Done 1405/08/28:** 191 tests pass, build has 0 warnings; ADR 0013. The entry is written in
  the same save as the change; a database trigger keeps `audit_entries` append-only; login codes
  and session tokens are excluded; a field can be recorded as "changed" without its values (for
  IBANs in 1.3); `ExecuteUpdate`, `ExecuteDelete` and raw SQL writes are build errors.
- **0.7 Integration test base.** Real PostgreSQL per test run, authenticated test clients per role.
  Also the day-boundary test of `SystemClock` (ADR 0005), which has no test project until now.
  *Done when:* the auth tests run on it in CI.
  Already done in 0.5: one Testcontainers PostgreSQL per run, one database per test class, a fake
  clock and SMS sender, `CreateClientAsAsync(role)`. Left for 0.7: data reset between tests if
  needed, the `SystemClock` test, and running it all in CI.
  **Done 1405/08/28:** 200 tests pass locally (9 new), build has 0 warnings. CI runs build and
  every test on each push (`.github/workflows/ci.yml`, GitHub Actions on Linux); its first run
  is the developer's next push. **No data reset between tests**, decided in this task: tests are
  already kept apart by one database per class and their own rows, and a reset tool would need
  the audit trigger (ADR 0013) switched off, since it refuses `DELETE` and `TRUNCATE` on
  `audit_entries`. The isolation rule is in docs/ARCHITECTURE.md section 12. The `SystemClock`
  test checks the Tehran day boundary in winter, in summer and in a past daylight-saving summer;
  a fixed +03:30 offset was tried and fails it.
- **0.8 Panel shell.** `web/panel`: React, Vite, Tailwind, RTL, Persian font, login, sidebar,
  typed API client generated from OpenAPI, Jalali dates, money formatting. *Done when:* the Owner
  logs in and sees an empty dashboard.
  **Done 1405/08/28:** checked in Chrome against a throwaway database: login with Persian
  digits, the Persian message for a wrong code, the dashboard, the session kept across a
  reload, logout. 204 .NET tests (4 new) and 23 panel tests pass; build has 0 warnings and 0
  type errors; CI has a panel job. ADR 0014: one origin and no CORS, token in memory, the
  committed contract `web/panel/openapi/panel.json` kept equal to the API by a test, numbers
  only as JSON numbers. Not checked by eye: the phone drawer (the browser window could not be
  narrowed). Found on the way: Vite cannot run from a path containing `#`; the developer moves
  the checkout out of `c#project`.
- **0.9 CI.** Build, tests, lint and format checks on every push. *Done when:* a failing test
  fails the pipeline.
  Build and tests on every push exist since 0.7, brought forward because 0.7's own check was
  "runs in CI". Left for 0.9: lint and format checks (.NET and the panel from 0.8), and proving
  that a failing test turns the run red.

## Phase 1 — Master data

Goal: everything the later phases point at exists and can be edited by the operator.

Open questions: O.2; how many models and colours; do colours change the material recipe.

- **1.1 Catalog core.** Product (model code), Variant (colour → SKU), Category,
  `ProductTranslation` with locale (Persian filled, others empty). *Done when:* a product with
  three colours can be created and read back through the API.
- **1.2 Stages and routes.** Stage list and the ordered route of stages for each product.
  *Done when:* a product's route can be defined and reordered.
- **1.3 Contractors.** Profile, phone, which stages they do, active flag, optional IBAN (checksum-validated, stored encrypted, shown masked; used by Phase 15). *Done when:* CRUD plus
  search by name and phone work, with audit.
- **1.4 Workers.** In-house worker profile, job, start date, active flag, optional IBAN as in 1.3. *Done when:* same as 1.3.
- **1.5 Rate cards.** Rate per product × stage, with a start date; history kept. *Done when:* the
  rate valid on a given date can be queried and old rates are never overwritten.
- **1.6 Panel screens.** Products, stages, contractors, workers, rates; and the owner's users
  screen (list, add, deactivate, change a user's mobile when a phone is lost; added 1405/08/27
  with ADR 0012); and, for the owner, the change history of a record from the audit (added
  1405/08/28 with ADR 0013). *Done when:* the operator can enter a real model with its route and rates
  without help.

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
  plain text for now) and an optional bank reference, so that a payment created by a payout
  item in Phase 15 is the same record as one typed by hand. *Done when:* balance = earned − advances − payments in every test case.
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

Open questions: O.4, O.5, O.6, O.11; server in Iran, size and provider; panel address
(`panel.yalfan.ir`).

- **3.0 SMS provider (moved from 10.1 on 1405/08/27).** Adapter for the chosen provider behind
  `ISmsSender`, the login-code template on the service line, delivery status recorded. Staff
  cannot log in without it (ADR 0012). *Done when:* a real login code reaches a real phone and
  the log provider is no longer configured outside Development.
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
  *Done when:* five hand-calculated workers match to the last Rial.
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

Goal: every Rial in or out sits in a named account.

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

## Phase 10 — Worker and contractor app, and SMS

Goal: a worker or a contractor checks his own account on his phone instead of asking the office.

Decided 1405/08/24: one installable PWA for both, because a worker and a contractor need the same
thing (my work, my money) and one app is half the work to build and maintain. Wholesale buyers get
their own panel inside the store (Phase 13), also installable, also phone + SMS code.

Open questions: O.11; which events send an SMS; who pays for SMS volume.

- **10.1 SMS sending.** The adapter itself moved to 3.0. What is left: more templates (work
  accepted, payment made), a queue so a slow provider never blocks a request. *Done when:* a
  notice is queued, delivered and its status recorded.
- **10.2 OTP login.** Phone + code for contractors, rate limits, lock on abuse. *Done when:*
  brute-force and resend-flood tests fail safely. The flow and its limits exist since 0.5
  (ADR 0012); this task adds the contractor role, links a user to a contractor profile and opens
  `/contractor/auth`.
- **10.3 Contractor API.** `/contractor/*`: own work orders, deliveries, balance, payments.
  *Done when:* tests prove one contractor can never read another's data.
- **10.4 Contractor PWA.** `web/contractor`: three or four large, simple screens, installable.
  *Done when:* a contractor with no training finds his balance.
- **10.5 Notices.** SMS on work accepted, payment made, work overdue. Each kind can be turned off.
  *Done when:* each event sends exactly one message.
- **10.6 Worker view.** The same app for in-house workers: today's and this month's attendance,
  lateness and overtime, piecework, advances, payslips and payments, always up to date.
  *Done when:* tests prove one worker can never read another's data.
- **10.7 Requests.** A worker asks for leave or an advance in the app; the manager approves with
  one tap. *Done when:* an approved request lands in Attendance or the ledger without retyping.

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
- **11.6 SEO foundation (asked by the developer 1405/08/24).** Structured product data, titles
  and descriptions per locale, clean URLs, sitemap per language, page-speed budget, Google Search
  Console and analytics set up. *Done when:* every product page passes Google's rich-results
  test and the sitemap is accepted.
- **11.7 Showcase release.** The catalog goes live on `yalfan.ir` without online payment.
  This is also what eNamad is applied for with (O.13). *Done when:* the site is public and indexed.

## Phase 12 — Store: retail orders

Goal: a retail buyer in Iran orders and pays online.

Open questions: O.13, O.14; return policy text; guest checkout or account only; whether the
gateway provider also offers payouts (15.0), because one provider for both is one contract.

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

### Marketing track (ongoing, not a build task)

Asked by the developer on 1405/08/24. Ranking and advertising are continuous work with a monthly
budget, not something that is finished once, so they sit beside the phases:
- **Ongoing SEO:** content, internal links, fixing what Search Console reports. Ranking takes
  months and cannot be promised.
- **Product feed (code, one task when needed):** an export of products and prices for Iranian
  price-comparison and marketplace sites.
- **Advertising:** Iranian ad networks and large sites, marketplaces, Instagram. Google Ads is
  not directly available to an Iranian business. Which channels and what budget is the owner's
  decision.

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

## Phase 15 — Payouts through the bank

Goal: the owner ticks who gets paid, approves once, and the money goes out and is recorded,
with no hand transfers. Design: docs/ARCHITECTURE.md 7.5.

Decided by the developer on 1405/08/24: the target is **API payout**, the design is
**bank-agnostic**, and feasibility is checked afterwards. The tasks are ordered so that nothing
built is wasted whatever 15.0 finds: 15.1 to 15.5 are needed by every outcome, and only 15.6
depends on a provider saying yes.

**When (decided 1405/08/24):** this is the last management phase, built after the panel and the
store are live and their bugs are fixed (Phase 14). There is no hurry. Why last: it is the one
feature that can lose real money, so it goes on top of a system that has already proved itself;
and the likely providers want eNamad and a payment gateway first, which only exist after
Phase 12. Until then payments are made by hand and recorded in the panel (2.6).

**Prepared from the start**, so this phase adds a module and changes nothing underneath:
- 1.3 and 1.4 store each contractor's and worker's IBAN (optional, validated, encrypted).
- 2.6 records every payment with a method and a reference, so a payment made by a payout item
  later is the same kind of record as one typed by the operator.
- 2.7 settlements and 5.5 payslips end in "approved and unpaid" amounts, which is exactly what a
  payout batch is built from.
- 12.3 chooses the store's payment gateway with payouts in mind (same provider if possible).

Open questions: is the workshop a registered company or does the owner work as an individual
(most bank APIs are offered to registered companies); the owner's bank; how often contractors
are paid today and how (cash, card transfer); who besides the owner may approve.

- **15.0 Feasibility (no code; best done together with the gateway choice in Phase 12).** For the owner's bank and for two or three
  licensed payment providers find out, from the provider itself and in writing where possible:
  who may sign up (individual or company only), required documents, whether payouts to third
  parties (wages, contractors) are allowed, from which account the money leaves, the APIs offered
  (group transfer, per-transfer status, balance, IBAN holder inquiry, statement), fees, ceilings,
  settlement timing, a sandbox. The developer's ChatGPT research of 1405/08/24 (named banks,
  an open-banking platform, a payment provider, transfer ceilings) is a list of leads, **not yet
  verified**; each claim is checked here. ADR 0010. *Done when:* a comparison table with real
  terms and a go or no-go for API payout.
  Lead added 1405/08/24 from a Jibit article the developer supplied (a marketing page, not API
  documentation, undated): it describes payout by panel or API, single or in batches, as an
  interbank cycle transfer to any IBAN or as an instant transfer, from a balance topped up at the
  provider, plus inquiry services (IBAN holder, card to IBAN, national ID against mobile number).
  It names brokers, exchanges and online selling platforms as its customers and ties its side
  services to having its payment gateway. So ask them exactly: can a workshop sign up, as an
  individual or only as a company; is a payment gateway (and so eNamad) a precondition; is paying
  wages and contractors an allowed use; how is the balance topped up; fees per transfer for each
  method; ceilings; does the API accept an idempotency key and give per-transfer status and the
  balance; is there a sandbox. If the answer is yes, the store's payment gateway (Phase 12) and
  payouts may be the same provider, which would be one contract instead of two.
- **15.1 Payee bank accounts.** IBAN with checksum validation, encrypted storage, masked display,
  verification state, holding period after a change, audit. *Done when:* an unverified or
  just-changed IBAN cannot be put in a batch.
- **15.2 Payout batch.** Build a batch from approved, unpaid payroll, settlements and advances;
  per-item idempotency key and status; checks (duplicates, ceilings, total against balance);
  owner approval. *Done when:* the same payable can never be in two open batches.
- **15.3 Provider port and file adapter.** `IPayoutProvider` with capability flags, and
  `BankFileProvider` that writes the bank's group-transfer file. *Done when:* a five-person batch
  produces a file the bank's own checker accepts.
- **15.4 Settlement.** Item confirmed settled posts the ledger entry and sends the SMS; failed or
  returned makes the amount payable again; a batch can be partly settled. *Done when:* tests
  cover all-settled, partly failed, returned after settlement.
- **15.5 Panel screens.** Tick payees, see total against balance, approve, watch each item's
  status. *Done when:* the owner pays a test batch end to end from his phone.
- **15.6 First API adapter (only after a go from 15.0).** Submit, status polling job, balance,
  IBAN inquiry if offered. *Done when:* the provider's sandbox passes: success, timeout then
  re-query without double payment, partial failure, duplicate submit refused.
- **15.7 Reconciliation.** Statement by API or file import, matched to items by bank reference;
  differences listed. *Done when:* a statement with one missing and one extra line is caught.
- **15.8 Safety.** Dedicated payout account, one-time code on approval, daily ceiling, alert on
  every submitted batch, a kill switch that stops all payouts. *Done when:* each guard has a test
  that proves it blocks.

## Later ideas (not planned)

- An LLM assistant over the reports and the catalog (the architecture leaves room, section 11).
- QR labels on work-order batches, scanned on return, so receiving needs no typing.
- A photo attached to a QC reject, visible to the contractor in the app.
- The contractor confirms in the app what he was handed, so both sides have the same record.
- A nightly summary to the owner: today's output, who was absent, what is due tomorrow.
- A wall screen in the workshop for the live board.

## Open questions, all in one place

| # | Question | Blocks |
|---|---|---|
| Q.1 | Real stages of making a bag, and which are done outside | Phase 1 |
| Q.2 | Contractor pay method and rates; what happens to rejects | Phase 2 |
| Q.3 | Stored money unit: Toman or Rial. **Answered 1405/08/24: Rial, everywhere** (stored and shown), ADR 0002 | 0.3 |
| Q.4 | Do contractors receive raw material or cut pieces | Phase 2 / 6 |
| Q.5 | Shift hours, grace period, overtime, leave rules | Phase 4 |
| Q.6 | In-house pay types; legal deductions in scope or not | Phase 5 |
| Q.7 | Wholesale price tiers, credit and return policy | Phase 8 |
| Q.8 | Third language is English (assumed) | Phase 11 |
| Q.9 | Where the store is served from for foreign visitors | Phase 11 |
| Q.10 | Number of models and colours today | Phase 1 |
| Q.11 | Registered company or individual; which bank | 12.3 / 15.0 |
| Q.12 | How often and how contractors are paid today | Phase 2 |
