# ADR 0027 — The change history: the owner reads the audit of a model, a contractor, a worker

- Status: accepted
- Date: 2026-11-26 (1405/09/05)
- Written in: roadmap task 1.6.6 (the last part of 1.6)

## Context

Every change is already recorded, in the same save, with who, when and the old and new values
(ADR 0013). What was missing is a way for the owner to read it: when a rate, a contractor's
mobile or an IBAN looks wrong, he must see who changed it and when without asking the developer
to query the database. Task 1.6 promised this on 1405/08/28; on 1405/09/05 it was split from
1.6.5 into its own task, and its shape was accepted with 1.6.5's questions: a «تاریخچه» of a model
(with its bags, sizes and rates), a contractor and a worker (with their stages, an IBAN as
changed with no number), for the owner, on a page of its own, newest first, one block per save
with who and when, each change in the workshop's words. The plan below was accepted on
1405/09/05 as written, the proposal on the IBAN included.

## Decision

### What the owner sees

- **A «تاریخچه» link** at the top of a model's, contractor's and worker's page, opposite the back
  link, for the owner alone. The page (`/models/:id/history`, `/contractors/…`, `/workers/…`)
  shows «not found» to anyone else, as the users screen does, and the API refuses them too.
- **One card per save**, newest first: who saved it (the user's name; «سیستم» for start-up and
  jobs), the Jalali date and the time in Tehran, then one line per change: «نام: کیف دستی ← کیف
  دستی چرمی», «کیف ۱۰۱-۳ غیرفعال شد», «سایز «بزرگ» اضافه شد», «نرخ برش از ۱ دی ۱۴۰۵» with the
  amount in Rial and its words under it (ADR 0002 point 9), «مرحلهٔ «دوخت» برداشته شد». What a
  new model or person was made with is listed under one line («مدل ساخته شد»).
- **Sizes dragged into a new order** are one line («ترتیب سایزها عوض شد»), not one per size.
- **More than six bag lines in one save fold** under one line («۲۴ کیف اضافه شد») that opens on a
  tap, because creating a model can make up to 500 bags in one save. Why six: a few bags read
  better as lines of their own.
- **Codes and mobiles stay left to right** inside the Persian lines (Unicode isolates), as the
  model's and the lists' pages show them; otherwise «۰۹۱۲ ۱۱۱ ۲۲۳۳» reads «۲۲۳۳ ۱۱۱ ۰۹۱۲». The
  user's name is isolated too, so a Latin name does not pull the date around it.
- **Twenty saves a page**, with «قدیمی‌ترها» for the next twenty. Why: the audit is never pruned
  (ADR 0013), so a model's history only grows.

### The API

- `GET /panel/history/models/{id}`, `…/contractors/{id}`, `…/workers/{id}`, with `?before=` for the
  next page. **Owner only, on the whole group** (`/panel/history`), so a route added there later
  cannot be left open by forgetting it. Read only.
- **The answer is data, the words are the panel's:** `saves` (`at`, `by`, `changes`), each change
  a `part` (model, size, bag, rate, contractor, worker, stage), an `action` (created, updated,
  deleted), a `label` and `fields` (`name`, `old`, `new`), with `before` for the next page. Every
  frontend maps codes to its own language (CLAUDE.md); the parts, actions and field names are
  enums, so the panel's labels are checked by the compiler.
- **Names instead of ids:** the user, the stage, the category, fabric, colour and size, the bag's
  code. They are **today's names**: when a name itself changed, that change is a line of its own
  with the old name, and nothing is deleted, so every id still has a name.
- **Fields in a fixed order** (name, description, category; full name, mobile, notes, start date,
  IBAN), not the audit's: jsonb keeps its keys in its own order (ADR 0013). The parts of one save
  in the order the save gave them when it gave one (the sizes of a new model as they were typed,
  even after being dragged), else today's.
- **Left out** because they mean nothing to the owner: keys and parent ids, the moment of entry
  (the card has it), the currency (always Rial), the model's number (on its page), and on a new
  part what every new part has (on, last place, no notes, no IBAN).

### Which audit entries belong to a record

- A model: its row and its Persian translation, its sizes and their names, its bags and its rates.
  A person: the row and a stage row per stage. **All by exact key**, joined with `UNION ALL`, so
  each part uses the existing `(entity_type, entity_id, occurred_at)` index and **no migration**
  is needed. This works because sizes, bags, rates and stages are never deleted, so today's rows
  are every row the record ever had; a stage unticked later has no row today, so every stage is
  asked for (there are only a few).
- Only the Persian translation is read: the panel is Persian, and the store's other languages join
  the history when they get a screen (Phase 11).
- **A save is every entry with the same moment, user and request.** The interceptor reads the
  clock once per save, so its entries share the moment exactly; a page is the twenty newest
  moments, and `before` is the oldest moment shown. The tests' clock stands still, so they move
  it on between changes, as real saves would be.

### The vocabulary is checked against the audit

Each audited table names, for every field the audit records, the history field it becomes or
that it is left out (`HistoryVocabulary`). A field the vocabulary does not know is left out, and a
test creates one row of each kind and fails if the audit recorded a field the vocabulary does not
name. Why: a column added later (a worker's pay terms in Phase 5) is then a decision, not a
silent gap in the owner's history.

The tables are named by constants (`AuditedTables`) that the EF configurations map with as
well, so the history and the database read one name and cannot drift apart.

### Read only, in code

The use cases read the audit through `IQueryable<AuditEntry>`, not a `DbSet`, without tracking, so
no use case can add or change an entry (the interceptor alone writes it; the trigger refuses any
change).

### An IBAN: set, replaced or removed (changes ADR 0013)

A redacted field now also records **whether there was a value** before and after (`oldSet`,
`newSet`), never the value. The history then says «شبا ثبت شد», «شبا عوض شد» or «شبا پاک شد»,
and a contractor added with an IBAN shows «شبا ثبت شد» in his first card. Why: replacing an IBAN
that was already there is the usual payout fraud the owner must notice, and «عوض شد» alone hides
it among first entries and removals. Whether there is an IBAN is no secret: everyone in the panel
sees its last four digits. Entries written before this carry no flags and read «شبا عوض شد»;
nothing is in production yet.

### Not in this task

- The history of the catalog lists, the stages and the users: the roadmap asks for models,
  contractors and workers. The API shape takes another record with one more handler.
- Who saw a whole IBAN: that is a read, logged (ADR 0019), not a change.

## Consequences

- A new audited column of these tables fails the vocabulary test until it is named or left out.
- A new record with a history needs a handler that lists its rows and their names; the reader,
  the assembler and the panel's page are shared.
- The panel's words for a change live in `historyLines.ts` and `fa.ts`, tested without the API.
- The history shows names as they are today; the old name of something renamed is in that
  rename's own line, not in every older line.
