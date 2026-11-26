import type { ReactNode } from 'react';
import { TextLink } from './Links';

interface PageHeaderProps {
  /**
   * The page's one `h1`. Left out on a record's own page (a model, a contractor), where the
   * record's name, edited where it stands in the card below, is the `h1` (`InlineName heading`).
   */
  title?: string;
  /** For a list on the page that is named by the title (`aria-labelledby`). */
  titleId?: string;
  /** One sentence under the title saying what the page is for. */
  hint?: string;
  /** The page this one was opened from: «بازگشت به فهرست». */
  back?: { to: string; label: string };
  /** At the inline end: the page's own button («مدل جدید») or link («تاریخچه»). */
  actions?: ReactNode;
}

/**
 * The top of every page inside the panel's frame, so each page starts the same way and has its
 * one `h1` (docs/design/UI.md).
 */
export function PageHeader({ title, titleId, hint, back, actions }: PageHeaderProps) {
  const backLink = back && <TextLink to={back.to}>{back.label}</TextLink>;

  if (title === undefined) {
    return (
      <header className="flex items-center justify-between gap-4">
        {backLink}
        {actions}
      </header>
    );
  }

  return (
    <header>
      {backLink}
      <div className={`flex flex-wrap items-center justify-between gap-3 ${back ? 'mt-1' : ''}`}>
        <div>
          <h1 id={titleId} className="text-2xl font-bold">
            {title}
          </h1>
          {hint && <p className="mt-1 text-muted">{hint}</p>}
        </div>
        {actions}
      </div>
    </header>
  );
}
