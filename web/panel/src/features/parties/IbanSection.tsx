import { useId, useState, type FormEvent, type KeyboardEvent } from 'react';
import { refusalMessage } from '../../shared/api/refusal';
import { toPersianDigits } from '../../shared/format/digits';
import { formatIban, ibanDigitCount, ibanLastFour } from '../../shared/format/iban';
import { t } from '../../shared/i18n/fa';
import { Button } from '../../shared/ui/Button';
import { Card, CardHeader } from '../../shared/ui/Card';
import { InsetForm } from '../../shared/ui/Form';
import { IbanField } from '../../shared/ui/IbanField';
import { useSession } from '../auth/useSession';
import { partiesApi, type PartyDetails, type PartyKind, type Write } from './partiesApi';

interface IbanSectionProps {
  kind: PartyKind;
  party: PartyDetails;
  /** Sends a change in turn with the rest of the page's changes. */
  write: Write;
}

/**
 * A contractor's or a worker's IBAN (ADR 0019, ADR 0025). Every screen shows only its last four
 * digits; a tap opens a small form to set or replace it, and to remove it. The owner alone can
 * show the whole number, because he pays by hand until Phase 15; it is kept in this card only,
 * never in the panel's cache, and is gone when he leaves the page or the IBAN is changed here.
 */
export function IbanSection({ kind, party, write }: IbanSectionProps) {
  const headingId = useId();
  const session = useSession();
  const isOwner = session.status === 'signedIn' && session.user.role === 'owner';
  const [editing, setEditing] = useState(false);
  // Counts the changes made here; a new count draws the whole number afresh, so one on show
  // never outlives the IBAN it belongs to, even when the new one ends in the same four digits.
  const [changes, setChanges] = useState(0);

  const masked = party.maskedIban;
  const changed: Write = async (send) => {
    const details = await write(send);
    setChanges((count) => count + 1);
    return details;
  };

  return (
    <Card labelledBy={headingId}>
      <CardHeader id={headingId} title={t('iban.title')} hint={t('iban.hint')} />

      <div className="flex flex-wrap items-center gap-x-4 gap-y-2">
        <button
          type="button"
          onClick={() => setEditing(true)}
          aria-expanded={editing}
          title={masked ? t('iban.change') : t('iban.noneTap')}
          className="-mx-2 rounded-md px-2 py-1 text-start hover:bg-fill focus-visible:outline-2 focus-visible:outline-brand"
        >
          {masked ? (
            <>
              <span dir="ltr" className="text-lg font-medium tracking-wider text-ink">
                {`•••• ${ibanLastFour(masked)}`}
              </span>
              <span className="sr-only">{t('iban.change')}</span>
            </>
          ) : (
            <span className="flex flex-col">
              <span className="font-medium text-warning">{t('iban.none')}</span>
              <span className="text-xs text-muted">{t('iban.noneTap')}</span>
            </span>
          )}
        </button>
        {masked && isOwner && <WholeIban key={changes} kind={kind} partyId={party.id} />}
      </div>

      {editing && <IbanForm kind={kind} party={party} write={changed} onClose={() => setEditing(false)} />}
    </Card>
  );
}

/** The owner's «نمایش کامل»: one request, logged by the API, kept in this component's state only. */
function WholeIban({ kind, partyId }: { kind: PartyKind; partyId: string }) {
  const [whole, setWhole] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string>();

  async function toggle() {
    setMessage(undefined);
    if (whole) {
      setWhole(null);
      return;
    }

    setBusy(true);
    try {
      setWhole(await partiesApi[kind].wholeIban(partyId));
    } catch (error) {
      setMessage(refusalMessage(error));
    } finally {
      setBusy(false);
    }
  }

  async function copy() {
    if (!whole) {
      return;
    }

    try {
      // Latin digits with no spaces: what a bank's app takes when it is pasted.
      await navigator.clipboard.writeText(whole);
      setMessage(t('iban.copied'));
    } catch {
      setMessage(t('iban.copyFailed'));
    }
  }

  return (
    <>
      <Button variant="link" busy={busy} onClick={() => void toggle()} aria-expanded={whole !== null}>
        {whole ? t('iban.hide') : t('iban.show')}
      </Button>
      {whole && (
        <div className="flex w-full flex-col gap-2 rounded-lg bg-subtle p-3">
          <div className="flex flex-wrap items-center gap-3">
            <span dir="ltr" className="text-lg font-medium tracking-wide text-ink select-all">
              {formatIban(whole)}
            </span>
            <Button variant="secondary" className="h-10" onClick={() => void copy()}>
              {t('iban.copy')}
            </Button>
          </div>
          <p className="text-xs text-muted">{t('iban.wholeNote')}</p>
        </div>
      )}
      {message && (
        <p role="status" className="w-full text-sm text-body">
          {message}
        </p>
      )}
    </>
  );
}

function IbanForm({ kind, party, write, onClose }: IbanSectionProps & { onClose: () => void }) {
  const headingId = useId();
  const [digits, setDigits] = useState('');
  const [error, setError] = useState<string>();
  const [busy, setBusy] = useState<'save' | 'remove' | null>(null);
  const hasIban = party.maskedIban != null;

  async function send(request: () => Promise<PartyDetails>, which: 'save' | 'remove') {
    setBusy(which);
    setError(undefined);
    try {
      await write(request);
      onClose();
    } catch (refused) {
      setError(refusalMessage(refused, 'iban'));
      setBusy(null);
    }
  }

  function save(event: FormEvent) {
    event.preventDefault();
    if (digits.length !== ibanDigitCount) {
      setError(t('iban.incomplete', { count: toPersianDigits(String(digits.length)) }));
      return;
    }

    void send(() => partiesApi[kind].setIban(party.id, `IR${digits}`), 'save');
  }

  function onKeyDown(event: KeyboardEvent<HTMLFormElement>) {
    if (event.key === 'Escape' && busy === null) {
      event.preventDefault();
      onClose();
    }
  }

  return (
    <InsetForm labelledBy={headingId} onSubmit={save} onKeyDown={onKeyDown}>
      <h3 id={headingId} className="font-bold">
        {hasIban ? t('iban.changeTitle') : t('iban.newTitle')}
      </h3>
      <IbanField
        label={t('iban.field')}
        value={digits}
        onChange={(typed) => {
          setDigits(typed);
          setError(undefined);
        }}
        hint={hasIban ? `${t('iban.fieldHint')} ${t('iban.replaceNote')}` : t('iban.fieldHint')}
        error={error}
        autoFocus
        disabled={busy !== null}
        focusOnError
      />
      <div className="flex flex-wrap gap-3">
        <Button type="submit" busy={busy === 'save'} disabled={digits === '' || busy !== null}>
          {t('iban.save')}
        </Button>
        <Button variant="secondary" onClick={onClose} disabled={busy !== null}>
          {t('parties.form.cancel')}
        </Button>
        {hasIban && (
          <Button
            variant="danger"
            className="ms-auto"
            busy={busy === 'remove'}
            disabled={busy !== null}
            onClick={() => void send(() => partiesApi[kind].removeIban(party.id), 'remove')}
          >
            {t('iban.remove')}
          </Button>
        )}
      </div>
    </InsetForm>
  );
}
