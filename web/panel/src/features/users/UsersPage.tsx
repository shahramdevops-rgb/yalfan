import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useId, useRef, useState, type FormEvent } from 'react';
import { NotFoundPage } from '../../app/NotFoundPage';
import { upsert } from '../../shared/api/listCache';
import { toProblem } from '../../shared/api/problem';
import { attempt } from '../../shared/api/refusal';
import { inTurn } from '../../shared/api/serialWrites';
import { toPersianDigits } from '../../shared/format/digits';
import { formatMobile } from '../../shared/format/mobile';
import { errorMessage, t } from '../../shared/i18n/fa';
import { Button } from '../../shared/ui/Button';
import { Card, CardHeader, FormCard } from '../../shared/ui/Card';
import { Chip } from '../../shared/ui/Chip';
import { Field, FieldError, FormActions, FormFields } from '../../shared/ui/Form';
import { InlineName } from '../../shared/ui/InlineName';
import { Notice } from '../../shared/ui/Notice';
import { PageHeader } from '../../shared/ui/PageHeader';
import { ListSkeleton, LoadError } from '../../shared/ui/QueryStates';
import { StatusBadge } from '../../shared/ui/StatusBadge';
import { Switch } from '../../shared/ui/Switch';
import { TextField } from '../../shared/ui/TextField';
import { logout } from '../auth/authApi';
import { useSession } from '../auth/useSession';
import { endsOwnSessions, numberLock, placeUserRefusal, staffRoles, type UserField } from './users';
import { usersApi, usersKey, type LoginNumber, type StaffRole, type StaffUser } from './usersApi';

/** The longest name the API takes (User.FullNameMaxLength). */
const nameMaxLength = 100;

/** Sends one change of a user; answers the message to show when it is refused, or nothing. */
type Save = (send: () => Promise<StaffUser>, field?: string) => Promise<string | undefined>;

/**
 * The owner's users screen (ADR 0012, ADR 0026): every user a card, with no save button. The name
 * is edited where it stands, the role is two chips, a manager's or operator's number is changed
 * by tapping it, and switches turn a user, or one of an owner account's numbers, off and on. Only
 * an owner sees it; the API refuses everyone else as well.
 */
export function UsersPage() {
  const session = useSession();
  const me = session.status === 'signedIn' ? session.user : null;
  if (me?.role !== 'owner') {
    return <NotFoundPage />;
  }

  return <Users currentUserId={me.id} />;
}

function Users({ currentUserId }: { currentUserId: string }) {
  const queryClient = useQueryClient();
  const users = useQuery({ queryKey: usersKey, queryFn: usersApi.list });
  // One change after another: each answers the whole user, so the answer drawn last must be the
  // change saved last, or a quick rename and switch could show the switch undone.
  const queue = useRef<Promise<unknown>>(Promise.resolve());

  const store = (user: StaffUser) => queryClient.setQueryData<StaffUser[]>(usersKey, (list) => upsert(list, user));

  const save: Save = (send, field) => attempt(() => inTurn(queue, send), store, field);

  // Switching off a number of one's own account ends one's own sessions too (ADR 0026): the
  // page goes to the login at once instead of failing a few minutes later.
  async function signOutHere() {
    await logout();
    queryClient.clear();
  }

  return (
    <div className="flex max-w-3xl flex-col gap-6">
      <PageHeader title={t('users.title')} hint={t('users.hint')} />

      {users.isPending && <ListSkeleton rows={4} />}
      {users.isError && <LoadError error={users.error} onRetry={() => void users.refetch()} />}
      {users.data?.map((user) => (
        <UserCard
          key={user.id}
          user={user}
          isCurrentUser={user.id === currentUserId}
          save={save}
          onOwnSessionsEnded={() => void signOutHere()}
          endsOwnSessions={endsOwnSessions(user, currentUserId)}
        />
      ))}

      {users.data && <NewUserForm onCreated={store} />}
    </div>
  );
}

interface UserCardProps {
  user: StaffUser;
  isCurrentUser: boolean;
  save: Save;
  endsOwnSessions: boolean;
  onOwnSessionsEnded: () => void;
}

function UserCard({ user, isCurrentUser, save, endsOwnSessions, onOwnSessionsEnded }: UserCardProps) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();

  async function run(send: () => Promise<StaffUser>) {
    setBusy(true);
    const refused = await save(send);
    setError(refused);
    setBusy(false);
    return refused;
  }

  async function switchNumber(number: LoginNumber, isActive: boolean) {
    const refused = await run(() => usersApi.setNumberActive(user.id, number.id, isActive));
    if (!refused && !isActive && endsOwnSessions) {
      onOwnSessionsEnded();
    }
  }

  const mobile = user.loginNumbers[0]?.mobileNumber ?? '';

  return (
    <Card>
      <div className="flex items-start gap-4">
        <div className="flex min-w-0 flex-1 flex-wrap items-center gap-2 pt-1">
          <InlineName
            value={user.fullName}
            muted={!user.isActive}
            maxLength={nameMaxLength}
            onSave={(fullName) => save(() => usersApi.rename(user.id, fullName), 'fullName')}
          />
          {isCurrentUser && <StatusBadge>{t('users.you')}</StatusBadge>}
          {!user.isActive && <StatusBadge>{t('list.inactive')}</StatusBadge>}
        </div>
        {!user.isOwnerAccount && (
          <div className="flex shrink-0 flex-col items-center gap-1 pt-1">
            <span className="text-xs text-muted">{t('list.columnActive')}</span>
            <Switch
              checked={user.isActive}
              onChange={(isActive) => void run(() => usersApi.setActive(user.id, isActive))}
              label={t('list.activeLabel', { name: user.fullName })}
              disabled={busy}
            />
          </div>
        )}
      </div>

      {!user.isActive && <Notice tone="warning">{t('users.inactiveNote')}</Notice>}

      <Field label={t('users.role')}>
        {user.isOwnerAccount ? (
          <>
            <p className="text-base text-ink">{t('role.owner')}</p>
            <p className="text-sm text-muted">{t('users.ownerNote')}</p>
          </>
        ) : (
          <div className="flex flex-wrap gap-2">
            {staffRoles.map((role) => (
              <Chip
                key={role}
                label={t(`role.${role}`)}
                on={user.role === role}
                onToggle={() => {
                  if (user.role !== role) {
                    void run(() => usersApi.changeRole(user.id, role));
                  }
                }}
                disabled={busy}
              />
            ))}
          </div>
        )}
      </Field>

      {user.isOwnerAccount ? (
        // The hint only where a number can be switched off: an account with one number has none.
        <Field label={t('users.numbers')} hint={user.loginNumbers.length > 1 ? t('users.numbersHint') : undefined}>
          <ul className="divide-y divide-line-soft">
            {user.loginNumbers.map((number) => (
              <NumberRow
                key={number.id}
                number={number}
                lock={numberLock(user, number, isCurrentUser ? user.id : undefined)}
                disabled={busy}
                onChange={(isActive) => void switchNumber(number, isActive)}
              />
            ))}
          </ul>
        </Field>
      ) : (
        <Field label={t('users.mobile')}>
          {/* A row, so the left-to-right number sits at the start of the line, not across it. */}
          <div className="flex">
            <InlineName
              numeric
              // Persian digits in the field, as everywhere; the API reads them as the same number.
              value={toPersianDigits(mobile)}
              display={formatMobile(mobile)}
              label={t('users.mobile')}
              maxLength={20}
              hint={t('users.mobileChangeHint', { name: user.fullName })}
              onSave={(mobileNumber) => save(() => usersApi.changeMobile(user.id, mobileNumber), 'mobileNumber')}
            />
          </div>
        </Field>
      )}

      {error && <FieldError>{error}</FieldError>}
    </Card>
  );
}

interface NumberRowProps {
  number: LoginNumber;
  lock: 'last' | 'own' | undefined;
  disabled: boolean;
  onChange: (isActive: boolean) => void;
}

function NumberRow({ number, lock, disabled, onChange }: NumberRowProps) {
  const shown = formatMobile(number.mobileNumber);
  return (
    <li className="flex min-h-12 items-center gap-3 py-1.5">
      <div className="flex min-w-0 flex-1 flex-col">
        <div className="flex items-center gap-2">
          <span dir="ltr" className={`text-base ${number.isActive ? 'text-ink' : 'text-faint'}`}>
            {shown}
          </span>
          {!number.isActive && <StatusBadge>{t('users.numberOff')}</StatusBadge>}
        </div>
        {lock && (
          <span className="text-xs text-muted">{t(lock === 'last' ? 'users.lastNumber' : 'users.ownNumber')}</span>
        )}
      </div>
      <Switch
        checked={number.isActive}
        onChange={onChange}
        label={t('users.numberLabel', { mobile: shown })}
        disabled={disabled || lock !== undefined}
      />
    </li>
  );
}

type Errors = Partial<Record<UserField | 'form', string>>;

/**
 * A new manager or operator: name, number and role in one save. The role starts at operator, the
 * smaller power, so a slip gives less than was meant rather than more.
 */
function NewUserForm({ onCreated }: { onCreated: (user: StaffUser) => void }) {
  const queryClient = useQueryClient();
  const [fullName, setFullName] = useState('');
  const [mobileNumber, setMobileNumber] = useState('');
  const [role, setRole] = useState<StaffRole>('operator');
  const [errors, setErrors] = useState<Errors>({});
  const [saving, setSaving] = useState(false);
  const [added, setAdded] = useState<string>();
  const titleId = useId();

  function check(): Errors {
    const found: Errors = {};
    if (fullName.trim() === '') {
      found.fullName = t('users.nameMissing');
    }

    if (mobileNumber.trim() === '') {
      found.mobileNumber = t('users.mobileMissing');
    }

    return found;
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    setAdded(undefined);
    const found = check();
    setErrors(found);
    if (Object.keys(found).length > 0) {
      return;
    }

    setSaving(true);
    try {
      const created = await usersApi.create({ fullName: fullName.trim(), mobileNumber, role });
      onCreated(created);
      // The list is sorted by the API; the new user is fetched into his place.
      void queryClient.invalidateQueries({ queryKey: usersKey });
      setFullName('');
      setMobileNumber('');
      setRole('operator');
      setAdded(t('users.added', { name: created.fullName }));
    } catch (error) {
      const placed = placeUserRefusal(toProblem(error));
      setErrors(Object.fromEntries(Object.entries(placed).map(([field, code]) => [field, errorMessage(code)])));
    } finally {
      setSaving(false);
    }
  }

  const clear = (field: UserField) => setErrors((before) => ({ ...before, [field]: undefined }));

  return (
    <FormCard onSubmit={(event) => void submit(event)} labelledBy={titleId}>
      <CardHeader id={titleId} title={t('users.new')} hint={t('users.newHint')} />

      <FormFields>
        <TextField
          label={t('users.fullName')}
          value={fullName}
          onChange={(event) => {
            setFullName(event.target.value);
            clear('fullName');
          }}
          error={errors.fullName}
          maxLength={nameMaxLength}
          autoComplete="off"
        />
        <TextField
          label={t('users.mobile')}
          hint={t('users.mobileHint')}
          value={mobileNumber}
          onChange={(event) => {
            setMobileNumber(event.target.value);
            clear('mobileNumber');
          }}
          error={errors.mobileNumber}
          dir="ltr"
          inputMode="tel"
          autoComplete="off"
          maxLength={20}
        />
        <Field label={t('users.role')}>
          <div className="flex flex-wrap gap-2">
            {staffRoles.map((choice) => (
              <Chip
                key={choice}
                label={t(`role.${choice}`)}
                on={role === choice}
                onToggle={() => {
                  setRole(choice);
                  clear('role');
                }}
              />
            ))}
          </div>
          {errors.role && <FieldError>{errors.role}</FieldError>}
        </Field>
      </FormFields>

      <FormActions>
        {errors.form && <Notice tone="danger">{errors.form}</Notice>}
        {added && (
          <p role="status" className="text-sm text-success">
            {added}
          </p>
        )}
        <Button type="submit" busy={saving} className="self-start">
          {t('users.add')}
        </Button>
      </FormActions>
    </FormCard>
  );
}
