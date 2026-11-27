import { useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { NavLink, Outlet, useLocation } from 'react-router';
import { logout } from '../features/auth/authApi';
import { useSession } from '../features/auth/useSession';
import { formatWeekdayDate } from '../shared/format/dates';
import { t, type MessageKey } from '../shared/i18n/fa';
import { Drawer } from '../shared/ui/Dialog';
import {
  BagIcon,
  BagMark,
  CloseIcon,
  ContractorsIcon,
  HomeIcon,
  KeyIcon,
  ListIcon,
  LogoutIcon,
  MenuIcon,
  StagesIcon,
  WorkersIcon,
} from '../shared/ui/icons';

// The sidebar grows one item per module as the phases land (docs/ROADMAP.md).
const navigation: { to: string; label: MessageKey; icon: typeof HomeIcon; ownerOnly?: boolean }[] = [
  { to: '/', label: 'nav.dashboard', icon: HomeIcon },
  { to: '/models', label: 'nav.models', icon: BagIcon },
  { to: '/contractors', label: 'nav.contractors', icon: ContractorsIcon },
  { to: '/workers', label: 'nav.workers', icon: WorkersIcon },
  { to: '/lists', label: 'nav.lists', icon: ListIcon },
  { to: '/stages', label: 'nav.stages', icon: StagesIcon },
  // Only an owner manages users (ADR 0012); the others do not see the item at all.
  { to: '/users', label: 'nav.users', icon: KeyIcon, ownerOnly: true },
];

/** Tailwind's `lg`, where the sidebar stands beside the page and the menu button goes. */
const desktopWidth = '(min-width: 64rem)';

/**
 * The frame of every page behind the login. The sidebar sits on the inline start, which is the
 * right in a right-to-left page; on a phone it becomes a drawer, because the owner checks the
 * panel on his phone.
 */
export function AppLayout() {
  const location = useLocation();

  // The drawer stays open only on the page where it was opened, so a tap on one of its links
  // closes it by itself, without an effect and its extra render.
  const [menuOpenedOn, setMenuOpenedOn] = useState<string | null>(null);
  const menuOpen = menuOpenedOn === location.pathname;
  const closeMenu = () => setMenuOpenedOn(null);

  // A phone or tablet turned sideways to the desktop's width shows the sidebar itself: the drawer
  // closes, so it does not stay over a page it leaves inert.
  useEffect(() => {
    if (!menuOpen) {
      return;
    }

    const wide = window.matchMedia(desktopWidth);
    const closeWhenWide = () => {
      if (wide.matches) {
        setMenuOpenedOn(null);
      }
    };
    wide.addEventListener('change', closeWhenWide);
    return () => wide.removeEventListener('change', closeWhenWide);
  }, [menuOpen]);

  return (
    <div className="min-h-dvh lg:flex">
      <aside className="hidden w-64 shrink-0 border-e border-line bg-surface lg:block">
        <Sidebar />
      </aside>

      {/* The drawer is the browser's own dialog (ADR 0030): Escape and a tap beside it close it,
          the focus stays inside it, and goes back to the menu button when it closes. */}
      {menuOpen && (
        <Drawer label={t('nav.menu')} onClose={closeMenu}>
          <div className="relative h-full">
            <button
              type="button"
              className="absolute end-3 top-4 rounded-lg p-2 text-muted hover:bg-fill focus-visible:outline-2 focus-visible:outline-brand"
              aria-label={t('nav.closeMenu')}
              onClick={closeMenu}
            >
              <CloseIcon />
            </button>
            <Sidebar />
          </div>
        </Drawer>
      )}

      <div className="flex min-w-0 flex-1 flex-col">
        <Header onOpenMenu={() => setMenuOpenedOn(location.pathname)} />
        <main className="flex-1 px-4 py-6 lg:px-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

function Sidebar() {
  const session = useSession();
  const isOwner = session.status === 'signedIn' && session.user.role === 'owner';
  const items = navigation.filter((item) => !item.ownerOnly || isOwner);

  return (
    <nav className="flex h-full flex-col gap-6 p-4">
      <div className="flex items-center gap-3 px-2 py-1">
        <BagMark />
        <div>
          <p className="leading-tight font-bold">{t('app.name')}</p>
          <p className="text-xs text-muted">{t('app.subtitle')}</p>
        </div>
      </div>
      <ul className="flex flex-col gap-1">
        {items.map(({ to, label, icon: Icon }) => (
          <li key={to}>
            <NavLink
              to={to}
              // Only the dashboard needs an exact match; a module stays marked on its inner pages.
              end={to === '/'}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-lg px-3 py-2.5 text-base transition-colors ${
                  isActive ? 'bg-brand-soft font-medium text-brand-strong' : 'text-label hover:bg-fill'
                }`
              }
            >
              <Icon />
              {t(label)}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  );
}

function Header({ onOpenMenu }: { onOpenMenu: () => void }) {
  const session = useSession();
  const queryClient = useQueryClient();
  const [leaving, setLeaving] = useState(false);
  const user = session.status === 'signedIn' ? session.user : null;

  async function signOut() {
    setLeaving(true);
    await logout();
    // Nothing of this user's data stays in memory for the next one on the same PC.
    queryClient.clear();
  }

  return (
    <header className="flex h-16 items-center gap-3 border-b border-line bg-surface px-4 lg:px-8">
      <button
        type="button"
        className="-ms-2 rounded-lg p-2 text-body hover:bg-fill lg:hidden"
        aria-label={t('nav.openMenu')}
        onClick={onOpenMenu}
      >
        <MenuIcon />
      </button>

      <p className="hidden text-sm text-muted sm:block">{t('header.today', { date: formatWeekdayDate(new Date()) })}</p>

      <div className="ms-auto flex items-center gap-3">
        {user && (
          <div className="text-end leading-tight">
            <p className="text-sm font-medium">{user.fullName}</p>
            <p className="text-xs text-muted">{t(`role.${user.role}`)}</p>
          </div>
        )}
        <button
          type="button"
          onClick={() => void signOut()}
          disabled={leaving}
          className="flex items-center gap-1.5 rounded-lg px-3 py-2 text-sm text-body ring-1 ring-line hover:bg-subtle disabled:opacity-50"
        >
          <LogoutIcon />
          <span className="hidden sm:inline">{t('header.logout')}</span>
        </button>
      </div>
    </header>
  );
}
