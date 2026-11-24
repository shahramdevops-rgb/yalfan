import type { ReactNode } from 'react';

/** A white panel holding one part of a page, such as a model's sizes or its rates. */
export function Card({ children, labelledBy }: { children: ReactNode; labelledBy?: string }) {
  return (
    <section
      aria-labelledby={labelledBy}
      className="flex flex-col gap-5 rounded-2xl bg-white p-4 shadow-sm ring-1 ring-stone-200 sm:p-6"
    >
      {children}
    </section>
  );
}
