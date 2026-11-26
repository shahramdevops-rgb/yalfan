// A few inline icons; an icon library is not worth its weight for the shell.

const common = {
  xmlns: 'http://www.w3.org/2000/svg',
  viewBox: '0 0 24 24',
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 1.8,
  strokeLinecap: 'round',
  strokeLinejoin: 'round',
  'aria-hidden': true,
} as const;

export function MenuIcon({ className = 'size-6' }: { className?: string }) {
  return (
    <svg {...common} className={className}>
      <path d="M4 6h16M4 12h16M4 18h16" />
    </svg>
  );
}

export function CloseIcon({ className = 'size-6' }: { className?: string }) {
  return (
    <svg {...common} className={className}>
      <path d="M6 6l12 12M18 6L6 18" />
    </svg>
  );
}

export function HomeIcon({ className = 'size-5' }: { className?: string }) {
  return (
    <svg {...common} className={className}>
      <path d="M4 11l8-7 8 7v8a1 1 0 0 1-1 1h-4v-6h-6v6H5a1 1 0 0 1-1-1z" />
    </svg>
  );
}

export function LogoutIcon({ className = 'size-5' }: { className?: string }) {
  return (
    <svg {...common} className={className}>
      <path d="M14 4h4a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1h-4M9 8l-4 4 4 4M5 12h10" />
    </svg>
  );
}

export function BagMark({ className = 'size-9' }: { className?: string }) {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32" className={className} aria-hidden>
      <rect width="32" height="32" rx="8" className="fill-brand" />
      <path
        d="M9 11h14l-1.5 13h-11z"
        fill="none"
        className="stroke-on-brand"
        strokeWidth="2.2"
        strokeLinejoin="round"
      />
      <path
        d="M12.5 11a3.5 3.5 0 0 1 7 0"
        fill="none"
        className="stroke-on-brand"
        strokeWidth="2.2"
        strokeLinecap="round"
      />
    </svg>
  );
}

export function ListIcon({ className = 'size-5' }: { className?: string }) {
  return (
    <svg {...common} className={className}>
      <path d="M9 6h11M9 12h11M9 18h11M4.5 6h.01M4.5 12h.01M4.5 18h.01" />
    </svg>
  );
}

export function StagesIcon({ className = 'size-5' }: { className?: string }) {
  return (
    <svg {...common} className={className}>
      <path d="M4 7h4v4H4zM10 13h4v4h-4zM16 7h4v4h-4zM8 9h2.5a1.5 1.5 0 0 1 1.5 1.5V13M14 15h.5a1.5 1.5 0 0 0 1.5-1.5V11" />
    </svg>
  );
}

/** The models: a bag. */
export function BagIcon({ className = 'size-5' }: { className?: string }) {
  return (
    <svg {...common} className={className}>
      <path d="M5 8h14l-1.3 11.1a1 1 0 0 1-1 .9H7.3a1 1 0 0 1-1-.9zM9 8a3 3 0 0 1 6 0" />
    </svg>
  );
}

/** The contractors: one person, and the work going out of the workshop to him. */
export function ContractorsIcon({ className = 'size-5' }: { className?: string }) {
  return (
    <svg {...common} className={className}>
      <path d="M9.5 11a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7zM3 20a6.5 6.5 0 0 1 10.5-5.1M15.5 18h6M18.5 15l3 3-3 3" />
    </svg>
  );
}

/** The users who log in to the panel: a key. */
export function KeyIcon({ className = 'size-5' }: { className?: string }) {
  return (
    <svg {...common} className={className}>
      <circle cx="8" cy="15" r="4" />
      <path d="M10.8 12.2 20 3M17 6l3 3M14.5 8.5 17 11" />
    </svg>
  );
}

/** The in-house workers: people working together. */
export function WorkersIcon({ className = 'size-5' }: { className?: string }) {
  return (
    <svg {...common} className={className}>
      <path d="M9 11a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7zM2.5 20a6.5 6.5 0 0 1 13 0M16 4.3a3.5 3.5 0 0 1 0 6.4M18 14.4a6.5 6.5 0 0 1 3.5 5.6" />
    </svg>
  );
}

export function CheckIcon({ className = 'size-4' }: { className?: string }) {
  return (
    <svg {...common} className={className} strokeWidth={2.4}>
      <path d="M5 12.5l4.5 4.5L19 7.5" />
    </svg>
  );
}

export function PlusIcon({ className = 'size-5' }: { className?: string }) {
  return (
    <svg {...common} className={className}>
      <path d="M12 5v14M5 12h14" />
    </svg>
  );
}

export function SearchIcon({ className = 'size-5' }: { className?: string }) {
  return (
    <svg {...common} className={className}>
      <path d="M11 18a7 7 0 1 0 0-14 7 7 0 0 0 0 14zM20 20l-4-4" />
    </svg>
  );
}

/** The handle a row is dragged by. */
export function GripIcon({ className = 'size-5' }: { className?: string }) {
  return (
    <svg {...common} className={className} strokeWidth={3}>
      <path d="M9 6h.01M15 6h.01M9 12h.01M15 12h.01M9 18h.01M15 18h.01" />
    </svg>
  );
}
