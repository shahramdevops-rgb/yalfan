# ADR 0016 — Catalog: numbered models, variants of fabric × colour × size

- Status: accepted
- Date: 2026-11-22 (1405/09/01)
- Written in: roadmap task 1.1

## Context

Production, stock and the store all point at the catalog, so what a "bag" is must be settled
before the first work order. The developer answered on 1405/09/01 (roadmap Phase 1):

- The workshop has **no model codes and no SKUs** today.
- It makes **many models**.
- One model is sewn in **several fabrics**; **colours are one shared list**, any colour with any
  fabric.
- **Sizes belong to each model**: a model has sizes or not; when it has, the operator ticks
  "has sizes", says how many and types their names.

## Decision

### What is stored

```
Product (model): number, Persian name and description (ADR 0006), optional category, active
  ├── ProductSize: position, name          (only when the model has sizes)
  └── Variant:     number, SKU, fabric, colour, optional size, active
Category, Fabric, Colour: shared lists, each with a unique Persian name
```

- A **variant** is one bag that can be made and stocked: fabric × colour × size. Work orders and
  stock will point at variants, which is why the shape is fixed now: changing it later would
  move every one of those rows.
- **Sizes**: a model either has none, and then its variants have none, or it has one or more,
  and then every variant names one of them (`catalog.size_required`,
  `catalog.size_not_allowed`). Size names are unique within one model, checked in the Domain;
  the same name ("بزرگ") is normal across models, so there is no unique index on it.
- **Categories** are one level, at most one per model, and optional for now: the store is the
  first to need them, and a parent level is one column later.

### Numbers and codes are made by the system

- **Model number**: from the PostgreSQL sequence `product_numbers`, starting at **101**, because
  a one-digit number on a work order reads like a quantity. A sequence and not `MAX + 1`, so two
  saves at the same moment never get the same number. The handler takes the number after every
  check that reads the database; a number taken by a save that then fails is skipped, which is
  harmless because a model number is a label, not a legal document. It is separate from the
  `Guid` id, like every number people read (ARCHITECTURE.md 12).
- **SKU**: model number and variant number, `101-1`, `101-2`, stored and unique. Why: short,
  made by the system, readable on a label or a barcode, and it needs no codes invented for
  fabrics and colours; the screen always shows the fabric, colour and size names beside it.
  The variant number is the model's highest plus one; the unique index on (model, number) stops
  two variants added at the same moment from sharing one (the loser gets an unexpected error,
  not a duplicate).

### Uniqueness, in the Domain and in the database

- One variant per fabric, colour and size in a model: a Domain check for the coded answer
  (`catalog.variant_exists`) and a unique index with `NULLS NOT DISTINCT`, so two variants
  without a size also collide.
- No two models, categories, fabrics or colours share a Persian name (`catalog.name_taken`),
  after normalization (ADR 0006). Why: with many models, entering the same model twice is the
  likely mistake.

### Nothing is deleted

Models and variants are never deleted, only deactivated, because work orders and stock will point
at them. Every foreign key in the catalog is `RESTRICT`, so a delete can never cascade into them.

### The API (`/panel/catalog`, every staff role)

- Categories, fabrics, colours: add, rename, list. Lists come in the order the items were added
  (the ids are time-ordered); the panel sorts by name with Persian alphabetical order, which the
  database's default collation does not have.
- Models: create with sizes and first variants in **one request**, so a half-entered model is
  never saved; there a variant names its size by the name typed in the same request, because the
  sizes have no ids yet. Add a variant later by size id. Read one model with every name in
  Persian. Search by number (Persian digits work) or part of the name, newest first, 20 per page
  by default and at most 100.
- Every staff role may use it, because the operator enters the models (roadmap 1.6).

### Not in this task

Renaming a model or changing its category, deactivating models and variants, and adding a size to
an existing model come with the panel screens (1.6). Prices (Phases 8 and 11), images (11.4) and
the material recipe (Phase 6) are later.

## Consequences

- Production orders, work orders and stock movements reference `Variant`, never a colour or a
  model alone.
- Whether a size or a fabric changes the material quantities is asked in Phase 6; the variant
  already carries both, so either answer fits.
- The operator never types a code, and the codes stay short until the workshop has more than
  999 models (then four digits, `1000-1`, with no change).
