import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useEffect } from 'react';
import { createBrowserRouter, Navigate } from 'react-router';
import { RouterProvider } from 'react-router/dom';
import { LoginPage } from '../features/auth/LoginPage';
import { CatalogListsPage } from '../features/catalog/CatalogListsPage';
import { ModelPage } from '../features/catalog/ModelPage';
import { ModelsPage } from '../features/catalog/ModelsPage';
import { NewModelPage } from '../features/catalog/NewModelPage';
import { DashboardPage } from '../features/dashboard/DashboardPage';
import { HistoryPage } from '../features/history/HistoryPage';
import { NewPartyPage } from '../features/parties/NewPartyPage';
import { PartyListPage } from '../features/parties/PartyListPage';
import { PartyPage } from '../features/parties/PartyPage';
import { StagesPage } from '../features/production/StagesPage';
import { UsersPage } from '../features/users/UsersPage';
import { sessionStore } from '../shared/api/client';
import { Toaster } from '../shared/ui/Toaster';
import { AppLayout } from './AppLayout';
import { ErrorPage } from './ErrorPage';
import { NotFoundPage } from './NotFoundPage';
import { RequireSession } from './RequireSession';

// No path here may start with /panel/: those requests go to the API (ADR 0014).
const router = createBrowserRouter([
  { path: '/login', element: <LoginPage />, errorElement: <ErrorPage /> },
  {
    element: <RequireSession />,
    errorElement: <ErrorPage />,
    children: [
      {
        element: <AppLayout />,
        children: [
          { index: true, element: <DashboardPage /> },
          { path: 'models', element: <ModelsPage /> },
          { path: 'models/new', element: <NewModelPage /> },
          { path: 'models/:id', element: <ModelPage /> },
          // Owner only, like the users screen: the change history of a record (ADR 0027).
          { path: 'models/:id/history', element: <HistoryPage key="models" kind="models" /> },
          // The key gives each list its own page state: without it, going from one list to the
          // other would keep the search box's text and send it to the other list.
          { path: 'contractors', element: <PartyListPage key="contractors" kind="contractors" /> },
          { path: 'contractors/new', element: <NewPartyPage key="contractors" kind="contractors" /> },
          { path: 'contractors/:id', element: <PartyPage key="contractors" kind="contractors" /> },
          { path: 'contractors/:id/history', element: <HistoryPage key="contractors" kind="contractors" /> },
          { path: 'workers', element: <PartyListPage key="workers" kind="workers" /> },
          { path: 'workers/new', element: <NewPartyPage key="workers" kind="workers" /> },
          { path: 'workers/:id', element: <PartyPage key="workers" kind="workers" /> },
          { path: 'workers/:id/history', element: <HistoryPage key="workers" kind="workers" /> },
          { path: 'lists', element: <Navigate to="/lists/fabrics" replace /> },
          { path: 'lists/:kind', element: <CatalogListsPage /> },
          { path: 'stages', element: <StagesPage /> },
          // Owner only: the page shows «not found» to anyone else, and the API refuses them.
          { path: 'users', element: <UsersPage /> },
          { path: '*', element: <NotFoundPage /> },
        ],
      },
    ],
  },
]);

const queryClient = new QueryClient({
  defaultOptions: {
    // An office PC on a slow line: one retry, and no refetch on every window switch.
    queries: { retry: 1, refetchOnWindowFocus: false },
  },
});

export function App() {
  useEffect(() => {
    // After a reload the access token is gone (it lives in memory only); the refresh cookie
    // brings a new one, or the user lands on the login page.
    if (sessionStore.getState().status === 'loading') {
      void sessionStore.renew();
    }
  }, []);

  return (
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
      <Toaster />
    </QueryClientProvider>
  );
}
