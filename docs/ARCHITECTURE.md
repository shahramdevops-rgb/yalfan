# Yalfan — Architecture

Status: **proposed** on 1405/08/24 (2026-11-15). Only the solution skeleton, the database wiring,
the common building blocks, the request pipeline and staff login exist (tasks 0.1 to 0.5; ADRs
0001, 0002, 0005, 0011, 0012). Every decision here becomes an ADR in `docs/adr/` when the phase that needs it
starts; until then it can still change.

## 1. What the system is

Yalfan is a bag workshop in Hamedan: about 20 workers inside the workshop and about 40 outside
contractors paid by the piece. Today everything is on paper.

The system is one product with three faces:

| Face | Who uses it | Language | Purpose |
|---|---|---|---|
| **Panel** | Owner, workshop manager, one office operator | Persian only | Production, contractors, workers, attendance, stock, money, reports |
| **Contractor panel** | The 40 contractors, on their phones | Persian only | Their own work orders, deliveries and balance. Nothing else |
| **Store** | Retail buyers, wholesale buyers, foreign buyers | Persian, Arabic, English | Catalog, retail orders, wholesale orders, custom production requests |

The one goal everything serves: at any moment the owner knows what was made, who made it, what it
cost, what was sold, and what profit is left.

### Out of scope (decided)
- **Official accounting and tax** (Moadian, legal ledgers). The workshop issues no official
  invoices. The panel is the only system; Holoo is not used and not integrated.
- **International online payment.** Sanctions make it impossible. Foreign buyers request a
  proforma invoice and settle outside the site.
- **Native mobile apps.** Phones use the web panels (installable PWA).
- **The old Django site.** Its code, data and photos are dropped. Both old domains have lapsed.

## 2. Style

Modular monolith, Clean Architecture, organized by feature inside each layer. Same shape as the
Gym Management project, so its patterns and lessons carry over.

```
Api  →  Application  →  Domain
          ↑
    Infrastructure (implements Application interfaces)
```

One deployable backend, one PostgreSQL database. No microservices: one developer, about 60 staff
users and a small store do not need them, and a monolith keeps money and stock in one transaction.

## 3. The whole picture

```
 Panel (React SPA, fa)          Contractor panel (React PWA, fa)     Store (Next.js SSR, fa/ar/en)
        │                                  │                                   │
        └──────────────────────────────────┼───────────────────────────────────┘
                                           │  HTTPS, JSON, typed client from OpenAPI
 Attendance device ──push──►        ┌──────▼───────┐        ◄── future: LLM assistant
 (fingerprint/face, in workshop)    │  Yalfan.Api  │            (one more API consumer)
                                    │  + SignalR   │
                                    └──────┬───────┘
                         ┌─────────────────┼──────────────────┐
                         ▼                 ▼                  ▼
                    PostgreSQL        Hangfire jobs      Outside services
                                                         (SMS, Iranian payment gateway)
```

## 4. Solution layout

```
Yalfan/
├── src/
│   ├── Yalfan.Domain/            Entities, value objects, domain errors. No dependencies.
│   │   ├── Common/               Entity base, Result, Error, Money, Quantity
│   │   └── <Module>/             Catalog/, Production/, Ledger/, Attendance/, …
│   ├── Yalfan.Application/       Use cases. References Domain only.
│   │   ├── Common/               IAppDbContext, ICurrentUser, IClock, ISmsSender, IPaymentGateway
│   │   └── <Module>/<UseCase>/   Command or Query, Validator, Handler, Response
│   ├── Yalfan.Infrastructure/    EF Core, Identity, SMS, payment, jobs, device protocol
│   │   ├── Persistence/          AppDbContext, Configurations/, Migrations/, Interceptors/
│   │   ├── Identity/
│   │   ├── Sms/
│   │   ├── Payments/             Customer payment gateway (money coming in)
│   │   ├── Payouts/              Bank and provider adapters (money going out)
│   │   ├── Devices/              Attendance device protocol adapter
│   │   └── Jobs/
│   └── Yalfan.Api/               Program.cs, Endpoints/, Hubs/, auth setup
│       ├── Endpoints/Panel/      Staff endpoints
│       ├── Endpoints/Contractor/ Contractor-only endpoints
│       ├── Endpoints/Store/      Public and customer endpoints
│       ├── Endpoints/Device/     Attendance device ingest
│       └── Hubs/                 SignalR: live workshop board
├── web/
│   ├── panel/                    React + Vite, Persian, staff
│   ├── contractor/               React + Vite PWA, Persian, phone-first
│   ├── store/                    Next.js, fa/ar/en, server-rendered for search engines
│   └── shared/                   Generated API types, money and date formatting
├── tests/
│   ├── Yalfan.Domain.Tests/
│   └── Yalfan.Api.IntegrationTests/  In-memory API on a real PostgreSQL (Testcontainers)
├── docs/                         ROADMAP, ARCHITECTURE, BUSINESS_RULES, adr/
├── deploy/
├── docker-compose.yml
├── Yalfan.slnx
├── global.json
├── Directory.Build.props
└── Directory.Packages.props
```

## 5. Layer rules

| Layer | Holds | May reference | Must not |
|---|---|---|---|
| **Domain** | Entities, value objects, invariants, domain errors | Nothing | Know about EF, HTTP, time zones, languages |
| **Application** | One folder per use case; validation; permissions; transactions | Domain; EF Core core (for `IAppDbContext`) | Reference Infrastructure or any provider package (Npgsql, SMS, bank) |
| **Infrastructure** | EF Core, Identity, SMS, gateway, device adapter, jobs | Application, Domain | Hold business rules |
| **Api** | Endpoint mapping, auth, error mapping, SignalR hubs | Application | Hold business rules or touch `DbContext` directly |

- Business failures are `Result` values with an error **code**, never exceptions and never text.
- Each layer exposes one DI extension: `AddApplication()`, `AddInfrastructure(configuration)`.
- A module may read another module's data through the shared `IAppDbContext`, but changes another
  module's data only through that module's own handlers or domain methods.

## 6. Modules

Each module is a folder with the same name in Domain, Application and Api.

| Module | Main entities | Notes |
|---|---|---|
| **Identity** | User, OtpCode, RefreshToken | Everyone logs in with mobile + SMS code, no passwords (ADR 0012). Permissions come later |
| **Catalog** | Product (model), Variant (colour → SKU), Category, ProductTranslation, PriceList, ProductImage | One catalog feeds production, stock and the store |
| **Parties** | Contractor, Worker, Customer, Supplier | Every party that can owe or be owed has a ledger |
| **Production** | Stage, ProductRouting, ProductionOrder, WorkOrder, WorkOrderDelivery, QcResult, RateCard | The heart of the system. See 7.1 |
| **Inventory** | Material, Bom, Warehouse, StockMovement, StockCount | Stock is a ledger of movements. See 7.3 |
| **Ledger** | LedgerEntry, CashAccount, Payment, Cheque, Expense | All money. See 7.2 |
| **Attendance** | Device, Punch, Shift, AttendanceDay, Leave | Live board. See 8 |
| **Payroll** | PayPeriod, Payslip, Advance | Reads Attendance and Production, writes to Ledger |
| **Payouts** | PayeeBankAccount, PayoutAccount, PayoutBatch, PayoutItem | Pays workers and contractors through a bank or provider. See 7.5 |
| **Sales** | SalesOrder, OrderLine, Proforma, Shipment, Return | One order model for every channel. See 7.4 |
| **Notifications** | SmsTemplate, SmsMessage | OTP, settlement notices, order status |
| **Reports** | Read-only queries, no entities | Dashboard, cost and profit per model, contractor statements |
| **Audit** | AuditEntry | Who changed what, when, old and new value |

## 7. Core models

### 7.1 Production and contractor work

```
ProductionOrder (model, colours, quantity, due date)
   └── WorkOrder (one stage, one contractor or one in-house worker/team)
          ├── issued:    quantity + materials handed over
          ├── delivered: quantity brought back (one or many deliveries)
          ├── QC:        accepted / rework / rejected
          └── pay line:  accepted quantity × rate snapshot  →  LedgerEntry
```

- **Wage method stays open.** Pay is always *stage × accepted quantity × rate*. A product whose
  whole bag is paid as one price is simply a product with one paid stage. So per-bag, per-stage and
  mixed pay all fit, and the real rates can be entered later.
- **Rate snapshot.** A work order copies the rate when it is issued. Changing a rate card later
  never changes old work orders. The same rule holds for material cost and selling price.
- **Rework and rejects** stay on the contractor's record: they drive the quality numbers and can
  carry a deduction.
- Stages and the route of each product are data, not code. The workshop defines its own.

### 7.2 Money

- One append-only **ledger**. Every money event is an entry against a party (contractor, worker,
  customer, supplier) and, when cash moves, against a cash or bank account.
- **Nothing is deleted or edited.** A mistake is fixed by a reversing entry. Balances and debts are
  always calculated from entries, never stored as a number someone can type over.
- **Cheques** are first-class: received or issued, due date, status (in hand, deposited, cleared,
  bounced, returned). Wholesale selling in Iran runs on them.
- **Opening balances** are ordinary entries dated on the go-live day, so paper history ends and
  system history begins at one clear point.
- Amounts are whole numbers with a currency code, in **Rial everywhere**: stored, sent by the
  API and shown on every screen and print-out. There is no Toman in the system (ADR 0002).

### 7.3 Stock

- Stock is also a ledger: `StockMovement(item, warehouse, quantity, reason, reference)`.
- A contractor is a **warehouse**. Handing 100 metres of fabric to a contractor moves it from the
  main store to that contractor's location; it leaves when finished pieces come back, when unused
  material is returned, or when waste is recorded. Missing material is then visible per contractor.
- **BOM** per product (and per colour where it differs) gives expected usage, so real usage, waste
  and the material cost of each bag can be compared.
- Finished bags are stock items too; the store sells from the same numbers.

### 7.4 Sales

- One `SalesOrder` with a **channel**: entered in the panel (in-person and phone sales, which are
  most sales today), store retail, store wholesale, custom production request.
- Price comes from a **price list**: retail, wholesale tiers with minimum quantities, and a
  currency. A foreign-language visitor sees "request a proforma" instead of "pay".
- A wholesale or custom order can create a `ProductionOrder`; that link is how the cost and real
  profit of an order are known.

### 7.5 Payouts (money going out)

Decided by the developer on 1405/08/24: paying workers and contractors must end up **through an
API, with no hand transfers**, and the design must **not depend on one bank**. Whether a bank or
provider will actually give the workshop such an API is not known yet (roadmap task 15.0), so the
module is built so that this answer changes one adapter and nothing else.

The module itself is built last (roadmap Phase 15), after the panel and the store are live and
stable. What exists from the start is only what it will stand on: IBANs on contractor and worker
profiles, payments recorded with a method and a bank reference, and settlements and payslips
that end in approved, unpaid amounts. Until Phase 15 the owner pays by hand and the operator
records it; afterwards the same payment records are created by payout items instead.

```
Payroll run / contractor settlement / advances
        |  approved amounts that are still unpaid
        v
PayoutBatch (Draft)  -- items: payee, verified IBAN, amount, idempotency key
        |  checks: IBAN verified, no duplicates, total <= payout account balance, daily ceiling
        v
Owner approves (one-time code)        the only human step
        v
IPayoutProvider.Submit(batch)   <-- adapter chosen by configuration
        v
Items: Submitted -> Settled | Failed | Returned      (status polled, never assumed)
        v
Settled  -> ledger entry + SMS to the payee
Failed   -> amount is payable again, reason shown
        v
Reconciliation against the bank statement
```

**The port.** `IPayoutProvider` lives in Application; adapters live in Infrastructure/Payouts.
Providers differ, so each adapter declares its capabilities and the workflow adapts:

| Capability | Used for | If the provider lacks it |
|---|---|---|
| `SubmitBatch` | Sending the payments | Not optional |
| `GetItemStatus` | Final result of each transfer | Statuses come from a statement import or are confirmed by hand |
| `GetBalance` | "Total must not exceed the balance" | The owner confirms the balance |
| `InquireIban` | Account holder's name for a new IBAN | The manager verifies the name by eye |
| `GetStatement` | Reconciliation | Statement file import |
| `Methods` | Which transfer kinds exist (interbank cycle, instant) | Only the cycle transfer is offered |

Two shapes of provider must both fit, so neither is assumed:
- **Bank account model:** the money leaves the workshop's own (dedicated) bank account.
- **Prefunded balance model:** the workshop first tops up a balance held by a payment provider,
  and payouts are made from that balance. Here the provider balance *is* the payout account, and
  the "top up only what this run needs" rule applies to it unchanged.

Each item records its **transfer method** and an **expected arrival time**. Cycle transfers
(Paya) arrive at the next bank cycle after a cut-off; instant transfers, where a provider offers
them, arrive in minutes and usually cost more. Cut-off times and fees are provider settings, not
constants in code. A provider's own automatic retry does not replace the idempotency rule below.

**Adapters.**
- `BankFileProvider`: writes the bank's group-transfer file; a person uploads it. Works with any
  bank from day one. It is the fallback, and the proof that the module does not need an API to be
  correct. It costs little because everything around it is shared.
- `<Bank>ApiProvider` / `<PaymentProvider>ApiProvider`: one class per bank or licensed payment
  provider, added when 15.0 finds one the workshop can sign up for. Switching banks means adding
  an adapter and changing configuration.

**Rules (non-negotiable).**
- **Human release.** Calculation and batch building are automatic; releasing money always needs
  the owner's approval with a one-time code. Why: this is the one feature where a bug or a
  break-in costs real money at once.
- **Separate payout account.** Payouts run only from a dedicated account topped up for each run,
  never from the owner's main account. Why: it caps the worst possible loss at that balance. It
  is an account, not just a card: card transfers have low ceilings and payout APIs use IBAN.
- **Idempotency.** Every item has a unique key sent to the provider, and a batch that timed out
  is re-queried before it is ever re-sent. Why: a lost connection must never pay someone twice.
- **Per-item truth.** Each item has its own status and bank reference; a batch can be partly
  settled. An API "accepted" is not "paid": interbank (Paya) transfers settle in cycles, so the
  ledger entry is posted only when the item is confirmed settled.
- **IBAN care.** IBANs are checksum-validated, stored encrypted, shown masked. A new or changed
  IBAN must be verified (holder name) and waits a holding period before it can be paid. Why:
  swapping an IBAN is the usual fraud in payout systems.
- **Prepare is not approve.** The operator may build a batch; only the owner approves it.
- **Ceilings.** A daily total limit and a per-item limit, set by the owner, checked before submit.
- **Everything audited**, and provider secrets never in the repository.

## 8. Attendance, live

Only the ~20 in-house workers clock in. Contractors do not.

```
Device (workshop LAN, mobile internet)
   │  pushes each punch over HTTP; keeps punches in its own memory while offline
   ▼
POST /device/...   ── raw Punch stored first, idempotent by (device, user, time)
   ▼
AttendanceDay recalculated (in, out, late, early leave, overtime, absent) from Shift rules
   ▼
SignalR hub  ──►  live board: who is in, who is late, how many are inside
   ▼
Payroll reads AttendanceDay at period end
```

Decisions:
- **No computer in the workshop is required.** The device must be a model that pushes to a server
  by itself. Buying criteria: network (LAN or Wi-Fi), HTTP push to a custom server address,
  offline memory, fingerprint or face, a documented protocol. The exact model is chosen in task
  4.0 after a protocol test, before any money is spent.
- **Fallback** if no such device is available: a small agent on the office laptop that reads the
  device and forwards punches. Same server endpoint, so nothing else changes.
- Raw punches are never edited. A manual correction (forgot to clock out) is a separate, audited
  record on top.
- The device adapter lives in Infrastructure behind `IAttendanceDeviceProtocol`; Attendance rules
  in Domain know nothing about the brand.
- The workshop's internet is mobile data, not fibre. Device buffering covers outages; the live
  board shows "last heard from device" so stale data is never mistaken for an empty workshop.

## 9. Frontends

| App | Stack | Why |
|---|---|---|
| `web/panel` | React, Vite, Tailwind, TanStack Query, typed `openapi-fetch` client | Same stack as the gym panel; components and habits carry over. Behind login, no SEO need |
| `web/contractor` | Same stack, built as a phone-first PWA | Simple, big buttons, few screens. Login by phone + SMS code, no password (same flow as staff) |
| `web/store` | Next.js (React), server-rendered | A public, three-language store must be indexed by search engines; a client-only SPA is weak at that |

- The panel must be **operable by a non-technical office worker**: few fields per screen, Persian
  labels in workshop words, sensible defaults, undo by reversal instead of delete, printable work
  orders and receipts because contractors are used to paper.
- Pages stay light because the workshop is on mobile internet.

## 10. Three languages

Only the **store** is multilingual: Persian, Arabic, English (English is assumed as the third
language). Panels stay Persian. The infrastructure is built once at the start; translating real
content waits until foreign selling is real.

| Concern | Rule |
|---|---|
| Product text | Names and descriptions live in `ProductTranslation(product, locale, …)`, never in a single Persian column |
| UI text | No text in components. Message files per locale |
| API errors | The API returns error codes; each frontend translates |
| Direction | fa and ar are right-to-left, en is left-to-right. Only logical CSS (`start`/`end`), no `left`/`right` |
| URLs | Locale in the path: `/fa/…`, `/ar/…`, `/en/…`, with `hreflang` links |
| Money | Amount + currency code. Price lists per currency |
| Dates | Stored in UTC, Gregorian. Jalali, Hijri or Gregorian is display only |
| Numerals | Persian, Arabic or Latin digits are display only; input is normalized |

## 11. Room for an LLM later

Nothing is built for it now. The door stays open because:
- every capability is an Application use case behind the API, with permissions checked there, so an
  assistant can only do what its user may do;
- the API is described by OpenAPI, which maps directly to tool definitions;
- Reports are read-only queries, the natural first thing to expose ("how much do we owe
  contractors this month?").

A future `Assistant` module would call existing handlers. It needs no change to the layers.

## 12. Cross-cutting rules

- **Auth (decided in task 0.5, ADR 0012):** no passwords; everyone logs in with an Iranian
  mobile and a one-time SMS code. Own `User` entity in Domain, not ASP.NET Identity. A 15-minute
  JWT access token in the body and a 7-day rotating refresh token in an HttpOnly, Secure,
  SameSite=Strict cookie; a reused refresh token revokes the whole session. Endpoints are closed
  by default. Roles: Owner, Manager, Operator now; Contractor, RetailCustomer, WholesaleCustomer
  with their phases. Fine-grained permissions on top; `/panel/*` needs a staff role, and a
  contractor's token can reach only `/contractor/*` and only that contractor's rows.
- **API enums** travel as camelCase names (`"operator"`), never numbers.
- **Audit:** every create, change and reversal records user, time, old and new value.
- **Time:** `IClock` everywhere; business "today" is Asia/Tehran. Instants are `DateTimeOffset`
  in UTC, calendar days are `DateOnly`; calling `DateTime.Now` and its relatives is a build
  error (ADR 0005).
- **Identity of rows (decided in task 0.3):** every entity has a `Guid` id of version 7,
  created in the Domain. Why: the id exists before saving, so related rows can be built in one
  transaction; version 7 is time-ordered, so the index stays compact; and ids cannot be guessed
  by counting, which matters for contractor and customer endpoints. Numbers people read (work
  order number, settlement number) are separate, sequential fields.
- **Errors:** an `Error` is a stable code (`module.reason`, lower case) and a kind (validation,
  not found, conflict, forbidden, rule). It carries no text; the frontends translate the code.
  On the wire every error is a ProblemDetails with `code` and `traceId`: validation 400 (with
  `errors: [{ field, code, args }]`), not found 404, conflict 409, forbidden 403, rule 422, any
  exception 500 `server.unexpected` with no detail (decided in task 0.4, ADR 0011).
- **Use cases (decided in task 0.4, ADR 0011):** `ICommandHandler` / `IQueryHandler`, no
  mediator library; endpoints inject the interface and get the handler wrapped in a decorator
  pipeline (validation now; permissions and transactions later), so no caller can skip it.
  Endpoints are grouped per module in an `IEndpointModule`.
- **Validation:** FluentValidation in Application, run by the pipeline before the handler.
  Built-in rules are translated to generic `validation.*` codes; a rule can name its own code.
  Invariants that must never break are also enforced by database constraints.
- **Database (decided in task 0.2):**
  - PostgreSQL 18. Tables and columns are `snake_case` (EFCore.NamingConventions), because
    PostgreSQL folds unquoted names to lower case and quoted PascalCase names make every
    hand-written query and every report painful.
  - Migrations live in `Infrastructure/Persistence/Migrations` and are created with the pinned
    local tool (`dotnet ef`, version in `dotnet-tools.json`), so everyone generates the same code.
  - `Database:MigrateOnStartup` applies pending migrations before the API accepts requests. It
    is **off by default** and on only in Development, so starting the API never changes a schema
    by surprise. How production migrates (on start, or as a deploy step after a backup) is
    decided in task 3.1.
  - `GET /health` answers 200 when the API can reach the database and 503 when it cannot. The
    compose health check and, later, the reverse proxy and the uptime monitor use it.
  - No password is in the repository, not even a development one: docker compose reads `.env`
    (ignored by git, template in `.env.example`) and the API run from the host reads the
    connection string from `dotnet user-secrets`. Why: one rule with no exceptions is easier to
    keep than "secrets are fine when they are only for development".
  - The development database listens on `127.0.0.1:5433` only: 5432 is usually taken by another
    project's PostgreSQL, and a development database has no reason to be on the network.
- **Jobs:** Hangfire on PostgreSQL (SMS sending, cheque due reminders, nightly attendance close).
- **Logging:** Serilog, structured, configured in appsettings; Seq in development
  (`http://localhost:5342`). One line per request; bodies and query strings are never logged
  (ADR 0011).
- **Testing:** domain unit tests; integration tests against a real PostgreSQL; Vitest for
  frontend logic; Playwright for the main flows.
- **Backups:** nightly database dump, copied off the server, restore tested before go-live.

## 13. Deployment (to be settled in Phase 3)

- Docker Compose on one VPS: API, PostgreSQL, the panels, a reverse proxy with HTTPS.
- The server is bought only when Phase 3 starts; until then everything runs on the developer's
  machine.
- **Open question:** a server in Iran is right for the panel, the Iranian payment gateway and SMS,
  but slow for foreign visitors; a server abroad is the reverse. The likely answer is the backend
  and panels in Iran and the store's pages served from a second place abroad. Decided by an ADR in
  Phase 11, when the store starts.
- Domains: `yalfan.ir` and `yalfan.com` are both unregistered today and must be registered in the
  owner's name.

## 14. Planned ADRs

| # | Decision | Written in |
|---|---|---|
| 0001 | Modular monolith with Clean Architecture | 0.1 |
| 0002 | Money: stored unit, currency code, no deletion, calculated balances | 0.3 / 2.4 |
| 0003 | Rates and prices are snapshotted on the document | 2.2 |
| 0004 | Stock as a movement ledger; a contractor is a warehouse | 6.2 |
| 0005 | Gregorian UTC storage, localized display | 0.3 |
| 0006 | Three-language store, Persian-only panels | 1.1 |
| 0007 | Attendance device: push protocol and chosen model | 4.0 |
| 0008 | Deployment topology and where the store is served from | 3.1 / 11.2 |
| 0009 | One sales order model for every channel | 8.2 |
| 0010 | Payouts: provider port, chosen bank or provider, safety rules | 15.0 |
| 0011 | Request pipeline: handlers, validation, error responses, logging | 0.4 (written) |
| 0012 | Staff log in with an SMS code; JWT and rotating refresh cookie | 0.5 (written) |
