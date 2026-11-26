import { useState } from 'react';
import { t } from '../i18n/fa';
import { FieldError } from './Form';
import { InlineName } from './InlineName';
import { SortableList } from './SortableList';
import { StatusBadge } from './StatusBadge';
import { Switch } from './Switch';

/** A row of a simple list of names: a fabric, a colour, a category, a stage. */
export interface NamedItem {
  id: string;
  name: string;
  isActive: boolean;
}

interface NameListProps {
  /** In the order to show. */
  items: readonly NamedItem[];
  /** Each answers the message to show when the API refuses, or nothing. */
  onRename: (id: string, name: string) => Promise<string | undefined>;
  /** Left out for a list whose items cannot be switched off (a model's sizes): no switch column. */
  onSetActive?: (id: string, isActive: boolean) => Promise<string | undefined>;
  /** Given only for a list whose order means something (the stages); its rows get a drag handle. */
  onMove?: (from: number, to: number) => void;
  moving?: boolean;
  emptyText: string;
}

/**
 * The list screens' one shape: the name, edited by tapping it, and a switch for active. An
 * inactive row stays where it is, greyed and marked, so the switch stays under the finger and
 * undoing a slip is one more tap; nothing is deleted (ADR 0022).
 */
export function NameList({ items, onRename, onSetActive, onMove, moving = false, emptyText }: NameListProps) {
  if (items.length === 0) {
    return <p className="py-6 text-center text-muted">{emptyText}</p>;
  }

  const row = (item: NamedItem) => <Row item={item} onRename={onRename} onSetActive={onSetActive} />;

  return (
    <div>
      <div className="flex items-center justify-between border-b border-line pb-2 text-sm text-muted">
        <span className={onMove ? 'ps-11' : ''}>{t('list.columnName')}</span>
        {onSetActive && <span>{t('list.columnActive')}</span>}
      </div>
      {onMove ? (
        <SortableList
          items={items}
          getId={(item) => item.id}
          getName={(item) => item.name}
          onMove={onMove}
          locked={moving}
          renderItem={row}
        />
      ) : (
        <ul className="divide-y divide-line-soft">
          {items.map((item) => (
            <li key={item.id} className="flex items-center gap-2 py-2">
              {row(item)}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

function Row({
  item,
  onRename,
  onSetActive,
}: {
  item: NamedItem;
  onRename: NameListProps['onRename'];
  onSetActive: NameListProps['onSetActive'];
}) {
  const [error, setError] = useState<string>();
  const [busy, setBusy] = useState(false);

  async function toggle(isActive: boolean) {
    if (!onSetActive) {
      return;
    }

    setBusy(true);
    setError(await onSetActive(item.id, isActive));
    setBusy(false);
  }

  return (
    <div className="flex min-w-0 flex-1 flex-col gap-1">
      <div className="flex min-h-10 items-center gap-3">
        <div className="flex min-w-0 flex-1 items-center gap-2">
          <InlineName value={item.name} onSave={(name) => onRename(item.id, name)} muted={!item.isActive} />
          {!item.isActive && <StatusBadge>{t('list.inactive')}</StatusBadge>}
        </div>
        {onSetActive && (
          <Switch
            checked={item.isActive}
            onChange={(isActive) => void toggle(isActive)}
            label={t('list.activeLabel', { name: item.name })}
            disabled={busy}
          />
        )}
      </div>
      {error && <FieldError>{error}</FieldError>}
    </div>
  );
}
