// Which bags a model is made in, as the operator ticks them: for each fabric, the colours sewn in
// it (decided 1405/09/03, ADR 0023). Kept apart from the components so it is tested without a
// browser. Every ticked fabric and colour is made in every size of the model (also decided then).

/** One fabric and the colours ticked in it, in the order they were ticked. */
export interface FabricPick {
  fabricId: string;
  colourIds: readonly string[];
}

/** The fabrics in the order they were ticked, each with its colours. */
export type Picks = readonly FabricPick[];

/** A bag of the model being created: its size by name, because the sizes have no ids yet. */
export interface NewBag {
  fabricId: string;
  colourId: string;
  size: string | null;
}

/** A bag added to an existing model: its size by id. */
export interface BagToAdd {
  fabricId: string;
  colourId: string;
  sizeId: string | null;
}

/** An existing bag, as the model's page has it. */
interface ExistingBag {
  fabric: { id: string };
  colour: { id: string };
}

/** The fabric ticked when it was not, or taken out with its colours when it was. */
export function toggleFabric(picks: Picks, fabricId: string): FabricPick[] {
  return picks.some((pick) => pick.fabricId === fabricId)
    ? picks.filter((pick) => pick.fabricId !== fabricId)
    : [...picks, { fabricId, colourIds: [] }];
}

/** The colour ticked in this fabric when it was not, or unticked when it was. */
export function toggleColour(picks: Picks, fabricId: string, colourId: string): FabricPick[] {
  return picks.map((pick) =>
    pick.fabricId !== fabricId
      ? pick
      : {
          fabricId,
          colourIds: pick.colourIds.includes(colourId)
            ? pick.colourIds.filter((id) => id !== colourId)
            : [...pick.colourIds, colourId],
        },
  );
}

/** Every ticked fabric and colour, once each, in the order they were ticked. */
export function pairsOf(picks: Picks): { fabricId: string; colourId: string }[] {
  return picks.flatMap((pick) => pick.colourIds.map((colourId) => ({ fabricId: pick.fabricId, colourId })));
}

/** How many bags the ticks make: each fabric and colour once per size, or once without sizes. */
export function bagCount(picks: Picks, sizeCount: number): number {
  return pairsOf(picks).length * Math.max(sizeCount, 1);
}

/**
 * The bags of a new model: each ticked fabric and colour in every size, the sizes of one colour
 * next to each other so their codes follow each other.
 */
export function newBags(picks: Picks, sizes: readonly string[]): NewBag[] {
  return pairsOf(picks).flatMap((pair): NewBag[] =>
    sizes.length === 0 ? [{ ...pair, size: null }] : sizes.map((size) => ({ ...pair, size })),
  );
}

/** The fabrics and colours a model already has a bag in, to show them ticked and locked. */
export function existingPicks(bags: readonly ExistingBag[]): FabricPick[] {
  return bags.reduce<FabricPick[]>((picks, bag) => {
    const pick = picks.find((p) => p.fabricId === bag.fabric.id);
    if (!pick) {
      return [...picks, { fabricId: bag.fabric.id, colourIds: [bag.colour.id] }];
    }

    return pick.colourIds.includes(bag.colour.id)
      ? picks
      : picks.map((p) => (p === pick ? { ...p, colourIds: [...p.colourIds, bag.colour.id] } : p));
  }, []);
}

/**
 * The bags to add to an existing model: each newly ticked fabric and colour in every size. One the
 * model already has a bag in is left out; a size it lacks there is switched on in its own row.
 */
export function bagsToAdd(picks: Picks, existing: readonly ExistingBag[], sizeIds: readonly string[]): BagToAdd[] {
  const has = (fabricId: string, colourId: string) =>
    existing.some((bag) => bag.fabric.id === fabricId && bag.colour.id === colourId);

  return pairsOf(picks)
    .filter((pair) => !has(pair.fabricId, pair.colourId))
    .flatMap((pair): BagToAdd[] =>
      sizeIds.length === 0 ? [{ ...pair, sizeId: null }] : sizeIds.map((sizeId) => ({ ...pair, sizeId })),
    );
}
