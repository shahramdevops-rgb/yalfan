import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useEffect } from 'react';
import { createBrowserRouter } from 'react-router';
import { RouterProvider } from 'react-router/dom';
import { LoginPage } from '../features/auth/LoginPage';
import { DashboardPage } from '../features/dashboard/DashboardPage';
import { sessionStore } from '../shared/api/client';
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
    </QueryClientProvider>
  );
}
