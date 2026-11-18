# ADR 0013 — Audit: every change recorded in the same save, append-only

- Status: accepted
- Date: 2026-11-19 (1405/08/28)
- Written in: roadmap task 0.6

## Context

Money, work orders and contractor details will be changed by three people in the office. When a
balance or an IBAN looks wrong, the owner must be able to see who changed what and when, with the
old and new values (ARCHITECTURE.md 12). The record must not depend on each developer remembering
to write it, and it must not be erasable by a bug or a hand-written query.

## Decision

### An EF Core interceptor writes the audit inside the same save

`AuditInterceptor` (a `SaveChangesInterceptor`, one per DbContext) reads every added, changed and
deleted row before the save and adds one `AuditEntry` per row to the **same** `SaveChanges`.

- Why the same save: a change and its audit entry are stored together or not at all; there is no
  change without history and no history of a change that never happened.
- Why an interceptor: every write that goes through EF is recorded, whoever makes it (handler,
  start-up seeding, a later job), with nothing to remember.
- A failed save detaches the entries it created, so a retry does not store them twice.

### What an entry holds (`audit_entries`)

| Column | Meaning |
|---|---|
| `occurred_at` | From `IClock`, UTC |
| `user_id`, `actor` | The logged-in user (`user`), or nobody (`system`: start-up, jobs) |
| `trace_id` | The W3C trace id of the request, the same `TraceId` the logs carry, to jump to the request in Seq |
| `entity_type`, `entity_id` | Table name (`users`) and key |
| `action` | `Created`, `Updated`, `Deleted` |
| `changes` | jsonb: `{"isActive": {"old": true, "new": false}}` |

- Updated: only fields whose value really changed; a save with no real change writes nothing.
  Created: only `new`; deleted: only `old`.
- Values are the stored form (`"09121234567"`, `"Owner"`), not C# objects. Field names are
  camelCase, like the API's JSON, so a history screen can reuse the form labels.
- The key (already `entity_id`) and the row version (`xmin`) are left out.
- jsonb keeps its own key order (`new` before `old`); readers must not depend on order.
- No foreign key to `users`: writing the audit must never fail on a reference.

### What is left out or hidden

Marked in the EF configuration, not in the Domain, which knows nothing about auditing:

- `builder.ExcludeFromAudit()`: no entries. Used only for rows that are themselves security
  records written on every login or renewal (`otp_codes`, `refresh_tokens`); auditing them
  would multiply the table and copy their hashes. Never for business data.
- `.IsAuditRedacted()` on a property: the entry says the field changed (`"redacted": true`) with
  no values. For secrets whose change must be noticed but whose value must not be copied, such
  as a contractor's IBAN (1.3): swapping an IBAN is the usual payout fraud.

### Append-only, in code and in the database

- The interceptor throws if an `AuditEntry` is changed or deleted.
- A trigger refuses `UPDATE`, `DELETE` and `TRUNCATE` on `audit_entries`. Why: the same rule as
  the ledgers; a bug or a hand-written query cannot erase history. Dropping the trigger is a
  deliberate, visible act. A test-data reset (0.7) therefore creates a fresh database instead of
  deleting rows.

### Writes that skip the change tracker are a build error

`ExecuteUpdate`, `ExecuteDelete` and `Database.ExecuteSql…` go straight to the database and the
interceptor never sees them, so all their overloads are in `BannedSymbols.txt` (exact signatures:
the analyzer does not match a name without its parameter list). Reading with raw SQL
(`FromSql`) is still allowed. Why a build error: a written rule is forgotten at 2 a.m.; a build
error is not (ADR 0005). If a bulk change is ever truly needed, it is a reviewed exception that
writes its own audit entry.

`User.Deactivate()` was added so tests change users through the Domain instead of raw SQL; the
owner's users screen (1.6) uses it too.

### Not in this task

- Viewing the history in the panel: added to task 1.6 (owner only), where the first screens
  with something to show are built.
- IP address and browser: the trace id already links each entry to the full request log.

## Consequences

- Every new entity is audited by default; leaving one out or hiding a field is a visible line in
  its configuration.
- The audit grows with every change and is never pruned; for one workshop that is small. If it
  ever matters, old entries are archived, not deleted (decided then, with the trigger in view).
- Owned types and complex properties are not handled yet; the first entity that uses one
  extends the interceptor.
