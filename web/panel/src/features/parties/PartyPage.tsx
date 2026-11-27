import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useId, useRef, useState, type FormEvent, type KeyboardEvent } from 'react';
import { useParams } from 'react-router';
import { toProblem } from '../../shared/api/problem';
import { attempt } from '../../shared/api/refusal';
import { useSerialWrites } from '../../shared/api/serialWrites';
import { dayInTehran, formatDay } from '../../shared/format/dates';
import { toPersianDigits } from '../../shared/format/digits';
import { toJalali } from '../../shared/format/jalali';
import { formatMobile } from '../../shared/format/mobile';
import { t } from '../../shared/i18n/fa';
import { Button } from '../../shared/ui/Button';
import { Card } from '../../shared/ui/Card';
import { Field, FieldError, InsetForm } from '../../shared/ui/Form';
import { InlineName } from '../../shared/ui/InlineName';
import { JalaliDateField } from '../../shared/ui/JalaliDateField';
import { Notice } from '../../shared/ui/Notice';
import { PageHeader } from '../../shared/ui/PageHeader';
import { ListSkeleton, LoadError } from '../../shared/ui/QueryStates';
import { Switch } from '../../shared/ui/Switch';
import { HistoryLink } from '../history/HistoryLink';
import { stagesApi, stagesKey } from '../production/stagesApi';
import { IbanSection } from './IbanSection';
import {
  partiesApi,
  partyKey,
  partySearchesKey,
  profileChange,
  startDateOf,
  type PartyDetails,
  type PartyKind,
  type PartyProfile,
  type Write,
} from './partiesApi';
import { startYearsBack } from './partyForm';
import { StagePicker } from './StagePicker';
import { keepsOneStage } from './stagePicks';

/** The longest name and notes the API takes (PartyProfile). */
const nameMaxLength = 100;
const notesMaxLength = 500;

/**
 * One contractor or worker. As on a model's page there is no save button: the name, the mobile
 * and the notes are edited where they stand, each stage tick is saved at once, and a switch turns
 * the person on and off (decided 1405/09/03). Only a worker's start date opens a small form,
 * because three lists saved one by one would store half a date (ADR 0025). The IBAN has its own
 * card and requests (ADR 0019). Phase 2 adds a contractor's work and account under these cards.
 */
export function PartyPage({ kind }: { kind: PartyKind }) {
  const { id = '' } = useParams();
  const queryClient = useQueryClient();
  const key = partyKey(kind, id);
  const party = useQuery({ queryKey: key, queryFn: () => partiesApi[kind].get(id) });
  // One change after another, each from the last answer: leaving the notes by tapping a stage
  // starts two saves in one gesture, and each sends the whole profile.
  const write: Write = useSerialWrites<PartyDetails>(key, () => {
    void queryClient.invalidateQueries({ queryKey: partySearchesKey(kind) });
  });

  if (party.isPending) {
    return <ListSkeleton rows={6} />;
  }

  if (party.isError) {
    const code = toProblem(party.error).code;
    const missing = code === 'parties.contractor_not_found' || code === 'parties.worker_not_found';
    return (
      <div className="flex max-w-3xl flex-col gap-4">
        <PageHeader back={backTo(kind)} />
        {missing ? (
          <p className="text-body">{t(`parties.${kind}.missing`)}</p>
        ) : (
          <LoadError error={party.error} onRetry={() => void party.refetch()} />
        )}
      </div>
    );
  }

  // Keyed by the person: the owner's whole IBAN, a half-typed IBAN or start date belong to this
  // person and must not stay on the page when it goes straight to another person.
  return (
    <div key={party.data.id} className="flex max-w-3xl flex-col gap-6">
      <PageHeader back={backTo(kind)} actions={<HistoryLink kind={kind} id={party.data.id} />} />
      <Profile kind={kind} party={party.data} write={write} />
      <IbanSection kind={kind} party={party.data} write={write} />
    </div>
  );
}

const backTo = (kind: PartyKind) => ({ to: `/${kind}`, label: t(`parties.${kind}.back`) });

function Profile({ kind, party, write }: { kind: PartyKind; party: PartyDetails; write: Write }) {
  const stages = useQuery({ queryKey: stagesKey, queryFn: stagesApi.list });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();
  const [stagesError, setStagesError] = useState<string>();

  const update = (change: Partial<PartyProfile>) => write(profileChange(kind, change));
  const save = (change: Partial<PartyProfile>, field: string) => attempt(() => update(change), ignore, field);

  async function run(send: () => Promise<PartyDetails>, showError: (message: string | undefined) => void) {
    setBusy(true);
    showError(await attempt(send, ignore));
    setBusy(false);
  }

  const startDate = startDateOf(party);

  return (
    <Card>
      <div className="flex items-start gap-4">
        <div className="flex min-w-0 flex-1 flex-col gap-1 pt-1">
          <InlineName
            large
            heading
            value={party.fullName}
            muted={!party.isActive}
            maxLength={nameMaxLength}
            onSave={(fullName) => save({ fullName }, 'fullName')}
          />
        </div>
        <div className="flex shrink-0 flex-col items-center gap-1 pt-1">
          <span className="text-xs text-muted">{t('list.columnActive')}</span>
          <Switch
            checked={party.isActive}
            onChange={(isActive) =>
              void run(() => write(() => partiesApi[kind].setActive(party.id, isActive)), setError)
            }
            label={t('list.activeLabel', { name: party.fullName })}
            disabled={busy}
          />
        </div>
      </div>

      {!party.isActive && <Notice tone="warning">{t(`parties.${kind}.inactiveNote`)}</Notice>}

      {error && <FieldError>{error}</FieldError>}

      <Field label={t('parties.mobile')}>
        {/* A row, so the left-to-right number sits at the start of the line, not across it. */}
        <div className="flex">
          <InlineName
            numeric
            // Persian digits in the field, as everywhere; the API reads them as the same number.
            value={toPersianDigits(party.mobileNumber)}
            display={formatMobile(party.mobileNumber)}
            label={t('parties.mobile')}
            maxLength={20}
            onSave={(mobileNumber) => save({ mobileNumber }, 'mobileNumber')}
          />
        </div>
      </Field>

      {startDate && <StartDate startDate={startDate} update={update} />}

      <Field label={t('parties.stages')} hint={t(`parties.${kind}.stagesHint`)}>
        {stages.isPending && <ListSkeleton rows={1} />}
        {stages.isError && <LoadError error={stages.error} onRetry={() => void stages.refetch()} />}
        {stages.data && (
          <StagePicker
            stages={stages.data}
            ticked={party.stages.map((stage) => stage.id)}
            onChange={(stageIds) => void run(() => update({ stageIds }), setStagesError)}
            keepOne={keepsOneStage(kind)}
            disabled={busy}
          />
        )}
        {stagesError && <FieldError>{stagesError}</FieldError>}
      </Field>

      <Field label={t('parties.notes')}>
        <InlineName
          multiline
          allowEmpty
          maxLength={notesMaxLength}
          value={party.notes ?? ''}
          label={t('parties.notes')}
          placeholder={t('parties.notesEmpty')}
          onSave={(notes) => save({ notes: notes || null }, 'notes')}
        />
      </Field>
    </Card>
  );
}

const ignore = () => undefined;

interface StartDateProps {
  startDate: string;
  update: (change: Partial<PartyProfile>) => Promise<PartyDetails>;
}

/** A worker's start date: shown as a date; a tap opens the three lists with «ثبت» and «انصراف». */
function StartDate({ startDate, update }: StartDateProps) {
  const headingId = useId();
  // The date's line, drawn anew when the form closes, which then gets the focus back.
  const line = useRef<HTMLButtonElement>(null);
  const [editing, setEditing] = useState(false);
  const [day, setDay] = useState(startDate);
  const [error, setError] = useState<string>();
  const [busy, setBusy] = useState(false);
  const year = toJalali(dayInTehran()).year;

  function open() {
    setDay(startDate);
    setError(undefined);
    setEditing(true);
  }

  async function save(event: FormEvent) {
    event.preventDefault();
    if (day === startDate) {
      setEditing(false);
      return;
    }

    setBusy(true);
    const refused = await attempt(() => update({ startDate: day }), ignore, 'startDate');
    setBusy(false);
    if (refused) {
      setError(refused);
    } else {
      setEditing(false);
    }
  }

  function onKeyDown(event: KeyboardEvent<HTMLFormElement>) {
    // Escape in an open list closes the list, not the form.
    if (event.key === 'Escape' && !(event.target instanceof HTMLSelectElement) && !busy) {
      event.preventDefault();
      setEditing(false);
    }
  }

  return (
    <Field label={t('parties.startDate')}>
      {!editing ? (
        <div className="flex">
          <button
            ref={line}
            type="button"
            onClick={open}
            title={t('parties.changeStartDate')}
            className="-mx-2 rounded-md px-2 py-1 text-start text-base text-ink hover:bg-fill focus-visible:outline-2 focus-visible:outline-brand"
          >
            {formatDay(startDate)}
            <span className="sr-only">{t('parties.changeStartDate')}</span>
          </button>
        </div>
      ) : (
        <InsetForm labelledBy={headingId} onSubmit={save} onKeyDown={onKeyDown} returnFocus={line}>
          <h3 id={headingId} className="sr-only">
            {t('parties.changeStartDate')}
          </h3>
          <JalaliDateField
            label={t('parties.changeStartDate')}
            value={day}
            onChange={(picked) => {
              setDay(picked);
              setError(undefined);
            }}
            years={{ from: year - startYearsBack, to: year }}
            newestFirst
            error={error}
            disabled={busy}
            // The line it opened from is gone while it is open; the focus starts in it instead.
            autoFocus
          />
          <div className="flex flex-wrap gap-3">
            <Button type="submit" busy={busy}>
              {t('parties.form.save')}
            </Button>
            <Button variant="secondary" onClick={() => setEditing(false)} disabled={busy}>
              {t('parties.form.cancel')}
            </Button>
          </div>
        </InsetForm>
      )}
    </Field>
  );
}
