# Yalfan

Management panel and online store for the Yalfan bag workshop (Hamedan): about 20 in-house workers
and about 40 outside contractors paid by the piece. Three front doors:
- The panel: Owner, workshop manager, one office operator. Persian only.
- The contractor panel: each contractor sees only his own work and account, on a phone. Persian only.
- The store: retail, wholesale and foreign buyers. Persian, Arabic, English.

Status: Phase 1 (master data) in progress; see docs/ROADMAP.md for the next task. The developer
works alone; the workshop owner is his brother and is treated as a client.

## Where things are
- Roadmap, task status, the owner's to-do list, open questions: docs/ROADMAP.md (one status line
  per finished task)
- What each finished task delivered, how it was checked, what was found: docs/roadmap-done.md
  (search it for the task number; do not read it whole)
- Layers, modules, core models, attendance, languages: docs/ARCHITECTURE.md
- Architecture decisions, once written: docs/adr/
- How panel screens are built (colours, sizes, shared parts): docs/design/UI.md
- CI (GitHub Actions; build and every test on each push): .github/workflows/ci.yml

## How to answer the developer
- Reply in Persian.
- IMPORTANT: the very first word of every reply must be Persian, and so must the first word of
  every paragraph, heading, list item and table cell. The developer's screen picks the text
  direction from the first character; a line that opens with English, a path, code or a number
  renders left-to-right and the text gets mixed up. Rephrase so it opens in Persian
  («فایل `Foo.cs` …», not «`Foo.cs` …»). Code blocks are exempt.
- IMPORTANT: every decision, recommendation, fix or chosen option comes with its reason, in a
  short clause or line («X، چون Y»). That includes the cause of an error before its fix. When the
  developer asks "why", answer with the trade-off, not just the rule.
- When a decision is made in a chat, record it with its reason where it belongs (an ADR, the
  roadmap, or docs/ARCHITECTURE.md), so the "why" is not lost when the chat ends.

## Language and UI
- Panels are Persian and right-to-left. Only the store is multilingual (fa, ar, en).
- Code, identifiers, comments, commits, logs and docs are English.
- The API returns stable error codes; each frontend maps codes to its own language.
- No user-facing text inside components: message files per locale.
- Product names and descriptions live in translation rows, never in a single Persian column.
- Use logical CSS utilities (`ms-`, `me-`, `ps-`, `pe-`, `start-`, `end-`), never `left`/`right`
  ones, because fa and ar are right-to-left and en is left-to-right.
- Panel colours only by their role names in `web/panel/src/index.css` (`text-muted`,
  `bg-danger-soft`; docs/design/UI.md, ADR 0029), never a Tailwind colour or a written-out one;
  a raw colour fails the tests.
- The backend stores Gregorian UTC only. Jalali, Hijri or Gregorian is a display choice.
- Money is Rial: stored, in the API, typed and shown (ADR 0002), and every amount shown is
  labelled «ریال». Under every Rial amount shown, a quieter line says it in words, in Toman,
  with no Toman figure («دوازده هزار تومان»; ADR 0002 point 9, ADR 0024). Toman is never
  stored, sent by the API or typed.
- The office operator is not technical: few fields per screen, workshop words, safe defaults.

## Architecture rules (non-negotiable)
- Clean Architecture: Api → Application → Domain. Infrastructure implements Application
  interfaces. Domain references no other project and no EF Core.
- Organize by feature (module), not by technical type.
- Business failures return `Result`/`Error`. Exceptions are only for unexpected failures.
- Money and stock are ledgers. Entries are never edited or hard-deleted; a mistake is fixed by a
  reversing entry. Balances and stock on hand are calculated, never typed.
- Rates and prices are copied onto the document when it is issued. Changing a rate card or a price
  list never changes an existing work order, payslip or sales order.
- Raw attendance punches are never edited; corrections are separate, audited records.
- A contractor can reach only his own rows. A customer can reach only his own orders.
- Never call `DateTime.Now` or `DateTime.UtcNow`; inject the clock.
- Money is never float or double.

## Workflow
- Small phases. Before a phase starts, ask its open questions (docs/ROADMAP.md) and wait for the
  answers. Never invent a business rule; the workshop's real practice decides.
- One roadmap task per session, and a new session for each task: when a task is committed, the
  session ends and the next task starts in a fresh one. Why: a long session sends its whole
  context again with every message (1.6.5 reached 360k tokens). Do not start the next task.
- For any non-trivial task, present a plan first and wait for approval.
- When a task grows while it is planned (as 1.6.5 did with the owner's numbers), split it before
  building into lettered tasks in the roadmap (1.6.5, 1.6.5b), so each diff and each review stays
  small.
- When a task is finished, move its whole roadmap entry with its «Done» report to
  docs/roadmap-done.md and leave one status line in docs/ROADMAP.md (with anything still open),
  because every session and every review reads the roadmap.
- Commit when a task is finished and its tests are green (allowed by the developer on
  1405/08/28). IMPORTANT: never push; the developer pushes from his own terminal, so tell him
  when there is something to push.
- IMPORTANT: no `Co-Authored-By`, `Claude-Session` or any other Claude or Anthropic line in a
  commit message or a pull request. The developer does not want Claude listed as a contributor on
  his GitHub repositories.
- Commit messages follow Conventional Commits, e.g. `feat(production): add work order issue`.
- Never commit secrets.
- Every task ends with passing tests, shown as evidence (the summary of `scripts/check.ps1`).
- Two project agents (`.claude/agents/`, added 1405/09/04): `checker` runs `scripts/check.ps1`
  on a cheaper model and returns only the summary and failures; `reviewer` reads the task's
  changes with a fresh context and reports broken rules (money, access, audit, architecture,
  UI). When a task's checks are green, run `reviewer` before committing and fix or answer each
  finding. Why: the session that wrote the code shares its own assumptions, and in a money
  system one broken rule costs more than many visible bugs. Its report is not proof.
- Before running `reviewer`, ask the developer, naming the model chosen and why: `sonnet` for an
  ordinary task, `opus` for one that touches money, access, login or the audit, where a missed
  rule costs most. The session chooses; the developer only says yes or no. Pass the model with
  the call (`model`); `reviewer.md` keeps `inherit`, so a forgotten model falls back to the
  session's own, never a weaker one. Hand it a brief: the task number, the risky files, and the
  ADR and docs/ARCHITECTURE.md sections to read (added 1405/09/05, task 1.6.5b: the review of
  1.6.5 alone took about 158k tokens).

## Commands
Run from the repository root. Keep this section up to date when commands change.
- Check everything CI checks: `./scripts/check.ps1` (`-Only dotnet` or `-Only panel`; the panel
  includes the screenshots). One line per step with the test counts, and for a failure only its errors, grouped; full logs in
  `artifacts/check/` (ADR 0017). Use it instead of the separate build, test, lint and format
  checks below, so long output stays out of the conversation; read a log only when the summary
  is not enough. It sets up the panel's `subst` drive itself; Docker must be running
- Build: `dotnet build Yalfan.slnx` (warnings are errors; the analyzers and code style rules
  run in it, so it is also the .NET lint; exceptions live in `.editorconfig`, ADR 0015)
- Format C#: `dotnet format whitespace Yalfan.slnx` fixes; CI runs it with
  `--verify-no-changes`. Run it before every commit
- Test: `dotnet test --solution Yalfan.slnx` (Microsoft.Testing.Platform; the old
  `dotnet test Yalfan.slnx` form fails). Docker must be running: the integration tests start
  PostgreSQL with Testcontainers
- Login in Development: no SMS is sent; the code is shown at the top of the login page, and
  is also in the API log and Seq (ADR 0012).
  `.env` needs `JWT_SIGNING_KEY` and `IBAN_ENCRYPTION_KEY`; from the host, `Jwt:SigningKey` and
  `Encryption:IbanKey` come from user-secrets (the API does not start without either; ADR 0019)
- Run the stack in Docker: `docker compose up --build --wait` (needs `.env`, copied from
  `.env.example`); health at `http://localhost:8080/health`; stop with `docker compose down`
- Logs: console; in Development also Seq at `http://localhost:5342` (compose service `seq`)
- Run the API from the host: `docker compose up db seq --wait`, then
  `dotnet run --project src/Yalfan.Api` (`http://localhost:5080`; the connection string comes
  from `dotnet user-secrets`, see README)
- Add a migration: `dotnet ef migrations add <Name> --project src/Yalfan.Infrastructure
  --startup-project src/Yalfan.Api --output-dir Persistence/Migrations` (`dotnet tool restore`
  once). Development applies pending migrations when the API starts
- Database: PostgreSQL on `127.0.0.1:5433`, database and user `yalfan`
- Add a package: `dotnet add <project> package <name>`, then check `Directory.Packages.props`:
  Microsoft packages must stay on `10.0.x` (the command offers 11.x by default). The command
  also strips the blank lines from that file; restore them
- Panel (from `web/panel`, Node 24): `npm ci`, `npm run dev` (`http://localhost:5173`, forwards
  `/panel/*` to the API on 5080; `YALFAN_API_URL` overrides), `npm test`, `npm run build`,
  `npm run lint` (ESLint, warnings fail it), `npm run format` (Prettier; CI runs
  `npm run format:check`). Run format and lint before every commit (ADR 0015).
  Vite cannot run from a path containing `#` (ADR 0014): use a `subst` drive until the checkout
  moves
- Panel screenshots (from `web/panel`; ADR 0028): `npm run screens` compares the main pages at
  desktop and phone width with the pictures in `screens/__screenshots__`, inside Playwright's
  pinned Docker image (Docker must be running; `check.ps1` runs it too). A failure leaves the
  expected, actual and diff pictures in `artifacts/check/screens`. When a page changes on
  purpose, `npm run screens:update`, look at the new pictures and commit them with the change.
  A page that calls a new API route needs its answer in `screens/api.ts`
- Panel API types: generated from `web/panel/openapi/panel.json` before every dev, build and
  test run. When `PanelDocumentTests` fails, it has rewritten that file from the API: review and
  commit it
- Add a panel package: `npm install --before=<date two weeks ago> <name>` (ADR 0014), never a
  version published in the last days
