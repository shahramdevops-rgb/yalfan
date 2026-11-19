import { useSyncExternalStore } from 'react';
import { sessionStore } from '../../shared/api/client';
import type { SessionState } from '../../shared/session/sessionStore';

/** The session of this tab; components re-render when it changes. */
export function useSession(): SessionState {
  return useSyncExternalStore(sessionStore.subscribe, sessionStore.getState);
}
