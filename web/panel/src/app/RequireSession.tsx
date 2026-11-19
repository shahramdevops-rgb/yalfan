import { Navigate, Outlet, useLocation } from 'react-router';
import { useSession } from '../features/auth/useSession';
import { SplashScreen } from './SplashScreen';

/** Every page behind it needs a session; without one the user goes to the login page and back. */
export function RequireSession() {
  const session = useSession();
  const location = useLocation();

  if (session.status === 'loading') {
    return <SplashScreen />;
  }

  if (session.status === 'signedOut') {
    return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />;
  }

  return <Outlet />;
}
