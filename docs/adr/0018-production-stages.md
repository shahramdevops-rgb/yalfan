# ADR 0018 — Production stages: one shared list, no route per model

- Status: accepted
- Date: 2026-11-22 (1405/09/01)
- Written in: roadmap task 1.2

## Context

The plan for 1.2 was a stage list plus an ordered route of stages for each model, so that work
orders could follow the route and progress could be shown per stage. The developer answered the
open questions O.2 and Q.1 on 1405/09/01:

- **Cutting** is done in the workshop, never by a contractor.
- **Sewing** is done either by outside contractors, paid per piece, or by in-house workers on a
  day wage.
- **Packing** is the last step.
- Parts of a job may be given out on their own, for example **sewing only the handles**.
- Whether leather or any fabric adds a step is not known.
- A route per model was refused: the steps differ from model to model and could become many, so
  defining an ordered route for every one of many models would make the system complicated for
  the operator without a report that needs it today. His words: build the base now, add the
  details as the phases need them.

## Decision

- **Stages are one shared list**, stored as data in the table `stages`: an id and a unique
  Persian name. Rate cards (1.5) and work orders (Phase 2) point at a stage, so pay stays
  *stage × accepted quantity × rate* (ARCHITECTURE.md 7.1).
- **No route per model.** `ProductRouting` is dropped from the Production module. A work order
  will name the variant and the stage directly. Why: there is no route to follow in the
  workshop's own practice, and the only later reader of a route, the progress report (2.8), can
  count per stage from the work orders themselves.
- **Part of a job is one more stage.** "Sewing the handles" is added to the same list when it is
  first given out, with its own rates. Why: it needs no new structure, so the list grows with the
  workshop instead of being designed for every case in advance.
- **The three stages the developer named are inserted by the migration** that creates the table
  (`AddStages`): «برش», «دوخت», «بسته‌بندی», in the order of the work. Why: every database
  (development, tests, the server) starts with the real list and nobody has to type it at
  go-live. They are inserted once with `InsertData`, not with `HasData`, so a rename by the
  operator is never compared against seed data. Their ids are version 7 with one time and rising
  last digits, so the list, ordered by id like the catalog lists, shows them in the order of the
  work and before any stage the operator adds. Being written by the migration, they have no audit
  entry; every later change does (ADR 0013).
  **Changed 1405/09/03 (task 1.6.1, ADR 0022):** the order is now a position the operator drags,
  not the id; the migration gave the existing stages their positions in id order.
- **The name is one Persian column, not translation rows.** Stages appear only in the Persian
  panels and never in the store, and ADR 0006 puts only catalog text, which the store shows, in
  translation rows. The name is normalized like catalog names (`PersianText.Normalize`) before it
  is checked or stored, so «ي» typed on an Arabic keyboard is not a second stage.
- **No delete.** Work orders will point at stages. Deactivation comes with the screens in 1.6,
  together with the catalog lists, which do not have it yet either.
  **Done 1405/09/03 (task 1.6.1, ADR 0022):** stages and the catalog lists are deactivated and
  activated; nothing new can point at an inactive one.
- **Panel API:** `/panel/production/stages`, list, add and rename, for every staff role, like the
  catalog lists, because the operator enters the master data. Error codes:
  `production.name_required`, `production.name_too_long`, `production.name_taken`,
  `production.stage_not_found`.

## Not decided here

- **Whether the system refuses to give cutting to a contractor.** Cutting is in-house only today;
  the only place that would check it is issuing a work order, so 2.2 decides it. Adding a flag to
  the stage then is one column.
- **Whether the number of cut pieces is recorded**, and **whether bags sewn by in-house day-wage
  workers are counted.** Both change what the progress report (2.8) can show and are asked before
  Phase 2 starts.

## Consequences

- 1.3 (contractors) keeps "which stages a contractor does" as an open detail of that task; the
  list it would point at now exists.
- If an ordered route is ever needed (for example to warn when sewing is issued before cutting),
  it is one table of (model, stage, position) on top of this list; no stage, rate or work order
  row changes.
