# Yalfan — Architecture

Status: **proposed** on 1405/08/24 (2026-11-15). Nothing is built yet. Every decision here becomes
an ADR in `docs/adr/` when the phase that needs it starts; until then it can still change.

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
│   │   ├── Payments/
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
│   └── Yalfan.Api.IntegrationTests/
├── docs/                         ROADMAP, ARCHITECTURE, BUSINESS_RULES, adr/
├── deploy/
├── docker-compose.yml
├── global.json
├── Directory.Build.props
└── Directory.Packages.props
```

## 5. Layer rules

| Layer | Holds | May reference | Must not |
|---|---|---|---|
| **Domain** | Entities, value objects, invariants, domain errors | Nothing | Know about EF, HTTP, time zones, languages |
| **Application** | One folder per use case; validation; permissions; transactions | Domain | Reference Infrastructure or any provider package |
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
| **Identity** | User, Role, Permission, OtpCode | Staff log in with password; contractors and store customers with phone + SMS code |
| **Catalog** | Product (model), Variant (colour → SKU), Category, ProductTranslation, PriceList, ProductImage | One catalog feeds production, stock and the store |
| **Parties** | Contractor, Worker, Customer, Supplier | Every party that can owe or be owed has a ledger |
| **Production** | Stage, ProductRouting, ProductionOrder, WorkOrder, WorkOrderDelivery, QcResult, RateCard | The heart of the system. See 7.1 |
| **Inventory** | Material, Bom, Warehouse, StockMovement, StockCount | Stock is a ledger of movements. See 7.3 |
| **Ledger** | LedgerEntry, CashAccount, Payment, Cheque, Expense | All money. See 7.2 |
| **Attendance** | Device, Punch, Shift, AttendanceDay, Leave | Live board. See 8 |
| **Payroll** | PayPeriod, Payslip, Advance | Reads Attendance and Production, writes to Ledger |
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
- Amounts are whole numbers with a currency code. Toman or Rial as the stored unit is ADR 0002.

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
| `web/contractor` | Same stack, built as a phone-first PWA | Simple, big buttons, few screens. Login by phone + SMS code, no password |
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

- **Auth:** ASP.NET Identity + JWT. Roles: Owner, Manager, Operator, Contractor, RetailCustomer,
  WholesaleCustomer. Fine-grained permissions on top; a contractor's token can reach only
  `/contractor/*` and only that contractor's rows.
- **Audit:** every create, change and reversal records user, time, old and new value.
- **Time:** `IClock` everywhere; business "today" is Asia/Tehran.
- **Validation:** FluentValidation in Application; invariants that must never break are also
  enforced by database constraints.
- **Jobs:** Hangfire on PostgreSQL (SMS sending, cheque due reminders, nightly attendance close).
- **Logging:** Serilog, structured; Seq in development.
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
