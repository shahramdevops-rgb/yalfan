import { useEffect, useState, type FormEvent } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router';
import { SplashScreen } from '../../app/SplashScreen';
import { fieldCode, type Problem } from '../../shared/api/problem';
import { digitsOnly, formatCountdown, toPersianDigits } from '../../shared/format/digits';
import { errorMessage, t } from '../../shared/i18n/fa';
import { Button } from '../../shared/ui/Button';
import { BagMark } from '../../shared/ui/icons';
import { TextField } from '../../shared/ui/TextField';
import { developmentLoginCode, login, requestLoginCode } from './authApi';
import { useSession } from './useSession';

type Step = { name: 'mobile' } | { name: 'code'; mobile: string; resendAt: number };

/** The message for a refused request, preferring the error of the step's own field. */
const messageFor = (problem: Problem, field: string) => errorMessage(fieldCode(problem, field) ?? problem.code);

/**
 * Two steps, one field each: the mobile number, then the code from the SMS (ADR 0012). There is
 * no password to forget.
 */
export function LoginPage() {
  const session = useSession();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from ?? '/';

  const [step, setStep] = useState<Step>({ name: 'mobile' });
  const [mobile, setMobile] = useState('');
  const [code, setCode] = useState('');
  const [error, setError] = useState<string>();
  const [busy, setBusy] = useState(false);
  // Development only, until the SMS provider is connected: the code, shown at the top.
  const [shownCode, setShownCode] = useState<string>();

  if (session.status === 'loading') {
    return <SplashScreen />;
  }

  if (session.status === 'signedIn') {
    return <Navigate to={from} replace />;
  }

  async function sendCode(number: string) {
    setBusy(true);
    setError(undefined);
    const result = await requestLoginCode(number);
    setBusy(false);

    if (!result.ok) {
      setError(messageFor(result.problem, 'mobileNumber'));
      return;
    }

    setCode('');
    setStep({ name: 'code', mobile: number, resendAt: Date.now() + result.value.resendAfterSeconds * 1000 });
    if (import.meta.env.DEV) {
      setShownCode(await developmentLoginCode(number));
    }
  }

  async function submitCode(event: FormEvent, number: string) {
    event.preventDefault();
    const digits = digitsOnly(code);
    if (digits.length !== 6) {
      setError(t('login.codeFormat'));
      return;
    }

    setBusy(true);
    setError(undefined);
    const result = await login(number, digits);
    setBusy(false);

    if (result.ok) {
      // Nothing to wait for: a failed navigation is shown by the router's error page.
      void navigate(from, { replace: true });
    } else {
      setError(messageFor(result.problem, 'code'));
    }
  }

  return (
    <main className="flex min-h-dvh items-center justify-center px-4 py-10">
      {import.meta.env.DEV && step.name === 'code' && shownCode && (
        <div
          role="status"
          className="fixed inset-x-0 top-0 z-10 bg-warning-fill px-4 py-3 text-center text-warning-strong"
        >
          <p className="text-lg font-bold">
            {t('login.devCode')} <span dir="ltr">{toPersianDigits(shownCode)}</span>
          </p>
          <p className="text-xs">{t('login.devCodeNote')}</p>
        </div>
      )}
      <div className="w-full max-w-sm">
        <div className="mb-8 flex flex-col items-center gap-3 text-center">
          <BagMark className="size-14" />
          <div>
            <p className="text-2xl font-bold text-ink">{t('app.name')}</p>
            <p className="text-sm text-body">{t('app.subtitle')}</p>
          </div>
        </div>

        <div className="rounded-2xl bg-surface p-6 shadow-sm ring-1 ring-line">
          <h1 className="mb-5 text-lg font-bold">{t('login.title')}</h1>

          {step.name === 'mobile' ? (
            <form
              onSubmit={(event) => {
                event.preventDefault();
                void sendCode(digitsOnly(mobile));
              }}
              className="flex flex-col gap-5"
              noValidate
            >
              <TextField
                label={t('login.mobileLabel')}
                hint={t('login.mobileHint')}
                error={error}
                value={mobile}
                onChange={(event) => setMobile(event.target.value)}
                inputMode="tel"
                autoComplete="tel"
                maxLength={20}
                autoFocus
                required
              />
              <Button type="submit" busy={busy} disabled={digitsOnly(mobile).length === 0}>
                {t('login.sendCode')}
              </Button>
            </form>
          ) : (
            <form onSubmit={(event) => void submitCode(event, step.mobile)} className="flex flex-col gap-5" noValidate>
              <p className="text-sm leading-6 text-body">
                {t('login.codeSentTo', { mobile: toPersianDigits(step.mobile) })}
              </p>
              <TextField
                label={t('login.codeLabel')}
                error={error}
                value={code}
                onChange={(event) => setCode(event.target.value)}
                inputMode="numeric"
                autoComplete="one-time-code"
                maxLength={12}
                autoFocus
                required
              />
              <Button type="submit" busy={busy} disabled={digitsOnly(code).length === 0}>
                {t('login.submit')}
              </Button>
              <div className="flex items-center justify-between">
                <ResendButton resendAt={step.resendAt} disabled={busy} onResend={() => void sendCode(step.mobile)} />
                <Button
                  variant="link"
                  onClick={() => {
                    setError(undefined);
                    setShownCode(undefined);
                    setStep({ name: 'mobile' });
                  }}
                >
                  {t('login.changeMobile')}
                </Button>
              </div>
            </form>
          )}
        </div>
      </div>
    </main>
  );
}

/** Counts down the wait the API asks for between two codes (2 minutes, ADR 0012). */
function ResendButton({ resendAt, disabled, onResend }: { resendAt: number; disabled: boolean; onResend: () => void }) {
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(timer);
  }, [resendAt]);

  const secondsLeft = (resendAt - now) / 1000;
  if (secondsLeft > 0) {
    return <span className="text-sm text-muted">{t('login.resendIn', { time: formatCountdown(secondsLeft) })}</span>;
  }

  return (
    <Button variant="link" disabled={disabled} onClick={onResend}>
      {t('login.resend')}
    </Button>
  );
}
