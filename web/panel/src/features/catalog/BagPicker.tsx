import { byPersianName } from '../../shared/format/persianOrder';
import { t } from '../../shared/i18n/fa';
import { Chip } from '../../shared/ui/Chip';
import { toggleColour, toggleFabric, type Picks } from './bags';
import type { CatalogListItem } from './catalogListsApi';

interface BagPickerProps {
  /** The whole lists, inactive items included: a bag the model already has may be in one. */
  fabrics: readonly CatalogListItem[];
  colours: readonly CatalogListItem[];
  picks: Picks;
  onChange: (picks: Picks) => void;
  /** The fabrics and colours the model already has bags in: shown ticked, and cannot be unticked. */
  locked?: Picks;
}

/**
 * The fabrics as buttons, and under each ticked fabric its own row of colours (decided
 * 1405/09/03): a model's fabrics need not share the same colours, and nothing that is not sewn
 * gets a code. Buttons that wrap to the next line, not a wide table, so a phone shows them all.
 * Inactive fabrics and colours are left out, unless the model already has a bag in them (ADR 0022).
 */
export function BagPicker({ fabrics, colours, picks, onChange, locked = [] }: BagPickerProps) {
  const lockedColours = (fabricId: string) => locked.find((pick) => pick.fabricId === fabricId)?.colourIds ?? [];
  const pickedColours = (fabricId: string) => picks.find((pick) => pick.fabricId === fabricId)?.colourIds;
  const isLocked = (fabricId: string) => locked.some((pick) => pick.fabricId === fabricId);

  const shownFabrics = byPersianName(fabrics.filter((fabric) => fabric.isActive || isLocked(fabric.id)));
  const ticked = shownFabrics.filter((fabric) => isLocked(fabric.id) || pickedColours(fabric.id) !== undefined);

  // A colour ticked in a fabric that was locked, but not yet picked, needs that fabric's pick first.
  function tickColour(fabricId: string, colourId: string) {
    const withFabric = pickedColours(fabricId) === undefined ? toggleFabric(picks, fabricId) : picks;
    onChange(toggleColour(withFabric, fabricId, colourId));
  }

  if (shownFabrics.length === 0) {
    return <p className="text-stone-500">{t('bags.noFabrics')}</p>;
  }

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-col gap-2">
        <p className="text-sm font-medium text-stone-700">{t('bags.fabrics')}</p>
        <div className="flex flex-wrap gap-2">
          {shownFabrics.map((fabric) => (
            <Chip
              key={fabric.id}
              label={fabric.name}
              on={ticked.includes(fabric)}
              locked={isLocked(fabric.id)}
              title={isLocked(fabric.id) ? t('bags.lockedHint') : undefined}
              note={fabric.isActive ? undefined : t('bags.inactive')}
              onToggle={() => onChange(toggleFabric(picks, fabric.id))}
            />
          ))}
        </div>
      </div>

      {ticked.map((fabric) => {
        const already = lockedColours(fabric.id);
        const picked = pickedColours(fabric.id) ?? [];
        // No new bag in an inactive fabric or colour: the API refuses it (ADR 0022).
        const shownColours = byPersianName(
          colours.filter((colour) => already.includes(colour.id) || (fabric.isActive && colour.isActive)),
        );

        return (
          <div key={fabric.id} className="flex flex-col gap-2 border-s-2 border-brand-100 ps-3">
            <p className="text-sm font-medium text-stone-700">{t('bags.coloursOf', { fabric: fabric.name })}</p>
            {shownColours.length === 0 ? (
              <p className="text-sm text-stone-500">{t('bags.noColours')}</p>
            ) : (
              <div className="flex flex-wrap gap-2">
                {shownColours.map((colour) => (
                  <Chip
                    key={colour.id}
                    label={colour.name}
                    on={already.includes(colour.id) || picked.includes(colour.id)}
                    locked={already.includes(colour.id)}
                    title={already.includes(colour.id) ? t('bags.lockedHint') : undefined}
                    note={colour.isActive ? undefined : t('bags.inactive')}
                    onToggle={() => tickColour(fabric.id, colour.id)}
                  />
                ))}
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
}
