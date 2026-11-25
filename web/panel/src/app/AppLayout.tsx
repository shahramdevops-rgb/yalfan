import { useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { NavLink, Outlet, useLocation } from 'react-router';
import { logout } from '../features/auth/authApi';
import { useSession } from '../features/auth/useSession';
import { formatWeekdayDate } from '../shared/format/dates';
import { t, type MessageKey } from '../shared/i18n/fa';
import {
  BagIcon,
  BagMark,
  CloseIcon,
  ContractorsIcon,
  HomeIcon,
  ListIcon,
  LogoutIcon,
  MenuIcon,
  StagesIcon,
  WorkersIcon,
} from '../shared/ui/icons';

// The sidebar grows one item per module as the phases land (docs/ROADMAP.md).
const navigation: { to: string; label: MessageKey; icon: typeof HomeIcon }[] = [
  { to: '/', label: 'nav.dashboard', icon: HomeIcon },
  { to: '/models', label: 'nav.models', icon: BagIcon },
  { to: '/contractors', label: 'nav.contractors', icon: ContractorsIcon },
  { to: '/workers', label: 'nav.workers', icon: WorkersIcon },
  { to: '/lists', label: 'nav.lists', icon: ListIcon },
  { to: '/stages', label: 'nav.stages', icon: StagesIcon },
];

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

  return (
    <div className="min-h-dvh lg:flex">
      <aside className="hidden w-64 shrink-0 border-e border-stone-200 bg-white lg:block">
        <Sidebar />
      </aside>

      {menuOpen && (
        <div className="fixed inset-0 z-40 lg:hidden">
          <button
            type="button"
            className="absolute inset-0 bg-stone-900/40"
            aria-label={t('nav.closeMenu')}
            onClick={closeMenu}
          />
          <aside className="absolute inset-y-0 start-0 w-72 max-w-[85%] bg-white shadow-xl">
            <button
              type="button"
              className="absolute end-3 top-4 rounded-lg p-2 text-stone-500 hover:bg-stone-100"
              aria-label={t('nav.closeMenu')}
              onClick={closeMenu}
            >
              <CloseIcon />
            </button>
            <Sidebar />
          </aside>
        </div>
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
  return (
    <nav className="flex h-full flex-col gap-6 p-4">
      <div className="flex items-center gap-3 px-2 py-1">
        <BagMark />
        <div>
          <p className="leading-tight font-bold">{t('app.name')}</p>
          <p className="text-xs text-stone-500">{t('app.subtitle')}</p>
        </div>
      </div>
      <ul className="flex flex-col gap-1">
        {navigation.map(({ to, label, icon: Icon }) => (
          <li key={to}>
            <NavLink
              to={to}
              // Only the dashboard needs an exact match; a module stays marked on its inner pages.
              end={to === '/'}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-lg px-3 py-2.5 text-base transition-colors ${
                  isActive ? 'bg-brand-50 font-medium text-brand-800' : 'text-stone-700 hover:bg-stone-100'
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
    <header className="flex h-16 items-center gap-3 border-b border-stone-200 bg-white px-4 lg:px-8">
      <button
        type="button"
        className="-ms-2 rounded-lg p-2 text-stone-600 hover:bg-stone-100 lg:hidden"
        aria-label={t('nav.openMenu')}
        onClick={onOpenMenu}
      >
        <MenuIcon />
      </button>

      <p className="hidden text-sm text-stone-500 sm:block">
        {t('header.today', { date: formatWeekdayDate(new Date()) })}
      </p>

      <div className="ms-auto flex items-center gap-3">
        {user && (
          <div className="text-end leading-tight">
            <p className="text-sm font-medium">{user.fullName}</p>
            <p className="text-xs text-stone-500">{t(`role.${user.role}`)}</p>
          </div>
        )}
        <button
          type="button"
          onClick={() => void signOut()}
          disabled={leaving}
          className="flex items-center gap-1.5 rounded-lg px-3 py-2 text-sm text-stone-600 ring-1 ring-stone-200 hover:bg-stone-50 disabled:opacity-50"
        >
          <LogoutIcon />
          <span className="hidden sm:inline">{t('header.logout')}</span>
        </button>
      </div>
    </header>
  );
}
