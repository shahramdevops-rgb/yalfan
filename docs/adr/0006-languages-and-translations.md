# ADR 0006 — Persian-only panels, a three-language store, catalog text in translation rows

- Status: accepted
- Date: 2026-11-22 (1405/09/01)
- Written in: roadmap task 1.1

## Context

The system has three front doors (ARCHITECTURE.md 1). The panel and the contractor panel are used
by people in Hamedan who read Persian; the store sells to retail, wholesale and foreign buyers in
Persian, Arabic and English. The catalog (models, categories, fabrics, colours, sizes) is entered
once, in the panel, and shown everywhere, so its text must be able to exist in three languages
from the first model on. On 1405/09/01 the developer added: **no translation is done in the
panel; everything there is Persian.**

## Decision

### Panels are Persian only

- No language switch, no locale in panel requests or responses. The panel API returns the
  Persian text directly (`name`, `description`), so the office operator, who is not technical,
  never sees the idea of a language.
- UI text lives in message files and the API returns error codes, as before (ARCHITECTURE.md 10).

### Catalog text lives in translation rows, one table per kind

| Item | Table | Text |
|---|---|---|
| Model | `product_translations` | name, description |
| Category | `category_translations` | name |
| Fabric | `fabric_translations` | name |
| Colour | `colour_translations` | name |
| Size of a model | `product_size_translations` | name |

- Each row is keyed by the item and the locale, so an item has at most one text per language.
  The locale is stored as `fa`, `ar` or `en`, the codes a hand-written query expects.
- Why rows and not one Persian column: the store shows this text in three languages, and moving
  single Persian columns into rows later would touch every product; adding a language is rows,
  not a schema change.
- Why one table per kind and not one generic table (`entity_type`, `entity_id`): a table per
  kind has a real foreign key to its item and its own unique index, so "no two colours with the
  same name in the same language" is enforced by the database.
- In code the rows are EF owned collections (`NameTranslation`, `ProductTranslation`); the
  Domain exposes `PersianName` for the panel.

### Only Persian is filled for now

- The panel writes the Persian row and nothing else. A missing row means "not translated yet".
- Where Arabic and English are entered, and what the store shows when a row is missing, are
  Phase 11 questions (roadmap Phase 11, open questions). Task 11.5 still lists translations in
  the panel and must change with that answer.

### Persian names are normalized

Before a Persian name is checked, stored or searched, `PersianText.Normalize` trims it, turns runs
of white space into one space, and turns the Arabic «ي», «ى» and «ك» into the Persian «ی» and «ک».
Why: the same name typed on an Arabic keyboard would otherwise be a different name, and duplicates
would slip past the unique checks. Digits and the zero-width non-joiner are left as typed. This is
applied to Persian only; Arabic text keeps its own letters.

## Consequences

- Every new catalog text (image alt text in 11.4, for example) gets a translation table of its
  own kind, not a column.
- Panel queries pick the `fa` row; the store's queries will pick the visitor's locale.
- An audit entry of a translation row has `entity_type` = the translation table and `entity_id`
  = `<item id>,fa` (ADR 0013).
