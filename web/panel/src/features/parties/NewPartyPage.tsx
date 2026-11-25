import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router';
import { toProblem } from '../../shared/api/problem';
import { dayInTehran } from '../../shared/format/dates';
import { toPersianDigits } from '../../shared/format/digits';
import { ibanDigitCount } from '../../shared/format/iban';
import { toJalali } from '../../shared/format/jalali';
import { errorMessage, t } from '../../shared/i18n/fa';
import { Button } from '../../shared/ui/Button';
import { IbanField } from '../../shared/ui/IbanField';
import { JalaliDateField } from '../../shared/ui/JalaliDateField';
import { ListSkeleton, LoadError } from '../../shared/ui/QueryStates';
import { TextAreaField } from '../../shared/ui/TextAreaField';
import { TextField } from '../../shared/ui/TextField';
import { stagesApi, stagesKey } from '../production/stagesApi';
import { partiesApi, partySearchesKey, type PartyKind } from './partiesApi';
import { placeRefusal, startYearsBack, type PartyField } from './partyForm';
import { StagePicker } from './StagePicker';

type Errors = Partial<Record<PartyField | 'form', string>>;

/**
 * A new contractor or worker in one form and one save, as the operator copies them from the paper
 * book: name, mobile, stages, for a worker the start date, and the optional IBAN and notes. A
 * worker's start date starts empty and must be picked, because most workers entered started long
 * before, and a today left in by mistake would count the service and the pay from the wrong day
 * (ADR 0025). After saving, the person's page opens.
 */
export function NewPartyPage({ kind }: { kind: PartyKind }) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const stages = useQuery({ queryKey: stagesKey, queryFn: stagesApi.list });
  const isWorker = kind === 'workers';

  const [fullName, setFullName] = useState('');
  const [mobileNumber, setMobileNumber] = useState('');
  const [stageIds, setStageIds] = useState<string[]>([]);
  const [startDate, setStartDate] = useState<string | null>(null);
  const [iban, setIban] = useState('');
  const [notes, setNotes] = useState('');
  const [errors, setErrors] = useState<Errors>({});
  const [saving, setSaving] = useState(false);
  const year = toJalali(dayInTehran()).year;

  /** What the form can tell before sending; the API checks everything again. */
  function check(): Errors {
    const found: Errors = {};
    if (fullName.trim() === '') {
      found.fullName = t('parties.nameMissing');
    }

    if (mobileNumber.trim() === '') {
      found.mobileNumber = t('parties.mobileMissing');
    }

    if (!isWorker && stageIds.length === 0) {
      found.stageIds = t('parties.stageMissing');
    }

    if (isWorker && startDate === null) {
      found.startDate = t('parties.startDateMissing');
    }

    if (iban !== '' && iban.length !== ibanDigitCount) {
      found.iban = t('iban.incomplete', { count: toPersianDigits(String(iban.length)) });
    }

    return found;
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    const found = check();
    setErrors(found);
    if (Object.keys(found).length > 0) {
      return;
    }

    setSaving(true);
    try {
      const created = await partiesApi[kind].create({
        fullName,
        mobileNumber,
        stageIds,
        notes: notes.trim() || null,
        iban: iban ? `IR${iban}` : null,
        // Only a worker's form has a start date; a contractor's stays null and is not sent.
        startDate: startDate ?? undefined,
      });
      void queryClient.invalidateQueries({ queryKey: partySearchesKey(kind) });
      void navigate(`/${kind}/${created.id}`, { replace: true });
    } catch (error) {
      const placed = placeRefusal(toProblem(error));
      setErrors(Object.fromEntries(Object.entries(placed).map(([field, code]) => [field, errorMessage(code)])));
      setSaving(false);
    }
  }

  // Each change clears its own message, so a corrected field stops saying what was wrong.
  const clear = (field: PartyField) => setErrors((before) => ({ ...before, [field]: undefined }));

  return (
    <div className="flex max-w-3xl flex-col gap-6">
      <div>
        <Link to={`/${kind}`} className="text-sm text-brand-700 hover:underline">
          {t(`parties.${kind}.back`)}
        </Link>
        <h1 className="mt-1 text-2xl font-bold">{t(`parties.${kind}.new`)}</h1>
      </div>

      <form
        onSubmit={(event) => void submit(event)}
        noValidate
        className="flex flex-col gap-6 rounded-2xl bg-white p-4 shadow-sm ring-1 ring-stone-200 sm:p-6"
      >
        <div className="flex flex-col gap-5 sm:max-w-md">
          <TextField
            label={t('parties.fullName')}
            value={fullName}
            onChange={(event) => {
              setFullName(event.target.value);
              clear('fullName');
            }}
            error={errors.fullName}
            maxLength={100}
            autoComplete="off"
            autoFocus
          />
          <TextField
            label={t('parties.mobile')}
            hint={t('parties.mobileHint')}
            value={mobileNumber}
            onChange={(event) => {
              setMobileNumber(event.target.value);
              clear('mobileNumber');
            }}
            error={errors.mobileNumber}
            dir="ltr"
            inputMode="tel"
            maxLength={20}
            autoComplete="off"
          />
        </div>

        <fieldset className="flex flex-col gap-2">
          <legend className="text-sm font-medium text-stone-700">{t('parties.stages')}</legend>
          <p className="text-sm text-stone-500">{t(`parties.${kind}.stagesHint`)}</p>
          {stages.isPending && <ListSkeleton rows={1} />}
          {stages.isError && <LoadError error={stages.error} onRetry={() => void stages.refetch()} />}
          {stages.data && (
            <StagePicker
              stages={stages.data}
              ticked={stageIds}
              onChange={(ticked) => {
                setStageIds(ticked);
                clear('stageIds');
              }}
            />
          )}
          {errors.stageIds && (
            <p role="alert" className="text-sm text-red-700">
              {errors.stageIds}
            </p>
          )}
        </fieldset>

        {isWorker && (
          <JalaliDateField
            label={t('parties.startDate')}
            value={startDate}
            onChange={(day) => {
              setStartDate(day);
              clear('startDate');
            }}
            years={{ from: year - startYearsBack, to: year }}
            newestFirst
            hint={t('parties.startDateHint')}
            error={errors.startDate}
          />
        )}

        <IbanField
          label={t('iban.field')}
          value={iban}
          onChange={(digits) => {
            setIban(digits);
            clear('iban');
          }}
          hint={t('iban.optionalHint')}
          error={errors.iban}
        />

        <div className="sm:max-w-xl">
          <TextAreaField
            label={t('parties.notes')}
            hint={t('parties.notesHint')}
            value={notes}
            onChange={(event) => {
              setNotes(event.target.value);
              clear('notes');
            }}
            error={errors.notes}
            maxLength={500}
          />
        </div>

        <div className="flex flex-col gap-3 border-t border-stone-100 pt-5">
          {errors.form && (
            <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-800">
              {errors.form}
            </p>
          )}
          <Button type="submit" busy={saving} className="self-start">
            {t(`parties.${kind}.save`)}
          </Button>
        </div>
      </form>
    </div>
  );
}
