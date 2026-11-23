# ADR 0021 — Piece rates: an append-only rate card per model and stage

- Status: accepted
- Date: 2026-11-23 (1405/09/02)
- Written in: roadmap task 1.5

## Context

Contractor pay is *stage × accepted quantity × rate* (ARCHITECTURE.md 7.1), and a work order
copies the rate in force on the day it is issued (task 2.2, ADR 0003 to be written there). So the
rates must exist, with their history, before the first work order. The roadmap's check for this
task: the rate valid on a given date can be queried, and old rates are never overwritten.

The developer answered the questions for 1.5 on 1405/09/02 (roadmap Phase 1, "Answered … for
1.5"); the proposals he did not change were accepted as written:

- **In-house workers are paid a day wage; outside contractors are paid by the piece**
  (کنترات, by the number of pieces). So the rate card is the contractors' piece rates only.
- A rate belongs to a **model and a stage**, the same for every fabric, colour and size of the
  model, and **the same for every contractor**.
- The **start date may be in the past or the future**.
- A **mistyped rate is corrected by entering the right one with the same start date**; the
  later entry wins and the wrong one stays in the history.
- A rate is **per piece, in Rial, a whole number above zero**; a stage that is not paid has no
  rate.
- **Every staff role** may enter rates, as roadmap 1.6 expects the operator to.

## Decision

### What is stored

```
PieceRate: model, stage, amount (Money, Rial), starts on (date), entered at (instant)
```

- **One table, `piece_rates`, of entries, not a "current rate" column plus a history table.**
  Why: the rate on any day, past or future, is then one rule over one list, and nothing has to
  be moved from "current" to "history" when a rate changes, which is where a history usually
  loses rows.
- **Called `PieceRate`, not `RateCard`** as ARCHITECTURE.md first named it, because in-house
  workers are on a day wage (5.1, per worker), and the name should say this is pay per piece.
  `RateCard` is the Domain's rule over the entries.
- **No contractor, fabric, colour or size column.** If a size or a contractor is ever paid
  differently, an optional column is added whose empty value means "all", so the entries typed
  until then keep their meaning and no row changes.
- **The amount is `Money`**, the first money in the database (ADR 0002, point 8): `amount`
  bigint and `currency` (`IRR`), as complex-property columns of the row. The Domain refuses
  another currency with an exception (a bug: the API takes Rial only) and an amount of zero or
  less with `production.rate_not_positive`; the database repeats the second as a check
  constraint.
- **The start date is a `date`** (ADR 0005), a day of the workshop, not an instant.
- **`entered_at`** comes from `IClock` and orders entries with the same start date.

### Which rate is in force

For one model and stage on a day: of the entries whose start date has come, the one with the
**latest start date**; of several with that start date, the one **entered last** (the id breaks
an exact tie, so the order never changes between reads). An entry with the same start date as a
later one is **replaced**; an entry followed by a later start date is simply past.

- **Why the start date decides and not the typing order:** "from 1 December 120" typed before
  "from 15 November 110" still starts on 1 December. Cancelling a planned raise is done by
  entering the old amount again with the raise's start date.
- **Why "replaced" is worked out and not stored:** storing it would mean updating the old row,
  and the table is append-only. The history shows the flag from the same rule.
- The rule runs in the Domain (`RateCard`) over the entries of one model, which are a few per
  stage, not in SQL, so it is unit-tested without a database.

### Append-only, in the database too

The migration adds `reject_append_only_change()`, a trigger function that refuses UPDATE, DELETE
and TRUNCATE on `piece_rates` with "piece_rates is append-only". Why: the roadmap's check is that
an old rate is never overwritten, and a trigger makes that true even for a bug or a hand-written
query, as for `audit_entries` (ADR 0013). The function names the table in its message, so the
ledger (2.4) and stock movements (6.2) can reuse it. In the Domain every property is init-only
and there is no method that changes an entry. Foreign keys to `products` and `stages` are
`RESTRICT`. Each entry is audited when it is created; the audit now records the parts of a
complex property (ADR 0013, update of 1405/09/02).

### The API (`/panel/production/rates`, every staff role)

- `POST /`: model, stage, amount in Rial, start date. Returns the entry.
- `GET /?productId=…&on=…`: the model's rate card on a day, `on` defaulting to the workshop's
  today (Tehran). Every stage in the order of the work, each with the amount and start date in
  force, or none, so the screen (1.6) shows which stages still have no rate. This is the read a
  work order will make when it is issued (2.2).
- `GET /history?productId=…&stageId=…`: every entry of the model, or of one stage, grouped by
  stage in the order of the work, newest first, with `isReplaced`. No paging: a model has a few
  entries per stage a year.
- Error codes: `production.product_not_found`, `production.stage_not_found`,
  `production.rate_not_positive`; a missing or non-positive field is the generic
  `validation.required` or `validation.too_small` on that field. The product code is the
  module's own, like `parties.stage_not_found`, because each module answers in its own codes.
- Amounts are JSON numbers in Rial (ADR 0014). The screen shows them with the Toman line of
  ADR 0002, point 9.

## Not decided here

- **What a work order does when its stage has no rate** on the issue day: refuse, or let the
  operator type one. Decided in 2.2 with the snapshot.
- **Whether a cutting rate makes sense.** Cutting is in-house only and in-house work is paid by
  the day, so a cutting rate would never be used; the rate card does not refuse it, because no
  stage is marked "in-house only" yet (ADR 0018 leaves that to 2.2).
- **A model that stops being given out at a stage.** There is no end date: the model is
  deactivated (1.6), or simply not issued.

## Consequences

- ARCHITECTURE.md names the Production entity `PieceRate`.
- 2.2 reads the rate card on the issue day and copies the amount onto the work order; changing
  the rate card afterwards cannot reach the work order, because the entry it copied from never
  changes either.
- 5.1 holds each in-house worker's day wage with its own history; it does not use this table.
