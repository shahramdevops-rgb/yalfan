# Yalfan — Finished tasks

The entries of finished tasks, moved out of `docs/ROADMAP.md` with their «Done» reports, so the
roadmap that every session and every review reads stays short (task 1.6.5b, 1405/09/05). Each
entry is as it was when its task finished: what was asked, what was delivered, how it was
checked and what was found on the way. Search for a task's number rather than reading the whole
file. The decisions each phase rests on, and the open questions, stay in the roadmap.

When a task is finished, its whole entry moves here, under its phase, and one status line stays
in the roadmap with anything still open.

## Phase 0 — Foundation

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

### 1.6 Panel screens

The entry of 1.6 itself stays in the roadmap until its last part is finished.

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
    **Done 1405/09/04** (except the check of 1.6, which waits for the developer): 475 .NET tests
    pass (none new: the API did not change), 101 panel tests pass (45 new), build has 0
    warnings. ADR 0024. Panel: the «نرخ کنترات» card on the model's page, under the name: each
    active stage in the order of the work with today's rate, its start date, a raise entered for
    later, and its history (today's, still to start, past, corrected); a tap on a rate opens a
    form for a new one, a tap on an entry of the history corrects it on its date, and the form
    says what saving will do. Shared parts: `Money` (the Rial figure and the Toman words under
    it), `AmountField` (grouped in threes while typed, Persian, Arabic and Latin digits, the
    caret kept in place, 15 digits at most), `JalaliDateField` (day, month and year lists through
    the browser's own Persian calendar, Esfand 30 only in a leap year), and the Persian number
    words. Checked in Chrome against a throwaway database (dropped afterwards): a new model with
    no rate; a rate typed in Persian digits; a zero refused with its message; a raise from 1 Dey
    shown on the card; a correction from the history, the old entry marked corrected; a rate
    from 1404 that only joins the history, whose history opened by itself; a digit typed in the
    middle of an amount and deleted, a letter and a comma dropped, the caret in place each time;
    Esfand 30 in 1403 and not in 1404; Escape; an inactive model with no new rate; a deactivated
    stage left out of the card; the lists, stages and models pages again; no console errors.
    The phone width was a 390 px frame inside the page, measured through the DOM (no sideways
    scrolling; the three date lists on one row) and seen in a screenshot. Found on the way and
    fixed: the «اصلاح‌شده» label was struck through and hard to read; the year list fell to a
    second line on a phone.
  - **1.6.4 Contractors and workers.** Search with the active filter and pages; the profile
    form with stages; the IBAN (last four digits; the whole number only for the owner).
    **Done 1405/09/04:** 479 .NET tests pass (1 new: every `/panel` answer, a refusal too, carries
    `Cache-Control: no-store`), 127 panel tests pass (26 new), build has 0 warnings. ADR 0025.
    API: only that header, set by a middleware before the error handler. Panel: «کنتراتی‌ها»
    (`/contractors`, `/contractors/new`, `/contractors/:id`) and «کارگران ثابت» (`/workers/…`) in
    the sidebar, one set of components for both. The list: one search box for part of the name or
    of the mobile, twenty per page, only the active people unless «غیرفعال‌ها هم دیده شوند» is on,
    all in the address. The new-person form: name, mobile, stages, a worker's start date (empty
    until picked, forty years back to this year, newest first), the optional IBAN and notes; every
    refusal under its field. The person's page: name, mobile and notes edited in place, each stage
    tick saved at once (a contractor keeps his last one), a switch, a worker's start date in a
    small form; its changes go one after another, each built from the last answer. The IBAN card:
    «•••• ۱۰۰۱», a form with IR fixed and the digits grouped as on a bank card, «پاک کردن شبا»
    inside it; for the owner «نمایش کامل» with «کپی». Shared parts taken out on the way: the search
    box, the pager and the list's address (the models page uses them now), the choice chip (the
    bag picker uses it), the IBAN field, a notes field, the mobile shown grouped, and an empty
    state for the Jalali date field (the rate form is unchanged).
    Checked in Chrome against a throwaway database (dropped afterwards), 27 contractors and 4
    workers: Persian alphabetical order and two pages; the search by Persian digits of a mobile;
    the back button; the inactive switch; a whole IBAN shown to the owner, copied in Latin digits
    and logged by the API; the notes left by tapping a stage, both saved; a contractor's last stage
    locked; a taken mobile refused with its message, a new one saved from Persian digits and from
    +98; a short IBAN, a mistyped one (refused by the API) and the caret kept when a digit is typed
    in the middle; an IBAN replaced and removed, the whole number on show gone with it; a
    deactivated stage kept on a worker who had it and left out of a new form; a future start date
    refused under the field; the search text not carried from one list to the other; the models
    page, a new model with the shared chips, and the rate form's date field again; no console
    errors. The phone width was a 390 px frame inside the page, measured through the DOM (no
    sideways scrolling on any of the seven pages) and seen in screenshots. Found on the way and
    fixed: a note saved by tapping a stage could be undone by the stage's save, both sending the
    whole profile at once (the page now sends its changes one after another); the IBAN field
    showed its hint under the error, and a refused IBAN left the caret out of the field.
    Fixed after the review the same day (asked by the developer: no bug left behind), each seen
    in Chrome before and after: the models page had the same gap, a description left by choosing
    a category vanished from the screen and the next save would have erased it (its changes now
    go through the same queue, `useSerialWrites`); in all three lists the sidebar link to the same
    list cleared the address but the old search came back from the box, and a page past the last
    one said the list was empty. Checked again: the model's description and category together,
    sizes added and moved, the model's and a bag's switch, new bags; the notes and a stage on a
    contractor; the back button and a space typed in the middle of a search.
    Reviewed the same day by the `reviewer` agent (twice), and fixed: a person's or a model's page
    kept its state when it went straight to another record (now keyed by the id; seen in Chrome
    with an open IBAN form); the contractor's last-stage lock tested through the code the page
    runs; tests for "each change from the last answer"; the search box no longer erases a letter
    typed while its own search reaches the address; placeholder data no longer moves a valid
    page. Left open for the developer: Q.13 (two users saving the same record) and whether the
    panel gets component tests (ADR 0025, "Not decided here"). 135 panel tests pass.
  - **1.6.5 Users and login numbers.** The owner's users screen. API: list users, deactivate and
    activate, change a mobile (ending the user's sessions), correct a name and a role; owner
    accounts with several login numbers from the configuration (added 1405/09/05, ADR 0026).
    **Done 1405/09/05:** 525 .NET tests pass (46 new: 12 domain, 34 integration on PostgreSQL),
    145 panel tests pass (10 new), build has 0 warnings. ADR 0026. A user logs in with one or more
    numbers (`user_login_numbers`, migration `UserLoginNumbers`, which copies each user's number);
    a login code, its wait and its hourly limit belong to one number. `Identity:Owners` replaces
    `Identity:SeedOwner` (the old setting stops the start): owner accounts are created or given
    their missing numbers at start, never switched back on; compose, `.env.example` and the README
    follow. API, owner only: `GET /panel/users`, `PUT …/{id}/name|role|mobile`,
    `POST …/{id}/deactivate|activate`, `POST …/{id}/numbers/{numberId}/deactivate|activate`; a new
    number, a deactivation and a number switched off revoke every session of the user; one number
    always stays on (`identity.last_login_number`); an owner account is locked otherwise
    (`identity.owner_locked`). The access token lives ten minutes; `/panel/auth/me` carries no
    mobile. Panel: «کاربران» (`/users`) in the sidebar for an owner only, «not found» for anyone
    else; a card per user with the name edited in place, the role as two chips, the number edited
    in place with a line saying the user will be logged out everywhere, a switch; an owner
    account with a switch per number, the last one on locked; the new-user form starting at
    «اپراتور دفتر». Checked in Chrome against a throwaway database (dropped afterwards): the two
    owner accounts from the configuration, the developer's single number locked; a name corrected
    in Persian; the empty form and a taken number (the owner's second, typed in Persian digits)
    refused under their fields; a user added; his role changed; his number changed from +98 with
    the line shown while typing; deactivated with its note and activated; the owner's second
    number switched off from his own session, which went to the login at once; that number then
    got no code while the first logged in, with the first locked as the last one on; the second
    switched back on without logging out; a manager saw no «کاربران» and `/users` said not found;
    the contractor and model pages again; no console errors. The phone width was a 390 px frame
    inside the page, measured through the DOM (no sideways scrolling), not seen by eye: the
    Chrome window was minimized and screenshots timed out. Found on the way and changed: the
    stolen-phone hint is shown only on an account with more than one number.
    Reviewed the same day by the `reviewer` agent, and fixed: a thief whose page on the stolen
    phone still worked could switch the stolen number back on, so a number of one's own account
    is now switched back on only by another owner account (`identity.own_number_switch_on`; seen
    in Chrome: locked on the owner's own card, free on the developer's); "one number stays on" is
    also a constraint trigger in the database, against two switches at once; the same number
    under two configured accounts stops the start; a configured account none of whose numbers is
    left is logged as a warning, and compose has a spare slot per account, with the way to change
    a number written in `.env.example` and the ADR; the numbers are listed in number order, since
    ids made in the same millisecond have no order (a test would have failed now and then).
  - **1.6.5b Housekeeping (asked by the developer on 1405/09/05, for the next session).** The
    review of 1.6.5 alone took about 158k tokens and the 1.6.5 session reached 360k in context,
    so the developer asked for these, in one session, before 1.6.6:
    1. **The real login numbers in the local configuration.** The developer's own owner account
       and the workshop owner's account, in the git-ignored `.env` (`DEVELOPER_MOBILE`,
       `OWNER_MOBILE`, with `DEVELOPER_FULL_NAME`, `OWNER_FULL_NAME`) and in user-secrets
       (`Identity:Owners:…`, README). The numbers are in the session's private memory note
       (`owner-login-numbers`), never in the repository. Ask the two names, and whether the third
       number (the one not yet placed) is the owner's second (`OWNER_MOBILE_2`).
    2. **Each task in a new session.** A long session sends its whole context again with every
       message; write it into CLAUDE.md's workflow: when a task is committed, the session ends.
    3. **A lighter roadmap.** Move the «Done» reports of finished tasks into
       `docs/roadmap-done.md` and leave one status line per task here, because every session and
       every review reads this file and it grows with each task (83 KB on 1405/09/05). Update
       "Where things are" in CLAUDE.md.
    4. **Ask before the review, and choose its model.** Before running `reviewer`, the session
       asks the developer, saying which model it chose and why: Sonnet for an ordinary task,
       Opus for one that touches money, access, login or the audit, where a missed rule costs
       most. The session chooses; the developer only says yes or no. Written into CLAUDE.md's
       workflow; the model is passed with each call, not fixed in `.claude/agents/reviewer.md`.
    5. **A narrower review brief.** The session hands the reviewer the risky files and the ADR
       sections to read; the reviewer greps the task's lines in the roadmap instead of reading it
       whole, and skips what no rule covers. Written into `.claude/agents/reviewer.md`.
    6. **Smaller tasks.** When a task grows while it is planned (as 1.6.5 did with the owner's
       numbers), it is split before building, so each diff and each review stays small. Written
       into CLAUDE.md's workflow.
    *Done when:* the numbers log in on the local panel; CLAUDE.md, `.claude/agents/reviewer.md`
    and the roadmap carry the rules above; `scripts/check.ps1` is green.
    **Done 1405/09/05:** no code changed; 525 .NET tests and 145 panel tests pass, build has 0
    warnings. Not reviewed by the `reviewer` agent: asked under the new rule (Sonnet, since only
    docs changed), the developer chose no review for this task.
    (1) The two owner accounts, with the names the developer gave, are in the git-ignored `.env`
    and in user-secrets: the workshop owner with two numbers, the developer with one. Checked by
    logging in with each number through the panel's dev server, the same requests the login page
    sends (the Chrome extension was not connected, so not seen by eye): the owner's two numbers
    reach one account, the developer's number another, and «کاربران» lists the two. The
    development database held only «Test Owner» from 0.5 (a test number) and the three stages;
    at the developer's word «Test Owner» was deleted with its session and its login number (no
    audit entry pointed at it), so no made-up owner can log in there. The sample data of the
    browser checks could not be kept for the developer: each check ran on a throwaway database
    that was dropped afterwards. A grep found none of the numbers or names in the repository.
    (2, 4, 6) CLAUDE.md's workflow: a new session for each task; the reviewer's model chosen by
    the session and asked of the developer (`reviewer.md` keeps `inherit`, so a forgotten model
    falls back to the session's own, never a weaker one) with a brief of the risky files and the
    ADR sections; a task split when it grows while planned; a finished task's entry moved here.
    (3) The entries of 0.1 to 1.6.5 moved here verbatim, checked by a script that found every
    removed line in this file, in order; the roadmap went from 85 KB to about 57 KB; "Where things
    are" in CLAUDE.md and the README name this file. The Phase 1 decisions stay in the roadmap,
    because later tasks rest on them. (5) `reviewer.md`: works from the brief, greps the task's
    lines instead of reading the roadmap, and skips the changes no rule covers.
  - **1.6.6 Change history.** A «تاریخچه» on a model (with its bags, sizes and rates), a
    contractor and a worker (with their stages; an IBAN as «شبا عوض شد», with no number), for the
    owner, on its own page: newest first, one block per save with who and when, each change in the
    workshop's words. API: read the audit of a record with names instead of ids. (Split from 1.6.5
    on 1405/09/05; the shape was accepted with 1.6.5's questions.)
    **Done 1405/09/05:** 535 .NET tests pass (10 new integration tests on PostgreSQL; the two
    IBAN audit tests extended), 156 panel tests pass (11 new), build has 0 warnings; no migration
    (`dotnet ef migrations has-pending-model-changes`: none). ADR 0027; ADR 0013 updated. API,
    owner only on the whole group: `GET /panel/history/models|contractors|workers/{id}`, twenty
    saves a page, `?before=` for the next; each save its moment, the user's name and its changes
    (part, action, label, fields as text with ids already named), fields in a fixed order. The
    audit is read by exact keys with `UNION ALL` on the existing index, through an untracked
    `IQueryable` in `IAppDbContext`; the audited tables are constants shared with the EF
    configurations; every recorded field is named or left out in `HistoryVocabulary`, and a test
    fails on one the vocabulary does not know. The audit now records whether a redacted field had
    a value before and after (`oldSet`, `newSet`), so the history says «شبا ثبت شد / عوض شد / پاک
    شد». Panel: «تاریخچه» on the model, contractor and worker pages for the owner only, «not
    found» on the history page for anyone else; a card per save with the name, the Jalali date and
    the time in Tehran (`formatTime`); sizes dragged into a new order as one line; more than six
    bag lines folded; a new rate with its amount and Toman words; codes and mobiles kept left to
    right inside the Persian lines. Checked in Chrome against a throwaway database (dropped
    afterwards), with a model changed by a manager and the owner, a model of eight bags, a
    contractor and a worker: every card in its order with names instead of ids, the folded bags,
    the IBAN set, replaced and removed, the start date, the stages. Found in Chrome and fixed:
    a mobile read in reverse groups and a bag code reversed (now Unicode isolates, measured
    through the DOM against the model page's left-to-right codes); a Latin user name pulled the
    date around it (now `<bdi>`); fields came in jsonb's key order (the IBAN first) and a new
    model's sizes in today's dragged order (both fixed in the API, and the integration tests now
    assert the order). The phone width was a 390 px frame measured through the DOM (no sideways
    scrolling), not seen by eye; a manager's view (no link, «not found») was not opened in
    Chrome: the API's 403 is tested and the page uses the users screen's check.
    Reviewed the same day by the `reviewer` agent on Sonnet (the developer's choice): no broken
    rule and nothing blocking. Its three questions, answered without a change: a save whose user
    is not found would read «سیستم», which cannot happen since users are never deleted; the page
    never sees a loading session, because `RequireSession` renders the routes only once signed in;
    only the Persian translation is read, as ADR 0027 says.

### 1.7 Design system

The entry of 1.7 itself stays in the roadmap until its last part (1.7d) is finished.

  - **1.7 Screenshots.** Playwright screenshots of the main pages at desktop (1280) and phone
    (390) width, compared on each run of `scripts/check.ps1` and in CI. Decided with the
    developer: Playwright runs **inside its official Docker image**, pinned, on his PC and in
    CI alike, because fonts render differently on Windows and Linux and a picture taken on one
    always fails on the other (about 2 GB pulled once); the API is **answered by fixed sample
    data** in the browser and the clock is frozen, so no database, SMS or backend is needed and
    every run is the same; the comparison runs **on every run of the check script**, not only
    in CI, because the developer runs it before each commit. *Done when:* the pictures exist for
    both widths, a changed pixel fails the check with the page named, and the ADR is written.
    **Done 1405/09/06:** 535 .NET tests and 156 panel tests pass (none changed), and 28
    screenshots pass: 14 pages, each at both widths, the whole page. Build has 0 warnings. ADR 0028;
    ADR 0014 point 7 and ADR 0017 point 1 updated. `@playwright/test` 1.65.0, exact, installed
    with `--before=2026-11-12`; the image `mcr.microsoft.com/playwright:v1.65.0-noble` pinned by
    digest (about 1 GB, not 2). `npm run screens` and `npm run screens:update`
    (`scripts/screens.mjs`) run it in Docker with `--shm-size=1g` rather than Playwright's
    suggested `--ipc=host`, which would share the host's memory; `node_modules` lives in a
    Docker volume and the production build in the container's `/tmp`, so the PC's own are not
    touched. The API is answered in the browser from sample data typed by the contract and
    type-checked by the build (`screens/api.ts`, `screens/sample.ts`: made-up names, mobiles
    `0912 000 0xxx`); the clock is frozen at 1405/09/04, ten in the morning in Tehran. A request
    with no answer or an error thrown in the page fails the test naming it. `check.ps1` has a
    `panel screens` step that prints each failed page and width with its reason; CI has a
    `Screens` step and keeps the pictures of a failure as the artifact `screens`. How it was
    checked: runs with no change were all green, five times; a text moved one shade
    (`stone-500` to `stone-600`, the stages under the names in the contractor and worker lists)
    **passed with Playwright's default `threshold` of 0.2**, so the tolerance is now none, and
    it then failed on exactly those two lists at both widths, with the diff picture marking the
    line; a sample answer taken out failed as «requests the sample API has no answer for: GET
    /panel/users»; the version check stops a mismatched image before Docker; the `subst` drive
    resolves to the real path for Docker. The pictures were looked at: real pages with their
    data, not errors or grey placeholders. Found for 1.7b: the model, contractor and worker
    pages have no `h1`.
    Reviewed the same day by the `reviewer` agent on Sonnet (the developer's choice): nothing
    blocking. Its three findings, fixed: `@playwright/test` was `^1.65.0`, so an `npm update` could
    move it away from the image (now exact, and the script compares the lock with the image);
    a failed run could have left its pictures in `web/panel/test-results` if the script was
    bypassed (now always the mounted `artifacts/check/screens`); three pages (a new model, a new
    contractor, the history) waited only for their heading (now every page that loads data waits
    for a text from it). Its question, whether `screens:update` takes the picture of a page just
    added, answered from the first run, which took all 28 that way. Its note that the container
    runs as root, which matters only on a Linux PC, is in ADR 0028.
  - **1.7b Tokens and the simple parts.** The tokens by role (surface, line, muted text,
    danger, warning, success, info, brand) with Tailwind's default colours switched off, so
    each is changed in one place and a raw colour cannot creep back; a page header, a status
    badge (colour and text), a link that looks like a button, the form layout; the 1.6 screens
    moved onto them; `docs/design/UI.md` begun. Found while planning: `ring-brand-200` on the
    IBAN form names a shade that does not exist, so the ring is drawn in the text colour. Found
    in 1.7: the model, contractor and worker pages have no `h1`, which the page header gives
    them. The pictures of 1.7 show every change this task makes: one that should look the same
    must leave them unchanged.
    Decided with the developer on 1405/09/06, who accepted every proposal and left the look to
    the session: each role keeps the value it had, so the pictures prove the move; the name of
    a record, edited where it stands, is its page's `h1`; component tests wait for 1.7c, where
    the parts have behaviour. Mid-task he sent the workshop's logo, business card and hang tag
    and asked that they be used where needed: the panel took their olive green and cream, in a
    second commit, so the first one still shows the move changing nothing.
    **Done 1405/09/06:** 535 .NET tests and 159 panel tests pass (3 new: the colour check), and
    28 screenshots pass. Build has 0 warnings. ADR 0029; `docs/design/UI.md`; a line in
    `CLAUDE.md` and in `docs/ARCHITECTURE.md` section 9.
    - Colours: 376 raw colour classes in 39 files moved onto 32 roles in `src/index.css`
      (`page`, `surface`, `ink`, `muted`, `brand`, `danger-soft`…), each first with Tailwind's
      exact value; Tailwind's palette is off. No `info` yet: nothing uses it, and an unused
      colour is never seen in a picture.
    - `web/panel/checks/colours.test.ts` gives every colour class in `src` to Tailwind's own
      compiler and fails on one that makes no CSS (a raw colour, a misspelt role, under any
      variant), on a colour written out and on a class built from pieces. It lives with the Node
      project, because the app's has no Node types.
    - Parts: `PageHeader`, `StatusBadge`, `Notice`, `ButtonLink` and `TextLink` (Button's
      classes shared through `buttonStyle.ts`), `Field`, `FieldError`, `FormFields`,
      `FormSection`, `FormActions`, `InsetForm`, `FormCard`, `CardHeader`, and `InlineName
      heading`. Gone: five hand-written «غیرفعال» badges, eight hand-written card frames, two
      copies of `Field`, nine back and history links, ten warning and refusal boxes. The login
      page and the crash page keep their own centred layout.
    - The workshop's colours (second commit): `brand` is the logo's olive green (`#4b5a34`;
      white on it 7.5 to 1, teal gave 5.4) and its lettering `#3c4829` for `brand-strong`; the
      page ground is the business card's cream (`#f5f1e8`); cards stay white. The hint under a
      page title, the login page's subtitle and the loading line moved from `muted` to `body`,
      because `muted` on the cream is 4.26 to 1, below the 4.5 text needs (it was 4.41 on the
      grey already). The card's thin gold line is kept for the store. The bag mark of the panel
      stays as drawn, in the olive, until the logo's own file arrives (O.12b).
    How it was checked: right after the colours moved, before any part, all 28 pictures passed
    with no pixel changed. Three mistakes planted in `Card.tsx` (`hover:bg-subtel`,
    `text-stone-500`, `bg-[#fff]`) each failed the colour check, naming the file. After the
    screens moved onto the parts, 23 pictures stayed equal and five changed, from three pages,
    each looked at and taken on purpose: the history page's header now like every page (back
    link above a `text-2xl` title), the new-user form ends with a line and its button like the
    other two forms, the dashboard card's padding on a phone like every card. No picture shows
    an open IBAN or rate form (their new `brand-edge` frame changed no picture). The record
    pages' `h1` changed no pixel. The second commit changed all 28 pictures, which the developer
    looked at before it was committed. Not seen by eye: the pointer and focus states; the colour
    check proves each of their classes makes CSS, and their values did not change in the first
    commit.
    Reviewed the same day by the `reviewer` agent on Sonnet (the developer's choice): nothing
    blocking; no access, money, IBAN or audit rule broken, every handler, id and `role="alert"`
    kept. Its two findings, fixed: the colour check skipped a class under a variant with
    brackets (`data-[state=on]:`, `@md:`) and a piece built as `'bg-' + tone` (now found, with a
    test of its own); a record's `h1` was read by screen readers as «تغییر نام …» (its button now
    has no `aria-label` when it is the heading). Its questions, answered in ADR 0029: `Field` is
    a named group in every use, because a value edited in place («۰۹۱۲ …») needs its label read
    with it, and a text field never goes inside one; the new-user `FormCard` is a named form
    because it is one part among several; the docs now say that only the colours are enforced
    by a tool, the parts by the review.
