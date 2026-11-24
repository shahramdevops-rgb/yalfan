# ADR 0023 — The model screens: bags per fabric, every size, sizes added later

- Status: accepted
- Date: 2026-11-24 (1405/09/03)
- Written in: roadmap task 1.6.2 (the second part of 1.6)

## Context

Task 1.6.2 builds the panel screens for the models: the search, a new model with its sizes and
variants, and the model's own page. The API could create a model in one request and add one
variant (ADR 0016), but a model could not be renamed, moved to another category, deactivated,
given a size later, nor could a variant be switched off; those were left to this task. ADR 0022
also left a model's deactivation, and a rate for an inactive model, to it.

In the screens a variant is called a **bag** («کیف») and its SKU its **code** («کد»), because
those are the workshop's words; the code stays model number and bag number (`101-3`).

The developer accepted every proposal of the plan on 1405/09/03 («پیشنهاداتت درسته»), answering
the three questions that only the workshop could:

1. **Which colours in which fabric:** a model's fabrics need not share the same colours. The
   operator ticks, for each fabric, the colours sewn in it.
2. **Sizes:** a model with sizes makes each of its fabrics and colours in every size; a bag not
   made in one size is switched off on the model's page.
3. **A size added later:** to a model that has sizes, it is made in every fabric and colour the
   model still makes; to a model that had none, the bags it already has become that size, and
   their codes stay.

He also allowed a separate browser window at phone width for the check (it could not be used;
see the roadmap).

## Decision

### A new model: one form, one save

- **Four parts only:** the name, an optional category, the sizes, and the fabrics with their
  colours. The **description is not on this form**: it is on the model's page, because the
  store (Phase 11) is the first to need it and the form stays short for the operator.
- **Sizes are typed one by one** after a switch «این مدل سایز دارد»: each name is added with
  Enter or «افزودن» as a small tag that can be taken out before saving. There is **no separate
  field for how many**: the count is how many names were typed, one field less (1.1 described
  the operator saying how many; the plan changed that and was accepted). A name typed twice,
  also on an Arabic keyboard, is caught before sending.
- **Fabrics and colours:** the fabrics as buttons, and under each ticked fabric its own row of
  colours. Buttons that wrap to the next line, not a wide table, so a phone shows them all.
  Inactive fabrics and colours are left out (ADR 0022). Under the form the panel says how many
  bags will be made (fabrics × colours × sizes), so the operator sees the codes before they
  exist.
- **One save, all or nothing**, as before: the panel expands the ticks into one bag per fabric,
  colour and size, the sizes of one colour next to each other so their codes follow each other.
  The request shape of ADR 0016 is unchanged.

### The model's page: every change saved where it is made

- **The number** stands at the top and never changes: it is written on paper.
- **Name and description** are edited by tapping them, as the lists (ADR 0022); **the category**
  is a dropdown saved as soon as it changes. The API takes the three together
  (`PUT /panel/catalog/products/{id}`), as a contractor's profile; the page sends the other two
  as they are. The name stays unique among models. **A category deactivated after the model was
  put in it stays**, and saving the model again never fails on it; only a newly chosen category
  must be active (the rule of a contractor's stages in ADR 0022).
- **A switch for the model** (`…/deactivate`, `…/activate`). A deactivated model takes **no new
  bag, no new size and no new rate** (`catalog.product_inactive`, `production.product_inactive`,
  rule errors); what it has stays. Its page says so and hides the add parts. Every staff role
  may switch it, as the lists.
- **A switch for each bag** (`…/variants/{variantId}/deactivate|activate`). The model's switch
  does not touch the bags' switches, so activating the model again brings back exactly the bags
  that were in use. **A bag is usable only while it, its model, its fabric and its colour are
  active**; pickers built later (work orders in 2.2) apply all four. Switching a bag is allowed
  on an inactive model too, because it adds nothing. A bag is never deleted and its code is
  never given to another bag.
- **More bags** with the same fabric and colour rows. What the model already has is shown ticked
  and locked; a new tick is made in every size of the model. Because adding bags cannot be undone
  (only switched off), the ticks are collected and saved with one button that says how many
  («افزودن ۶ کیف»), not on each tap. The API takes several bags in one request, all or none
  (`POST …/variants` with `{ "variants": [...] }`, replacing the one-bag request of ADR 0016,
  which no screen used yet).
- **The sizes**: renamed by tapping (unique within the model), dragged into order with the
  stages' handle, sent as the whole list as the stages (`PUT …/sizes/order`; one made from an
  old copy is refused with `catalog.size_order_outdated`, 409), and added at the end
  (`POST …/sizes`):
  - to a model **with** sizes, the new size is made in every fabric and colour the model still
    makes, meaning one of its bags in them is active; a fabric or colour deactivated since is
    left out (ADR 0022). The hint under the field says so;
  - to a model **without** sizes, the bags it already has become that size and keep their
    codes. The hint says so before the size is added. The next sizes then follow the rule above.
  A size is never deleted, because bags point at it; a model has at most 20 sizes
  (`catalog.too_many_sizes`).
- **Every change answers the whole model** (`ProductDetails`), so the page shows exactly what
  was saved, new codes included. The category, fabrics and colours in it carry their active
  flag, so an item deactivated since is marked «غیرفعال».
- **A bag or size is looked up within the model in the address**; one of another model is "not
  found" (`catalog.variant_not_found`, `catalog.size_not_found`), so a changed address can never
  switch or rename another model's.

### The model list

- Newest first, twenty per page, searched by number (Persian digits too) or part of the name, the
  search and the page in the address so a reload or the back button keeps them; a page change
  scrolls back to the top. An inactive model stays in its place, greyed, as the lists (ADR 0022);
  there is no active filter, one control fewer, because with the newest first the old inactive
  ones sink by themselves. A filter is one query parameter later if the list ever needs it.

### Sizes: the position is no longer unique

`product_sizes (product_id, position)` was a unique index. Dragging rewrites the positions in one
save and PostgreSQL checks a unique index row by row, so a swap would fail half way: the same
reason as the stages (ADR 0022). The index stays, not unique (migration `SizePositionNotUnique`),
and the sizes are ordered by position, then id.

## Consequences

- Work orders (2.2) pick a bag of an active model whose bag, fabric and colour are active; the
  rate card (1.6.3) shows an inactive model's rates but takes no new one.
- `BagPicker`, `CategorySelect` and the bag arithmetic (`bags.ts`, tested) live in the catalog
  feature; `NameList` takes an optional switch (sizes have none), `InlineName` a multi-line mode
  for the description, `AddNameForm` a hint and a length.
- Found in the browser check and fixed in the shared `SortableList` (so the stages too): a quick
  drag released before React had drawn its last move dropped the row one place short, or not at
  all; the handlers now read the drag from a ref.
