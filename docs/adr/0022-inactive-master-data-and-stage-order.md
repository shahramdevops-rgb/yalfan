# ADR 0022 — Inactive master data, and the stages in an order dragged by the operator

- Status: accepted
- Date: 2026-11-24 (1405/09/03)
- Written in: roadmap task 1.6.1 (the first part of 1.6)

## Context

Task 1.6 builds the panel screens for the master data of Phase 1. The catalog lists (categories,
fabrics, colours) and the stages could be added and renamed, but not removed: models, variants,
contractors, rates and later work orders point at them, so they can never be deleted (ADR 0016,
ADR 0018). The screens need a way to take an item out of use. And the stages were ordered by
id, the order they were added (ADR 0018), so a stage added later, such as «دوخت دسته», could
only ever appear after «بسته‌بندی».

The developer approved the plan for 1.6.1 on 1405/09/03, with two changes of his own:

- **The stages must be shown in the order of the work**, never «دوخت» and «برش» after
  «بسته‌بندی», and **the operator drags a stage up or down** to put it in place.
- **No needless buttons** when a simpler action does the job.

The proposals he did not change were accepted as written: what "inactive" means (below), and the
three catalog lists on one page with a tab each, the stages on a page of their own.

## Decision

### Deactivate, never delete

Categories, fabrics, colours and stages get an **active flag** (`is_active`), like contractors
and workers (ADR 0019, ADR 0020). Every existing row started active.

- **An inactive item is left out of the pickers for new work.** What already points at it keeps
  it, and the screens show its name with «غیرفعال»: variants in a deactivated fabric or colour,
  models in a deactivated category, contractors and workers who do a deactivated stage, and the
  rates of a deactivated stage.
- **The API refuses a new link to an inactive item**, not only the screen, because a screen left
  open in another tab can still send it:
  - a new model in an inactive category: `catalog.category_inactive`;
  - a new variant (with a new model or added later) in an inactive fabric or colour:
    `catalog.fabric_inactive`, `catalog.colour_inactive`;
  - a contractor or worker who **newly** ticks an inactive stage: `parties.stage_inactive`. A
    stage ticked **before** it was deactivated stays allowed, so saving the profile again never
    fails on it;
  - a new rate for an inactive stage: `production.stage_inactive`.
  All are rule errors (422).
- **The rate card leaves inactive stages out**, because nothing new is given out at them; their
  rates stay in the rate history.
- **Activating puts the item back as it was**: a stage keeps its place in the order while it is
  inactive.
- **Every staff role may deactivate and activate**, as they may add and rename these lists
  (1.1, 1.2); each change is audited (`isActive` old and new).
- A model's own deactivation, and a rate for an inactive model, come with the model screens
  (1.6.2): the model's flag exists since 1.1, but nothing can set it yet.

Why: one rule for every list of master data, the same as the contractors'. Deleting is
impossible once something points at an item, and hiding it only on the screen would let an old
tab or a later caller (the contractor app, an assistant) use it again.

### The order of the stages is a position the operator drags

- **`stages.position`**, from 1, is the order of the work. Every list of stages follows it: the
  stage list, a contractor's and a worker's stages, the rate card and the rate history. Ties
  (only two stages added at the same moment can tie) fall back to the id, so the order is the
  same everywhere. The migration gave the existing stages their positions in id order, which was
  the order of the work for the three seeded ones.
- **A new stage goes to the end**; the operator drags it into place.
- **The new order is sent as the whole list** (`PUT /panel/production/stages/order`,
  `{ "stageIds": [...] }`), not as "move this stage to place 3". The Domain applies it only if it
  names every stage exactly once; otherwise it was made from an old copy (another user added a
  stage meanwhile) and is refused with `production.stage_order_outdated` (409), changing
  nothing; the panel then reloads the list. A list that names a stage twice is a validation
  error with the same code. Why the whole list: the request says exactly what the operator saw,
  and a stale one cannot be half applied.
- **The position is not a unique column.** Dragging rewrites the positions in one save, and
  PostgreSQL checks a unique index row by row, so swapping two stages would fail half way. The
  tie rule above keeps the order stable without it.
- **Only the stages whose place changed are written**, each with its own audit entry
  (`position` old and new).

### The screens

- **«فهرست‌ها»**: fabrics, colours and categories, one tab each, the tab in the address
  (`/lists/fabrics`) so a reload opens the same list. Sorted by Persian name in the browser
  (`Intl.Collator('fa')`), as ADR 0016 planned. **«مراحل کار»** (`/stages`): in the stored
  order, never sorted by name.
- **Few controls, all direct** (the developer's rule):
  - the name is edited where it stands: tap it, type, Enter or leave the field to save, Escape
    to undo. No rename button or dialog;
  - a **switch** turns an item on and off, its position showing the state. No activate and
    deactivate buttons, and no confirmation, because it is undone with one more tap;
  - a stage is **dragged by a handle** to its place. The handle is the only extra control: on a
    phone, dragging the whole row would fight with scrolling and with tapping the name;
  - one field and an «افزودن» button add a name. The field empties as soon as a name is sent,
    so the next can be typed while the first is still on its way over a slow line; a refused
    name comes back into the field with its message (found in the browser check: a two-second
    add had swallowed the next name).
- **An inactive row stays where it is**, greyed and marked «غیرفعال». The plan had them at the
  end of the list; keeping them in place was chosen while building, because the switch then
  stays under the finger and a slip is undone at once, and in the stage list a row that jumped
  to the end would misstate the order of the work.
- **The drag is written on Pointer Events, without a library**, so one code path serves a
  mouse, a finger and a pen; the handle stops the page from scrolling under the finger. The
  keyboard moves a stage with the up and down arrows on its handle, and a screen reader hears
  the new place. The new order shows at once and goes back if the API refuses it; while it is
  being saved, another drag waits and the handles are dimmed to show it. Why no library: the lists are short, the arithmetic is three
  small tested functions, and the panel stays light on mobile internet (ADR 0014).

## Consequences

- The list endpoints (`/panel/catalog/categories|fabrics|colours`, `/panel/production/stages`)
  return inactive items too, with `isActive`; every picker built later filters them out itself.
  `CatalogListItem` carries the flag for the list screens; `CatalogOptionSummary` stays without
  it, because it also describes a model's sizes, which have no flag. `StageSummary` carries it
  everywhere, so a contractor or a rate history can mark a deactivated stage.
- Picking a contractor by stage in 2.2 and the progress view in 2.8 read the stages in this
  order; nothing there needs a route per model (ADR 0018 still holds).
- The panel has its first shared list parts (`NameList`, `SortableList`, `InlineName`, `Switch`,
  `AddNameForm`); later list screens reuse them. Sizes of a model (1.6.2) can reuse the sortable
  list if their order needs changing.
