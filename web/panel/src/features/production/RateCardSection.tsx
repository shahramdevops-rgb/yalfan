import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useId, useState, type FormEvent, type KeyboardEvent } from 'react';
import { attempt } from '../../shared/api/refusal';
import { formatDate, formatDay } from '../../shared/format/dates';
import { toPersianDigits } from '../../shared/format/digits';
import { toJalali } from '../../shared/format/jalali';
import { t } from '../../shared/i18n/fa';
import { AmountField } from '../../shared/ui/AmountField';
import { Button } from '../../shared/ui/Button';
import { Card, CardHeader } from '../../shared/ui/Card';
import { InsetForm } from '../../shared/ui/Form';
import { JalaliDateField } from '../../shared/ui/JalaliDateField';
import { Money } from '../../shared/ui/Money';
import { Notice } from '../../shared/ui/Notice';
import { ListSkeleton, LoadError } from '../../shared/ui/QueryStates';
import { StatusBadge } from '../../shared/ui/StatusBadge';
import { correctedBy, entriesOf, entryState, inForceOn, upcomingRate, type EntryState } from './rates';
import { rateCardKey, rateHistoryKey, ratesApi, ratesKey, type RateEntry, type StageRate } from './ratesApi';

/** The stage whose form is open, and the start date it opens with when it corrects an entry. */
interface Editing {
  stageId: string;
  startsOn?: string;
}

/** How many Jalali years back and ahead the start date offers (ADR 0024). */
const yearsBack = 10;
const yearsAhead = 1;

const stateTone = {
  current: 'brand',
  future: 'warning',
  past: 'neutral',
  replaced: 'muted',
} as const satisfies Record<EntryState, string>;

/**
 * A model's piece rates (ADR 0021): every active stage in the order of the work with the rate in
 * force today, a raise already entered for later, and the stage's history. A tap on a rate opens
 * the form for a new one; a tap on an entry of the history opens it on that entry's start date,
 * which corrects it. Nothing is edited or deleted: every rate is a new entry (ADR 0024).
 */
export function RateCardSection({ productId, productIsActive }: { productId: string; productIsActive: boolean }) {
  const headingId = useId();
  const card = useQuery({ queryKey: rateCardKey(productId), queryFn: () => ratesApi.card(productId) });
  const history = useQuery({ queryKey: rateHistoryKey(productId), queryFn: () => ratesApi.history(productId) });
  const [editing, setEditing] = useState<Editing | null>(null);
  const [openHistories, setOpenHistories] = useState<ReadonlySet<string>>(new Set());

  const toggleHistory = (stageId: string, open?: boolean) =>
    setOpenHistories((before) => {
      const after = new Set(before);
      if (open ?? !after.has(stageId)) {
        after.add(stageId);
      } else {
        after.delete(stageId);
      }

      return after;
    });

  return (
    <Card labelledBy={headingId}>
      <CardHeader id={headingId} title={t('rates.title')} hint={t('rates.hint')} />

      {!productIsActive && <Notice tone="warning">{t('rates.inactiveNoAdd')}</Notice>}

      {(card.isPending || history.isPending) && !card.isError && !history.isError && <ListSkeleton rows={3} />}
      {(card.isError || history.isError) && (
        <LoadError
          error={card.error ?? history.error}
          onRetry={() => {
            void card.refetch();
            void history.refetch();
          }}
        />
      )}

      {card.data && history.data && card.data.stages.length === 0 && (
        <p className="text-muted">{t('rates.noStages')}</p>
      )}

      {card.data && history.data && card.data.stages.length > 0 && (
        <ul className="divide-y divide-line-soft">
          {card.data.stages.map((rate) => (
            <StageRow
              key={rate.stage.id}
              productId={productId}
              rate={rate}
              entries={entriesOf(history.data, rate.stage.id)}
              on={card.data.on}
              canAdd={productIsActive}
              editing={editing?.stageId === rate.stage.id ? editing : null}
              onEdit={(startsOn) => setEditing({ stageId: rate.stage.id, startsOn })}
              onClose={() => setEditing(null)}
              historyOpen={openHistories.has(rate.stage.id)}
              onToggleHistory={(open) => toggleHistory(rate.stage.id, open)}
            />
          ))}
        </ul>
      )}
    </Card>
  );
}

interface StageRowProps {
  productId: string;
  rate: StageRate;
  /** This stage's entries, newest first. */
  entries: RateEntry[];
  /** The workshop's today, as the API answered it. */
  on: string;
  canAdd: boolean;
  editing: Editing | null;
  onEdit: (startsOn?: string) => void;
  onClose: () => void;
  historyOpen: boolean;
  onToggleHistory: (open?: boolean) => void;
}

function StageRow({
  productId,
  rate,
  entries,
  on,
  canAdd,
  editing,
  onEdit,
  onClose,
  historyOpen,
  onToggleHistory,
}: StageRowProps) {
  const historyId = useId();
  const upcoming = upcomingRate(entries, on);
  const { stage } = rate;

  const current =
    rate.amount != null && rate.startsOn != null ? (
      <span className="flex flex-col gap-0.5">
        <Money amount={rate.amount} large />
        <span className="text-sm text-muted">{t('rates.since', { date: formatDay(rate.startsOn) })}</span>
      </span>
    ) : (
      <span className="flex flex-col">
        <span className="font-medium text-warning">{t('rates.none')}</span>
        {canAdd && <span className="text-xs text-muted">{t('rates.noneTap')}</span>}
      </span>
    );

  return (
    <li className="flex flex-col gap-3 py-4">
      <div className="flex gap-4">
        <p className={`w-24 shrink-0 pt-1 font-bold sm:w-32 ${stage.isActive ? '' : 'text-faint'}`}>{stage.name}</p>
        <div className="flex min-w-0 flex-1 flex-col items-start gap-2">
          {canAdd ? (
            <button
              type="button"
              onClick={() => onEdit()}
              title={t('rates.change', { stage: stage.name })}
              className="-mx-2 rounded-md px-2 py-1 text-start hover:bg-fill focus-visible:outline-2 focus-visible:outline-brand"
            >
              {current}
              <span className="sr-only">{t('rates.change', { stage: stage.name })}</span>
            </button>
          ) : (
            <div className="py-1">{current}</div>
          )}

          {upcoming && (
            <Notice tone="warning">
              <span className="flex items-start gap-2">
                <span className="shrink-0 pt-0.5">{t('rates.upcoming', { date: formatDay(upcoming.startsOn) })}</span>
                <Money amount={upcoming.amount} />
              </span>
            </Notice>
          )}

          {entries.length > 0 && (
            <button
              type="button"
              aria-expanded={historyOpen}
              aria-controls={historyId}
              onClick={() => onToggleHistory()}
              className="text-sm text-brand hover:underline"
            >
              {t('rates.history', { count: toPersianDigits(String(entries.length)) })}
            </button>
          )}
        </div>
      </div>

      {historyOpen && entries.length > 0 && (
        <ol id={historyId} className="flex flex-col divide-y divide-line rounded-lg bg-subtle px-3">
          {entries.map((entry) => (
            <HistoryRow
              key={entry.id}
              entry={entry}
              state={entryState(entry, entries, on)}
              onCorrect={canAdd && !entry.isReplaced ? () => onEdit(entry.startsOn) : undefined}
            />
          ))}
        </ol>
      )}

      {editing && (
        <RateForm
          // A new form for each start date it opens with, so a correction starts clean.
          key={editing.startsOn ?? 'new'}
          productId={productId}
          rate={rate}
          entries={entries}
          on={on}
          startsOn={editing.startsOn}
          onClose={onClose}
          onHidden={() => onToggleHistory(true)}
        />
      )}
    </li>
  );
}

function HistoryRow({ entry, state, onCorrect }: { entry: RateEntry; state: EntryState; onCorrect?: () => void }) {
  const amount = <Money amount={entry.amount} muted={state === 'replaced' || state === 'past'} />;

  return (
    <li className="flex flex-wrap items-center gap-x-4 gap-y-1 py-2">
      {onCorrect ? (
        <button
          type="button"
          onClick={onCorrect}
          title={t('rates.correct')}
          className="-mx-2 rounded-md px-2 py-1 text-start hover:bg-fill focus-visible:outline-2 focus-visible:outline-brand"
        >
          {amount}
          <span className="sr-only">{t('rates.correct')}</span>
        </button>
      ) : (
        <div className="py-1">{amount}</div>
      )}
      <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-body">
        <span>{t('rates.since', { date: formatDay(entry.startsOn) })}</span>
        <StatusBadge tone={stateTone[state]}>{t(`rates.state.${state}`)}</StatusBadge>
        <span className="text-xs text-muted">{t('rates.enteredOn', { date: formatDate(entry.enteredAt) })}</span>
      </div>
    </li>
  );
}

interface RateFormProps {
  productId: string;
  rate: StageRate;
  entries: RateEntry[];
  on: string;
  /** Set when the form corrects an entry: its start date. Otherwise the form starts on today. */
  startsOn?: string;
  onClose: () => void;
  /** Called when the saved rate is neither today's nor the next one, so only the history shows it. */
  onHidden: () => void;
}

function RateForm({ productId, rate, entries, on, startsOn: correcting, onClose, onHidden }: RateFormProps) {
  const queryClient = useQueryClient();
  const headingId = useId();
  const [digits, setDigits] = useState('');
  const [startsOn, setStartsOn] = useState(correcting ?? on);
  const [error, setError] = useState<string>();
  const [busy, setBusy] = useState(false);

  const corrected = correctedBy(entries, startsOn);
  const current = inForceOn(entries, on);
  const year = toJalali(on).year;

  let hint: string | undefined;
  if (corrected) {
    hint = t('rates.form.corrects');
  } else if (startsOn > on) {
    hint = current ? t('rates.form.future') : t('rates.form.futureNoRate');
  } else if (startsOn < on && current && current.startsOn > startsOn) {
    hint = t('rates.form.superseded', { date: formatDay(current.startsOn) });
  } else if (startsOn < on) {
    hint = t('rates.form.past');
  }

  // The amount the new one is measured against: the one it corrects, else today's.
  const reference = corrected
    ? { label: t('rates.form.onThatDay'), amount: corrected.amount }
    : current && { label: t('rates.form.current'), amount: current.amount };

  async function save(event: FormEvent) {
    event.preventDefault();
    // The button waits for a digit, so an empty amount never gets here; a zero does.
    if (Number(digits) === 0) {
      setError(t('rates.form.amountZero'));
      return;
    }

    setBusy(true);
    setError(undefined);
    const refused = await attempt(
      () => ratesApi.add({ productId, stageId: rate.stage.id, amount: Number(digits), startsOn }),
      () => undefined,
      'amount',
    );

    if (refused) {
      setError(refused);
      setBusy(false);
      return;
    }

    // The form closes on the refreshed card, so the new rate is in place when it does.
    await queryClient.invalidateQueries({ queryKey: ratesKey(productId) });
    const fresh = entriesOf(queryClient.getQueryData<RateEntry[]>(rateHistoryKey(productId)) ?? [], rate.stage.id);
    const saved = correctedBy(fresh, startsOn);
    if (saved && saved.id !== inForceOn(fresh, on)?.id && saved.id !== upcomingRate(fresh, on)?.id) {
      onHidden();
    }

    onClose();
  }

  function onKeyDown(event: KeyboardEvent<HTMLFormElement>) {
    // Escape in an open list closes the list, not the form.
    if (event.key === 'Escape' && !(event.target instanceof HTMLSelectElement) && !busy) {
      event.preventDefault();
      onClose();
    }
  }

  return (
    <InsetForm labelledBy={headingId} onSubmit={(event) => void save(event)} onKeyDown={onKeyDown}>
      <h3 id={headingId} className="font-bold">
        {t(corrected ? 'rates.form.correctTitle' : 'rates.form.title', { stage: rate.stage.name })}
      </h3>

      {reference && (
        <div className="flex items-start gap-2 text-sm text-body">
          <span className="shrink-0 pt-0.5">{reference.label}</span>
          <Money amount={reference.amount} />
        </div>
      )}

      <AmountField
        label={t('rates.form.amount')}
        value={digits}
        onChange={(typed) => {
          setDigits(typed);
          setError(undefined);
        }}
        error={error}
        autoFocus
        disabled={busy}
      />

      <JalaliDateField
        label={t('rates.form.startsOn')}
        value={startsOn}
        onChange={setStartsOn}
        years={{ from: year - yearsBack, to: year + yearsAhead }}
        hint={hint}
        disabled={busy}
      />

      <div className="flex flex-wrap gap-3">
        <Button type="submit" busy={busy} disabled={digits === ''}>
          {t('rates.form.save')}
        </Button>
        <Button variant="secondary" onClick={onClose} disabled={busy}>
          {t('rates.form.cancel')}
        </Button>
      </div>
    </InsetForm>
  );
}
