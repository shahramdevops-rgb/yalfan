# ADR 0012 — Staff log in with an SMS code; JWT access token and rotating refresh cookie

- Status: accepted
- Date: 2026-11-18 (1405/08/27)
- Written in: roadmap task 0.5

## Context

Three staff use the panel: the owner, the workshop manager and one office operator, none of them
technical. Contractors (Phase 10) and store customers (Phase 12) were already planned to log in
with their mobile and an SMS code. The roadmap first planned passwords for staff (ASP.NET Identity,
password policy, lockout). The developer asked whether a password is needed at all.

## Decision

### No passwords: every staff login is a one-time SMS code

Decided by the developer on 1405/08/27.

- **Why:** one login mechanism for staff, contractors and customers is built, tested and secured
  once instead of twice; nobody forgets a password, so there is no reset flow and no "the owner
  forgot his password and only the developer can fix it in the database"; a non-technical user
  has nothing to remember or write down.
- **Cost accepted:** the panel needs a working SMS provider before go-live 1. Iranian providers
  send login codes from a *service line* (خط خدماتی) with registered templates. The developer
  will obtain the service line early, before the store exists (owner's track O.11, now needed by
  Phase 3), and the provider adapter (old task 10.1) moves to Phase 3.
- **Risk accepted:** whoever holds the phone or its SIM can log in. For a workshop panel this is
  no worse than a password. Payout approval (Phase 15) has its own one-time code; a password as a
  second factor for the owner can be added there if wanted.
- If SMS is down, devices with an open session keep working (sessions last 7 days); only new
  logins wait.

### Login code rules

| Rule | Value | Why |
|---|---|---|
| Code | 6 random digits (`RandomNumberGenerator`) | With 5 tries, one chance in 200,000 per code |
| Lifetime | 2 minutes | Enough to read an SMS; useless when stolen later |
| Wrong tries per code | 5, then the code is dead | Replaces password lockout |
| Wait between codes | 2 minutes per number | The panel shows a countdown |
| Codes per hour | 5 per number | Stops a script from sending SMS at the workshop's cost |
| Storage | SHA-256 of user id + code | A backup or table dump never holds a working code |
| Only the newest code counts | asking again retires older codes | One code to think about |
| Single use | the code's row version (PostgreSQL `xmin`) | Two requests with the same code cannot both log in |

- **Same answer for every number.** `POST /panel/auth/otp` answers the same countdown for an
  unknown number, an inactive user, and a throttled request, and sends no SMS in those cases;
  a wrong code and an unknown number both give `identity.otp_invalid`. Why: the endpoint must
  not reveal which numbers belong to staff, and must not spend SMS on strangers.
- Numbers and codes are accepted with Persian or Arabic digits, spaces and dashes; numbers in
  any common form (`+98…`, `0098…`, `9…`). `MobileNumber` (Domain) keeps `09XXXXXXXXX`. Only
  Iranian mobiles, because the code goes out through an Iranian line.

### Sessions

- **Access token:** JWT, HMAC-SHA256, 15 minutes, claims `sub`, `name`, `role`. Short, so that a
  deactivated user or a changed role takes effect within minutes without a revocation list.
  The signing key (at least 32 bytes) comes from user-secrets or `Jwt__SigningKey`; without it
  the API does not start.
- **Refresh token:** 256 random bits, only its SHA-256 stored, 7 days from the last renewal.
  Every renewal rotates it. A token used again after it was replaced means it was copied: the
  whole family (one login) is revoked, logging out the thief and the owner.
- **Two tabs:** a replaced token presented within 30 seconds answers
  `identity.refresh_superseded` without revoking anything; the panel retries once with the
  cookie the other tab received. Why: two tabs renewing together is normal; ending the session
  for it would look like a random logout.
- **The refresh token travels only in a cookie:** `yalfan_refresh`, HttpOnly, Secure,
  SameSite=Strict, Path `/panel/auth`. Why: the panel's JavaScript cannot read an HttpOnly
  cookie, so an XSS bug cannot steal a seven-day session; SameSite=Strict keeps other sites from
  using it; the path keeps it off every other request. The access token is in the response body
  and kept in memory by the panel. CORS for the panel origin is set up in 0.8.
- Logout revokes the family and deletes the cookie.

### Users and roles

- **Own `User` entity in Domain, not ASP.NET Identity.** Without passwords, Identity is mostly
  password hashing, lockout and external-login machinery we would not use, plus seven tables and
  an `IdentityDbContext` base class. `User` (name, mobile, role, active, created) is a few lines
  with its rules where the Domain can test them. The roadmap and ARCHITECTURE.md said "Identity"
  when passwords were planned.
- One role per user: Owner, Manager, Operator. Contractor and customer roles come with their
  phases. Stored as text in the database and sent as camelCase names in JSON (`"operator"`),
  never as numbers, so reordering an enum never changes meaning; this holds for every enum in
  the API.
- **Only the owner adds users, and only managers and operators** (decided 1405/08/27). A second
  owner would hold everything the owner holds, including payout approval later, so that stays his
  decision outside the panel. Whether the manager may add operators is open; until the owner says
  so, the smallest power is the default.
- **The first owner** is created at start from `Identity:SeedOwner` (name and mobile, no secret)
  when no owner exists. A configured but invalid owner stops the start.
- User list, deactivation and changing a user's number come with the panel's users screen (1.6).

### Access rules in the API

- **Closed by default:** the fallback policy needs a logged-in user, so an endpoint someone forgot
  to protect is not public. Public endpoints say `AllowAnonymous()`. A side effect: an anonymous
  request to a route that does not exist gets 401, not 404, so outsiders cannot map routes.
- `/panel/*` needs a staff role (`Policies.Staff`), so a contractor's or customer's token never
  reaches the panel. `/panel/users` needs the owner.
- Role checks sit on the endpoints for now; finer permissions move into the Application pipeline
  (ADR 0011) when the first module needs them.
- New error kind `Unauthenticated` → 401, for a wrong code or a dead session.

### SMS provider

- `ISmsSender` (Application) takes a template name and values, the way Iranian providers send
  service messages. Until the real adapter exists, `LoggingSmsSender` writes the message, code
  included, to the log (console and Seq).
- `Sms:Provider` has no default. The log provider is refused outside Development and Testing,
  because a login code in a production log lets anyone who reads the log log in.

### Tests

The auth tests run against a real PostgreSQL started by Testcontainers (part of 0.7 brought
forward, because login cannot be proven without a database): one container per test run, one
database per test class. A fake clock moves time (code expiry, the 7-day session, the 15-minute
token) and a fake SMS sender hands the code to the test. Docker must be running for the tests.

## Consequences

- Go-live 1 depends on the SMS service line and its adapter (roadmap Phase 3).
- Task 10.2 (contractor OTP login) reuses this flow; it adds the contractor role and the
  `/contractor` routes.
- Application now references `Microsoft.EntityFrameworkCore` (the provider-neutral core) for
  `IAppDbContext`; the PostgreSQL provider stays in Infrastructure.
- A forgotten password does not exist; a lost phone is handled by the owner changing the user's
  number (1.6).
