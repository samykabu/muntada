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

/** Provides authentication state and token management to the app. */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({
    accessToken: localStorage.getItem('accessToken'),
    userId: localStorage.getItem('userId'),
    isAuthenticated: !!localStorage.getItem('accessToken'),
  });

  const [refreshToken] = useRefreshMutation();
  const [logoutMutation] = useLogoutMutation();

  const setAuth = useCallback((token: string, userId: string) => {
    localStorage.setItem('accessToken', token);
    localStorage.setItem('userId', userId);
    setState({ accessToken: token, userId, isAuthenticated: true });
  }, []);

  const clearAuth = useCallback(() => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('userId');
    setState({ accessToken: null, userId: null, isAuthenticated: false });
  }, []);

  const refresh = useCallback(async () => {
    try {
      const result = await refreshToken().unwrap();
      setState((prev) => ({ ...prev, accessToken: result.accessToken }));
      localStorage.setItem('accessToken', result.accessToken);
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
