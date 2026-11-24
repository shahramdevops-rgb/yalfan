# Yalfan — Roadmap

Status: **Phase 1 in progress.** Planned on 1405/08/24 (2026-11-15); tasks 0.1 to 0.3 done the
same day, 0.4 and 0.5 on 1405/08/27, 0.6 to 0.8 on 1405/08/28, 0.9 locally on 1405/08/29 (its
red run on GitHub waits for a push, which closes Phase 0), 1.1 and 1.2 on 1405/09/01, 1.3, 1.4
and 1.5 on 1405/09/02, 1.6.1 and 1.6.2 on 1405/09/03. Next: 1.6.3 (rates and the money
display). Read
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
| O.2 | Write down the real production stages and which contractors do which stage. **Stages answered 1405/09/01** (cutting, sewing, packing; ADR 0018). Which contractor does which stage is ticked per contractor by the operator (1.3, ADR 0019); the owner gives the list when the contractors are entered | Phase 1 |
| O.3 | Decide how contractors are paid (per bag, per stage, mixed) and list today's rates. **How, answered 1405/09/02:** by the piece (کنترات), per model and stage (ADR 0021). Today's rates are still to be listed; the operator enters them in the 1.6 screens | Phase 2 |
| O.4 | Buy a simple laptop or PC for the office operator; hire or name the operator | Phase 3 |
| O.5 | Collect from the paper books: each contractor's balance, work still out with contractors | Phase 3 |
| O.6 | Buy a server (ParsPack or another provider) in the developer's name: Iranian VPS, 2 cores, 4 GB, ~80 GB SSD, upgradable to 4 cores and 8 GB for Phase 11. Criteria in docs/ARCHITECTURE.md 13.1; confirmed in task 3.1 | Phase 3 |
| O.7 | Buy the attendance device **only after** task 4.0 names the model | Phase 4 |
| O.8 | Decide shift hours, allowed lateness, overtime and leave rules | Phase 4 |
| O.9 | Decide how each in-house worker is paid (fixed, piecework, mixed). **Answered 1405/09/02: a day wage** (روزمزد) | Phase 5 |
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
  **Done locally 1405/08/29; the red run on GitHub waits for the developer's push.** ADR 0015.
  204 .NET tests and 26 panel tests (3 new) pass; build has 0 warnings; every check exits
  non-zero on a deliberate error. The .NET lint is the build itself (`AnalysisMode` Recommended,
  code style enforced, warnings are errors); `dotnet format whitespace`, ESLint (type-checked,
  `--max-warnings 0`) and Prettier are CI steps, and every check runs even after one fails. Two
  ESLint rules enforce logical Tailwind utilities and no Persian text in components. LF line
  endings everywhere, because Windows and the Linux runner disagreed. Not adopted: the
  unused-using rule in the build, because it puts XML comments into the panel's contract.
  Found on the way: queries threw a plain object (now `ProblemError`), the drawer closed through
  a `setState` in an effect, and the Docker image needs `.editorconfig` copied in (it failed on
  CA1716 without it; the image builds now). Proof on GitHub: push `main` (the first CI run ever, expected green),
  then the throwaway branch `ci/prove-red`; expected red at .NET Format and Test and panel Lint,
  Format and Test, with both Builds green; then delete the branch.

## Phase 1 — Master data

Goal: everything the later phases point at exists and can be edited by the operator.

Open questions: O.2; how many models and colours; do colours change the material recipe.

Answered by the developer on 1405/09/01, for 1.1 (the stages, O.2 and Q.1, are answered further
down, for 1.2):
- **No codes today.** The workshop has no model codes and no SKUs, so the system numbers each
  model itself (a short sequential number shown next to the name), because a number is short to
  write on a work order and to say on the phone, and similar names cannot be mixed up.
- **Many models** (Q.10, no exact count), so product lists are searched and paged, not one list.
- **A bag is model + fabric + colour + optional size.** One model is sewn in several fabrics;
  colours are one shared list, any colour with any fabric. So a variant is fabric × colour ×
  size. Why it is decided now: work orders and stock point at variants, so changing what a
  variant is later would mean moving every one of those rows.
- **Sizes belong to each model, not to a shared list.** A model either has sizes or not. When it
  has, the operator ticks "has sizes", says how many, and types each size's name; every variant
  of that model then names one of them. A model without sizes has none, and its variants none.
- **No translation in the panel.** Everything on the panel is Persian; the operator never sees a
  language. Catalog text is still stored in translation rows with only Persian filled, because
  the store shows it in three languages and moving single Persian columns into rows later would
  touch every product. Where Arabic and English are entered is asked in Phase 11.
- **Accepted proposals (1405/09/01):** the SKU is model number + variant number (`101-1`),
  because it is short, made by the system and needs no codes invented for fabrics and colours;
  model numbers start at 101, because a one-digit number on a work order reads like a quantity;
  no two models, categories, fabrics or colours share a Persian name, because entering the same
  model twice is the likely mistake with many models; categories are one level, at most one per
  model and optional for now, because the store is the first to need them and a parent level is
  one column later; models and variants are never deleted, only deactivated, because work
  orders and stock will point at them.

Answered by the developer on 1405/09/01, for 1.2 (O.2 and Q.1, ADR 0018):
- **Three stages: cutting, sewing, packing**, more added when needed. Cutting is done in the
  workshop, never by a contractor; sewing by contractors (per piece) or by in-house workers on a
  day wage; anything but cutting may be given out, including part of a job ("only the handles").
- **No route per model**, the developer's call: the steps differ from model to model and could
  become many, so an ordered route for each of many models would make the system complicated.
  "Build the base, add the details as the phases need them." Whether leather adds a step is not
  known, and with no routes it does not need to be.

Answered by the developer on 1405/09/02, for 1.3 (all the proposals accepted; ADR 0019):
- **A contractor does one or more stages**, ticked from the shared list, and at least one,
  because in Phase 2 the work-order screen lists contractors by stage and one with no stage
  would appear in no list. Whether giving a contractor a stage he has not ticked is refused or
  only warned, and whether cutting can be ticked at all, is decided in 2.2, the only place it
  has an effect (as ADR 0018 left cutting).
- **Profile: full name, mobile, stages, optional IBAN, optional notes**, because the operator is
  not technical and wants few fields; a rare detail (address, workshop name) goes into the
  notes instead of a column nobody fills. **No national ID** for now: nothing uses it, and
  personal data that is not needed is not kept; if 15.0 finds an IBAN-to-national-ID check, it
  is one column. A contractor with his own helpers is one record: the person who takes the work
  and is paid, because the account is with him.
- **Mobile is required, an Iranian mobile, and one contractor per number**, because in Phase 10
  he logs into his own panel with it and an SMS code, and one number cannot open two accounts;
  it also catches the likely mistake of entering one contractor twice. **Two contractors may
  share a name**: people's names repeat, and the mobile tells them apart.
- **Every staff role may enter or change the IBAN**, because the operator types the
  contractors from the paper book and nothing is paid to it by the system before Phase 15; each
  change is audited as "changed" without the number, and the real guard (holder name verified,
  a holding period after a change) is 15.1. **Screens show only the last four digits; only the
  owner can see the whole number**, on a separate request, because until Phase 15 he pays by
  hand and needs it, while a leaked or swapped IBAN is the usual payout fraud and nobody else
  needs it once it is typed. Only Iranian IBANs (IR + 24 digits), because payouts are domestic.
- **No delete; deactivate and reactivate**, because work orders and accounts (Phase 2) will
  point at contractors. An inactive contractor is left out of pickers; his history stays.

Answered by the developer on 1405/09/02, for 1.4 (ADR 0020; the proposals he did not change were
accepted as written):
- **Every worker has a mobile**, so it is required, an Iranian mobile and one worker per number,
  because in Phase 10 it is the worker's login and it catches the same worker entered twice.
  **But some workers may not manage the app**, for example a woman who cannot read well enough;
  for them the office handles the account by hand (see 10.6). The same number may also be a
  contractor's or a staff user's, because one person may be in two lists (a manager on the
  payroll, a worker who also takes piecework home); Phase 10 decides how one login shows both.
- **The IBAN is optional for now**; when payouts start it must be verified against the holder's
  national ID (15.1). Otherwise as in 1.3: every staff role may set it, screens show the last
  four digits, only the owner sees the whole number.
- **Profile: full name, mobile, stages, start date, optional IBAN, optional notes.** No national
  ID or insurance number yet, because nothing uses them before Phase 5 and 15.
- **The job is the stages** from the shared list, **none or more**, because in-house work is
  recorded by stage with the contractors' screens (5.2), and someone like a storekeeper does no
  stage (the notes say what that person does).
- **The start date is required and not after today**, because payroll and the length of service
  count from it and a future date is almost always a mistyped year; an approximate day is typed
  when the exact one is not known.
- **No leaving date and no delete**: a worker who leaves is deactivated, because the rules of a
  final settlement are not known yet (5.5 adds the date if it needs it) and attendance and
  payslips will point at the worker. Names may repeat. The device number comes with 4.1 and pay
  terms with 5.1.

Answered by the developer on 1405/09/02, for 1.5 (ADR 0021; the proposals he did not change were
accepted as written):
- **In-house workers are paid a day wage; outside contractors are paid by the piece** (کنترات,
  by the number of pieces). So the rate card holds the contractors' piece rates only, and each
  worker's day wage belongs to 5.1 (O.9 answered).
- **A rate is per model and stage**, the same for every fabric, colour and size, because the
  operator then types a few numbers per model, not dozens; if a size is ever paid differently,
  an optional size column whose empty value means "all sizes" leaves the existing rates as they
  are.
- **One rate for every contractor**, for the same reason; a contractor-specific rate would be the
  same kind of optional column.
- **The start date may be in the past or the future**: future, because a raise announced for
  next month is entered now and starts on its day; past, because a rate taken over from the
  paper book is entered with its real date. Work orders already issued keep their own copy.
- **A mistyped rate is corrected by entering the right one with the same start date**; the later
  entry wins and the wrong one stays in the history as replaced, because nothing is overwritten
  and it stays visible who changed what.
- **Per piece, in Rial, a whole number above zero**; a stage that is not paid has no rate,
  because work orders count pieces and a zero is almost always a forgotten amount.
- **Every staff role may enter rates**, because 1.6 expects the operator to enter a model with
  its rates; every entry is audited.
- **Money display (changes ADR 0002):** wherever an amount is written, its Toman equivalent and
  its words in Toman go under it, so nobody confuses Rial and Toman (ADR 0002 point 9, built in
  1.6).

Decided with the developer on 1405/09/03, before 1.6: **1.6 is split into five tasks**
(1.6.1 to 1.6.5 below), because nearly every screen also needs something new in the API
(deactivating the lists, editing a model, the users, reading the audit) and the whole is far
more than one session. Models and rates come before contractors and workers, so the check of
1.6 ("the operator enters a real model with its rates without help") can be made after three
of them. Each task's questions are asked when it starts.

Answered by the developer on 1405/09/03, for 1.6.1 (ADR 0022; the proposals he did not change
were accepted as written):
- **The stages are shown in the order of the work**, never «دوخت» and «برش» after
  «بسته‌بندی», and **the operator drags a stage up or down** to change it. A new stage goes to
  the end.
- **No needless buttons** when a simpler action does the job: the name is edited by tapping it,
  a switch turns an item on and off, a stage is dragged by its handle.
- **Deactivating** a category, fabric, colour or stage leaves it out of the pickers for new
  work; whatever already points at it keeps it and shows it marked inactive. The API itself
  refuses a new link to an inactive item; a stage ticked on a contractor or worker before it was
  deactivated stays ticked. The rate card leaves inactive stages out; their history stays.
- **Fabrics, colours and categories on one page with a tab each**, the stages on their own page,
  because the three catalog lists work alike and the sidebar stays short.
- **Before and after building, three questions** are asked and answered in the report: is
  security complete, is this the best that could be done, and is nothing else broken.

Answered by the developer on 1405/09/03, for 1.6.2 (ADR 0023; every proposal of the plan was
accepted as written):
- **Each fabric of a model has its own colours**: the operator ticks, under each fabric, the
  colours sewn in it, so nothing that is not sewn gets a code.
- **A model with sizes makes each fabric and colour in every size**; a bag not made in one size
  is switched off on the model's page, because asking size by size multiplies the taps for a
  rare exception.
- **A size added later** is made in every fabric and colour the model still makes; added to a
  model that had none, the bags it already has become that size and keep their codes, because
  the codes are written on paper.
- **The sizes of a new model are typed one by one**, with no separate count; the description is
  on the model's page, not on the new-model form; inactive models stay in the list, greyed, with
  no filter. In the screens a variant is a «کیف» and its SKU its «کد».

- **1.1 Catalog core.** Product (model: system number + name), Variant (fabric × colour ×
  optional size → SKU), Category, Fabric, Colour, Size, `ProductTranslation` with locale
  (Persian filled, others empty). *Done when:* a product with three colours can be created and
  read back through the API.
  **Done 1405/09/01:** 256 .NET tests pass (52 new: 29 domain, 23 integration on PostgreSQL),
  26 panel tests pass, build has 0 warnings; the operator creates a model with three colours and
  reads it back (`ProductTests`). ADR 0006 (Persian-only panels, catalog text in translation
  rows, one table per kind) and ADR 0016 (numbered models, variants, SKUs). API under
  `/panel/catalog`: categories, fabrics and colours (add, rename, list); models (create with
  sizes and variants in one request, add a variant, read, search by number or name with pages).
  Persian names are normalized (Arabic «ي» and «ك», spaces) before checks and search. Found on
  the way: the audit wrote a composite key's enum part as `Fa`, not the stored `fa`; the key now
  goes through the value converter (ADR 0013 updated). Left for 1.6 with the screens: renaming
  a model or changing its category, deactivating models and variants, adding a size to an
  existing model.
- **1.2 Stages.** One shared stage list, no route per model (changed on 1405/09/01 from "stages
  and routes", see the answers above). *Done when:* a new database has the three stages, and a
  stage can be added, renamed and listed through the API, with audit.
  **Done 1405/09/01:** 271 .NET tests pass (15 new: 6 domain, 9 integration on PostgreSQL),
  26 panel tests pass, build has 0 warnings. ADR 0018. API `/panel/production/stages` (list,
  add, rename) for every staff role; the migration `AddStages` inserts «برش», «دوخت»,
  «بسته‌بندی» once, in the order of the work, and the list keeps that order. The name is one
  Persian column, not translation rows, because stages never appear in the store. No delete;
  deactivation comes with the 1.6 screens, like the catalog lists.
- **1.3 Contractors.** Profile, phone, which stages they do, active flag, optional IBAN (checksum-validated, stored encrypted, shown masked; used by Phase 15). *Done when:* CRUD plus
  search by name and phone work, with audit.
  **Done 1405/09/02:** 342 .NET tests pass (71 new: 33 domain, 38 integration on PostgreSQL),
  26 panel tests pass, build has 0 warnings. ADR 0019. API `/panel/parties/contractors` for every
  staff role: search (part of the name or of the mobile in one box, Persian alphabetical order,
  active filter, pages), read, add, save the profile, deactivate and activate, set or remove the
  IBAN on its own request; the whole IBAN only for the owner (`GET …/{id}/iban`, logged). The
  stages are a table with foreign keys to `stages`. IBANs: `Iban` value object (ISO 13616 check,
  Iranian only), AES-256-GCM with the key `Encryption:IbanKey` (`IBAN_ENCRYPTION_KEY` in `.env`),
  applied with the audit's redaction to every `Iban` property by type, so 1.4 needs no new code
  for it. The API refuses to start without the key; losing the key loses the IBANs, so it is
  kept off the server, apart from the backups (ARCHITECTURE.md 12). Found on the way: the audit
  wrote nothing for a row that is only its key (a contractor's stage); such link rows now record
  their key columns (ADR 0013 updated).
- **1.4 Workers.** In-house worker profile, job, start date, active flag, optional IBAN as in 1.3. *Done when:* same as 1.3.
  **Done 1405/09/02:** 382 .NET tests pass (40 new: 11 domain, 29 integration on PostgreSQL),
  26 panel tests pass, build has 0 warnings. ADR 0020. API `/panel/parties/workers` for every
  staff role, the same shape as the contractors: search (part of the name or of the mobile,
  Persian alphabetical order, active filter, pages), read, add, save the profile, deactivate and
  activate, set or remove the IBAN; the whole IBAN only for the owner, logged. The stages are
  `worker_stages` with foreign keys to `stages`; the start date is a `date` column. The IBAN
  needed no new code: the encryption and the audit's redaction apply to it by type (ADR 0019).
  The name and notes checks, the stage lookups and the search box are shared with the
  contractors; there is no common base class, because the two diverge from Phase 4.
- **1.5 Rate cards.** Rate per product × stage, with a start date; history kept. *Done when:* the
  rate valid on a given date can be queried and old rates are never overwritten.
  **Done 1405/09/02:** 412 .NET tests pass (30 new: 12 domain, 18 integration on PostgreSQL),
  26 panel tests pass, build has 0 warnings. ADR 0021. `PieceRate` entries (model, stage, Rial
  amount, start date, entry time) in `piece_rates`, the first money in the database (`amount`
  bigint, `currency`). API `/panel/production/rates` for every staff role: add a rate; the
  model's rate card on a day (default today in Tehran), every stage in the order of the work
  with its rate in force or none; the history of a model or of one stage, newest first, with
  replaced entries flagged. A trigger refuses UPDATE, DELETE and TRUNCATE on the table, proven
  by tests. Found on the way: the audit skipped complex properties, so a `Money` would have been
  saved without its amount in the history; their parts are now audited as fields named after
  their columns (ADR 0013 updated).
- **1.6 Panel screens.** Products, stages, contractors, workers, rates; and the owner's users
  screen (list, add, deactivate, change a user's mobile when a phone is lost; added 1405/08/27
  with ADR 0012); and, for the owner, the change history of a record from the audit (added
  1405/08/28 with ADR 0013). *Done when:* the operator can enter a real model with its rates
  without help. The stage list is shown in its stored order (the order of the work), not sorted
  by name (ADR 0018). Added 1405/09/02 (ADR 0002 point 9): the first amounts on screen are the
  rates, so this task builds the money display used everywhere after it: the Rial figure, and
  under it a quieter line with the Toman equivalent and its words («۱۲٬۰۰۰ تومان · دوازده هزار
  تومان»), following the typing under an amount field. Split on 1405/09/03 into:
  - **1.6.1 Lists and stages.** The panel's shared list parts; fabrics, colours and categories
    on one page with tabs, the stages on their own: add, rename, deactivate and activate; the
    stages dragged into the order of the work. API: an active flag on the four lists, refusing
    new links to inactive items, a stage position. *Done when:* the operator adds, renames,
    deactivates and activates a category, fabric, colour and stage, and drags a stage into
    place, and the API refuses a new link to an inactive item (with tests).
    **Done 1405/09/03:** 446 .NET tests pass (34 new: 13 domain, 21 integration on
    PostgreSQL), 42 panel tests pass (16 new), build has 0 warnings. ADR 0022. API: `…/deactivate`
    and `…/activate` for categories, fabrics, colours and stages; `PUT
    /panel/production/stages/order` with the whole list (a list made from an old copy is
    refused with `production.stage_order_outdated` and changes nothing); `catalog.*_inactive`,
    `parties.stage_inactive`, `production.stage_inactive` for new links to inactive items. Every
    list of stages (stage list, contractors, workers, rate card, rate history) now follows the
    position instead of the id. Panel: «فهرست‌ها» (`/lists/fabrics|colours|categories`) and
    «مراحل کار» (`/stages`) in the sidebar; names edited where they stand, a switch for active,
    stages dragged by a handle (mouse, finger, or arrow keys) on Pointer Events, no library.
    Changed while building: inactive rows stay in place, greyed, instead of moving to the end,
    so the switch stays under the finger and the stage list never misstates the order.
    Checked in Chrome against a throwaway database (dropped afterwards): adding, the duplicate
    name message, renaming in place and Escape, the switch, the tabs, a stage dragged with the
    mouse and moved with the arrow keys, the order kept after a reload, no console errors.
    Found on the way: an add that took two seconds left the button disabled, so a second Enter
    was lost and the next name typed meanwhile was erased; the field now empties when a name is
    sent and a refused one comes back. Not checked by eye: the phone width (the browser window
    was minimized), so the drag by finger waits for a check on a real phone.
  - **1.6.2 Models.** Search and pages; create a model with its sizes and variants; the model
    page. API: rename a model, change its description and category, deactivate models and
    variants, add a size to an existing model.
    **Done 1405/09/03:** 475 .NET tests pass (29 new: 17 domain, 12 integration on
    PostgreSQL), 56 panel tests pass (14 new), build has 0 warnings. ADR 0023. API under
    `/panel/catalog/products/{id}`: save the name, description and category (`PUT`);
    `…/deactivate` and `…/activate`; several bags in one request, all or none (`POST
    …/variants`, replacing the one-bag request); `…/variants/{variantId}/deactivate|activate`;
    sizes added (`POST …/sizes`), renamed (`PUT …/sizes/{sizeId}`) and reordered (`PUT
    …/sizes/order`). Every change answers the whole model. A deactivated model takes no new
    bag, size (`catalog.product_inactive`) or rate (`production.product_inactive`). Migration
    `SizePositionNotUnique`. Panel: «مدل‌ها» (`/models`, search and pages in the address),
    «مدل تازه» (`/models/new`), and the model's page (`/models/:id`).
    Checked in Chrome against a throwaway database (dropped afterwards), at a phone width of
    390 px: no sideways scrolling on any of the pages; a new model with two sizes and three
    fabric and colour pairs made six codes in order; the duplicate size typed on an Arabic
    keyboard, the empty name, the missing sizes and the taken name are refused with their
    messages; renaming, the category, the description, the model's and a bag's switch, a size
    added to a model with sizes and to one without, sizes renamed, moved with the arrow keys and
    dragged with a real mouse, more bags added with the locked ticks; the search by Persian
    digits and by a name typed on an Arabic keyboard, two pages and the back button; the stages
    and the lists of 1.6.1 again; no console errors. The Chrome window was minimized and could
    not be restored from the tools, so the phone width was a 390 px frame inside the page,
    measured through the DOM, not seen by eye. Found on the way and fixed: a quick drag released
    before React had drawn its last move dropped the row one place short, or not at all (also
    on the stages, ADR 0022 updated); the button that removes a typed size was 28 px, now 36;
    a page change in the model list now scrolls back to the top. Still to check on a real
    phone: the drag by finger.
  - **1.6.3 Rates and the money display.** The Rial figure with the Toman line and words; the
    amount field with its live line; the Jalali date field; a model's rate card, a new rate, the
    rate history. Ends with the check of 1.6: a real model entered with its rates without help.
  - **1.6.4 Contractors and workers.** Search with the active filter and pages; the profile
    form with stages; the IBAN (last four digits; the whole number only for the owner).
  - **1.6.5 Users and change history.** The owner's users screen; a «تاریخچه» on the records,
    for the owner. API: list users, deactivate and activate, change a mobile (ending the user's
    sessions), read the audit of a record with names instead of ids.

## Phase 2 — Contractor work and accounts

Goal: the paper notebook for contractors is replaced. This is the heart of the system.

Open questions: O.3; is material handed over tracked from the start or only in Phase 6; are
rejected pieces deducted or only recorded; who approves QC. Added 1405/09/01 with ADR 0018:
is the number of cut pieces recorded (cutting is in-house); are bags sewn by in-house day-wage
workers counted, so that progress (2.8) shows all sewing and not only the contractors'; does
the system refuse to issue cutting to a contractor (decided in 2.2).

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
  outside the repository. ADR on deployment. *Done when:* the stack runs from images on a clean
  machine, and from outside: the security headers are on every response, plain HTTP redirects to
  HTTPS, a burst of login requests from one IP is refused, and the API's database user cannot
  drop a table.
  Added 1405/09/02, after the project was checked against a generic security checklist. Each item
  needs the proxy, the domain or the production database, which is why it waits for this task:
  - **Security headers and HTTPS in Caddy:** HTTP redirects to HTTPS, HSTS,
    `X-Content-Type-Options: nosniff`, `frame-ancestors 'none'`, `Referrer-Policy`, and a
    Content-Security-Policy for the panel. Why in the proxy: one place covers both the panel's
    static files and the API. None of them exists today and no other task names them.
  - **Forwarded headers, then a per-IP rate limit on `/panel/auth/*`** (the ASP.NET Core rate
    limiter), and a wrong login code logged as a warning with the IP. Why in this order: until
    the API trusts the proxy's `X-Forwarded-For`, every request seems to come from the proxy, so
    a per-IP limit would block everyone at once and the logged IP would be useless. The
    per-number code limits (ADR 0012) stay; this adds a limit on one machine trying many numbers.
  - **Two database users:** the table owner runs migrations only; the API connects as a user
    that can read and write rows but cannot change the schema. Why: the owner of `audit_entries`
    can drop its append-only trigger (ADR 0013), so while the API connects as the owner, a bug or
    a break-in can still erase the audit. The running API therefore does not hold the owner's
    password (see the migration note in docs/ARCHITECTURE.md).
  - **`AllowedHosts`** set to the real domains instead of `*`, so a request with a forged `Host`
    header is refused.
  - **Fresh production keys:** the JWT signing key, the IBAN key and the database passwords are
    made new for the server, never copied from a development `.env`, because development keys
    sit on the developer's machine and in its logs.
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

Open questions: insurance and legal deductions in or out; pay period (monthly or weekly). O.9 is
answered: in-house workers are paid a day wage (1405/09/02).

- **5.1 Pay setup.** Per worker: fixed salary, hourly, piecework or mixed; overtime and lateness
  factors, with history. *Done when:* the terms valid on any date can be queried.
  Answered 1405/09/02: in-house workers are paid **a day wage**, so the terms are each worker's
  day wage with a start date, kept like the piece rates (ADR 0021: entries, never overwritten);
  piecework is for contractors only. Whether a day wage ever comes with a piece bonus is asked
  before Phase 5.
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
  Said by the developer on 1405/09/02 (ADR 0020): some workers will not manage the app, for
  example a woman who cannot read well enough, so the app is never the only way: the office
  prints or reads the same statement (5.6), and nothing waits for a worker to log in.
- **10.7 Requests.** A worker asks for leave or an advance in the app; the manager approves with
  one tap. *Done when:* an approved request lands in Attendance or the ledger without retyping.

## Phase 11 — Store: three-language catalog

Goal: the brand is visible again on the web, in three languages, with real products.

Open questions: O.12; third language confirmed as English; where the store is served from for
foreign visitors (ADR 0008); show prices to everyone or wholesale prices only after login;
where Arabic and English text is entered, since the developer said on 1405/09/01 that no
translation is done in the panel (11.5 still lists translations and must change with the answer).

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

- **14.1 Security review.** Auth, permissions, uploads, rate limits, dependency audit, and a
  secret scan of the whole git history with gitleaks. Why gitleaks (added 1405/09/02): the check
  on that day was a hand-written grep over 16 commits and found nothing, but gitleaks knows far
  more key formats than a hand-written pattern.
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
  Decided by the developer on 1405/09/02 (ADR 0020): the IBAN is verified against the holder's
  **national ID**, so this task adds the national ID to contractors and workers (they have none
  until then, because nothing else uses it).
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
| Q.1 | Real stages of making a bag, and which are done outside. **Answered 1405/09/01:** cutting (in-house only), sewing (contractors or in-house day-wage workers), packing; one shared list, no route per model (ADR 0018) | Phase 1 |
| Q.2 | Contractor pay method and rates; what happens to rejects. **Method answered 1405/09/02:** by the piece, per model and stage (ADR 0021); rejects still open | Phase 2 |
| Q.3 | Stored money unit: Toman or Rial. **Answered 1405/08/24: Rial, everywhere** (stored and shown), ADR 0002. **Added 1405/09/02:** under every Rial amount shown, its Toman equivalent and its words in Toman (ADR 0002 point 9) | 0.3 |
| Q.4 | Do contractors receive raw material or cut pieces | Phase 2 / 6 |
| Q.5 | Shift hours, grace period, overtime, leave rules | Phase 4 |
| Q.6 | In-house pay types; legal deductions in scope or not. **Pay type answered 1405/09/02: a day wage**; legal deductions still open | Phase 5 |
| Q.7 | Wholesale price tiers, credit and return policy | Phase 8 |
| Q.8 | Third language is English (assumed) | Phase 11 |
| Q.9 | Where the store is served from for foreign visitors | Phase 11 |
| Q.10 | Number of models and colours today. **Answered 1405/09/01: many** (no exact count); models have no codes today; a bag is model + fabric + colour + optional size (see Phase 1) | Phase 1 |
| Q.11 | Registered company or individual; which bank | 12.3 / 15.0 |
| Q.12 | How often and how contractors are paid today | Phase 2 |
