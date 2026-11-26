import type { ReactNode } from 'react';
import { Link } from 'react-router';
import { buttonClass } from './buttonStyle';

/**
 * A link to another page that looks like a button, such as «مدل جدید». A link and not a button,
 * so the browser's own ways of following a link (a new tab, the address on hover) still work.
 */
export function ButtonLink({
  to,
  variant = 'primary',
  children,
}: {
  to: string;
  variant?: 'primary' | 'secondary';
  children: ReactNode;
}) {
  return (
    <Link to={to} className={buttonClass(variant)}>
      {children}
    </Link>
  );
}

/** A quiet link in the workshop's colour: «بازگشت به فهرست», «تاریخچه». */
export function TextLink({ to, children }: { to: string; children: ReactNode }) {
  return (
    <Link
      to={to}
      className="rounded-sm text-sm text-brand hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand"
    >
      {children}
    </Link>
  );
}
