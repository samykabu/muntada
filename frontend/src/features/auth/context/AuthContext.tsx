import { createContext, useContext, useState, useCallback, type ReactNode } from 'react';
import { useRefreshMutation, useLogoutMutation } from '../api/authApi';

interface AuthState {
  accessToken: string | null;
  userId: string | null;
  isAuthenticated: boolean;
}

interface AuthContextType extends AuthState {
  setAuth: (token: string, userId: string) => void;
  clearAuth: () => void;
  refresh: () => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | null>(null);

/**
 * Provides authentication state and token management to the app.
 * Access tokens are kept in memory only (not localStorage) to reduce XSS risk.
 * Refresh tokens are HTTP-only cookies managed by the server.
 * UserId is persisted in sessionStorage for page reload survival.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({
    accessToken: null, // In-memory only — not persisted (XSS protection)
    userId: sessionStorage.getItem('userId'),
    isAuthenticated: !!sessionStorage.getItem('userId'),
  });

  const [refreshToken] = useRefreshMutation();
  const [logoutMutation] = useLogoutMutation();

  const setAuth = useCallback((token: string, userId: string) => {
    sessionStorage.setItem('userId', userId);
    setState({ accessToken: token, userId, isAuthenticated: true });
  }, []);

  const clearAuth = useCallback(() => {
    sessionStorage.removeItem('userId');
    setState({ accessToken: null, userId: null, isAuthenticated: false });
  }, []);

  const refresh = useCallback(async () => {
    try {
      const result = await refreshToken().unwrap();
      setState((prev) => ({ ...prev, accessToken: result.accessToken }));
    } catch {
      clearAuth();
    }
  }, [refreshToken, clearAuth]);

  const logout = useCallback(async () => {
    try {
      await logoutMutation().unwrap();
    } finally {
      clearAuth();
    }
  }, [logoutMutation, clearAuth]);

  return (
    <AuthContext.Provider value={{ ...state, setAuth, clearAuth, refresh, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

/** Hook to access authentication state and actions. */
export function useAuthContext() {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuthContext must be used within AuthProvider');
  return context;
}
