# ADR 0020 — In-house workers

- Status: accepted
- Date: 2026-11-23 (1405/09/02)
- Written in: roadmap task 1.4

## Context

About 20 people work inside the workshop. They will clock in (Phase 4), be paid through the
payroll (Phase 5), see their own account on their phone (Phase 10) and, in the end, be paid to
their IBAN (Phase 15). Some of them are women; not every worker will be able to use the app.

The developer answered the questions for 1.4 on 1405/09/02 (roadmap Phase 1, "Answered … for
1.4"); the proposals he did not change were accepted as written:

- **Every worker has a mobile.** Some workers may not manage the app (for example a woman who
  cannot read well enough); their account is then handled by hand in the office (Phase 10).
- **The IBAN is optional for now.** When payouts start (Phase 15) it must be verified against the
  holder's national ID.
- The profile is **full name, mobile, stages, start date, optional IBAN, optional notes**. No
  national ID or insurance number yet.
- The **job is the stages** the worker does, from the shared list, **none or more**.
- The **start date is required** and may not be after today. No leaving date yet.
- As for contractors (ADR 0019): names may repeat, the mobile is unique, every staff role may set
  the IBAN, only the owner sees the whole number, no delete.

## Decision

### The profile

- **`Worker` in the Parties module**, next to `Contractor`, with its stages in `worker_stages`
  (worker id, stage id) and foreign keys to `stages`, for the same reason as contractors: the
  database refuses a stage that does not exist, and "which workers cut" is an ordinary join.
- **Not a shared base class with `Contractor`.** Only the name and notes checks are shared
  (`PartyProfile`), so both forms refuse the same input with the same codes, and the stage lookups
  and the search box (`PartyStages`, `PartySearch`). Why: from Phase 4 a worker gets a device
  number, shifts and pay terms that a contractor never has, and a contractor gets rates and
  settlements a worker never has; a common base would have to be split again.
- **The job is the stages, none or more.** Why stages and not a free-text job title: in-house work
  is recorded by stage with the contractors' screens (5.2), and a title cannot be filtered. Why
  none is allowed, unlike a contractor: someone like a storekeeper or a supervisor does no
  production stage; the notes say what that person does. If a job title is wanted later, it is
  one optional column.
- **The start date** is a `DateOnly` (`date` in PostgreSQL, ADR 0005), required because payroll
  and the length of service (Phase 5) count from it. A day after the workshop's today
  (`IClock.Today`, Tehran) is refused with `parties.start_date_in_future`, because it is almost
  always a mistyped year. The validator checks it, so the form shows it on the date field, and so
  does the Domain, which receives today from the use case (ADR 0005, point 5). When the exact day
  of an old worker is not known, the operator types an approximate one and says so in the notes.
- **No leaving date.** A worker who leaves is deactivated. Why: the rules of a final settlement
  are not known yet; if 5.5 needs the date, it is one column.
- **No national ID, insurance number or device number yet.** Nothing uses them before Phase 4, 5
  and 15, and personal data that is not needed is not kept. The device number comes with the
  device (4.1); the national ID with the IBAN verification (15.1); insurance if Phase 5 puts
  legal deductions in scope.

### The mobile

- **Required, Iranian, unique among workers**, because it will be the worker's login in Phase 10
  and the unique index catches the same worker entered twice. Error `parties.mobile_taken`, the
  same code as for contractors.
- **Not unique across workers, contractors and staff users.** One person may be in two lists: a
  manager on the payroll who also logs into the panel, or a worker who also takes piecework home.
  How one login shows both is decided in Phase 10.

### The IBAN

Exactly as for contractors (ADR 0019): the `Iban` property is enough for the encryption and the
audit's redaction, which apply to every `Iban` property by type; the profile request has no IBAN;
`PUT` and `DELETE …/workers/{id}/iban` set and remove it; `GET …/workers/{id}/iban` returns the
whole number to the owner only and writes a log line. 15.1 adds the verification against the
holder's national ID and a holding period on that request.

### The panel API

`/panel/parties/workers`, for every staff role: list and search (part of the name or of the
mobile in one box, `isActive` filter, pages, Persian alphabetical order), read, add, save the
profile, deactivate, activate, and the IBAN requests above. New error codes:
`parties.worker_not_found`, `parties.start_date_in_future`; a missing start date is
`validation.required` on `startDate`.

## Consequences

- Phase 4 (4.1) adds the worker's number on the attendance device to this record.
- Phase 5 adds pay terms with history (5.1) and, if needed, a leaving date and insurance details.
- Phase 10 must allow a worker who never uses the app: the office prints or reads the statement
  (5.6) for that worker, so nothing in the account depends on the worker logging in.
- Phase 15 (15.1) adds the national ID to contractors and workers for the IBAN verification.
