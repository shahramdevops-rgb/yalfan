import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router';
import { toProblem } from '../../shared/api/problem';
import { dayInTehran } from '../../shared/format/dates';
import { toPersianDigits } from '../../shared/format/digits';
import { ibanDigitCount } from '../../shared/format/iban';
import { toJalali } from '../../shared/format/jalali';
import { errorMessage, t } from '../../shared/i18n/fa';
import { Button } from '../../shared/ui/Button';
import { FormCard } from '../../shared/ui/Card';
import { FieldError, FormActions, FormFields } from '../../shared/ui/Form';
import { IbanField } from '../../shared/ui/IbanField';
import { JalaliDateField } from '../../shared/ui/JalaliDateField';
import { Notice } from '../../shared/ui/Notice';
import { PageHeader } from '../../shared/ui/PageHeader';
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
      <PageHeader title={t(`parties.${kind}.new`)} back={{ to: `/${kind}`, label: t(`parties.${kind}.back`) }} />

      <FormCard onSubmit={submit} gap={6}>
        <FormFields>
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
        </FormFields>

        <fieldset className="flex flex-col gap-2">
          <legend className="text-sm font-medium text-label">{t('parties.stages')}</legend>
          <p className="text-sm text-muted">{t(`parties.${kind}.stagesHint`)}</p>
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
          {errors.stageIds && <FieldError>{errors.stageIds}</FieldError>}
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

        <FormActions>
          {errors.form && <Notice tone="danger">{errors.form}</Notice>}
          <Button type="submit" busy={saving} className="self-start">
            {t(`parties.${kind}.save`)}
          </Button>
        </FormActions>
      </FormCard>
    </div>
  );
}
