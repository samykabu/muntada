import { createContext, useContext, useState, useCallback } from 'react';
import type { ReactNode } from 'react';

/** Minimal tenant info kept in context. */
export interface TenantInfo {
  id: string;
  name: string;
  slug: string;
}

interface TenantContextType {
  currentTenant: TenantInfo | null;
  setCurrentTenant: (tenant: TenantInfo | null) => void;
  switchTenant: (id: string) => void;
  isLoading: boolean;
}

const TenantContext = createContext<TenantContextType | null>(null);

/**
 * Provides current-tenant state to the app.
 * Persists the selected tenant ID in sessionStorage for page-reload survival.
 */
export function TenantProvider({ children }: { children: ReactNode }) {
  const [currentTenant, setCurrentTenantState] = useState<TenantInfo | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const setCurrentTenant = useCallback((tenant: TenantInfo | null) => {
    setCurrentTenantState(tenant);
    if (tenant) {
      sessionStorage.setItem('currentTenantId', tenant.id);
    } else {
      sessionStorage.removeItem('currentTenantId');
    }
  }, []);

  const switchTenant = useCallback((id: string) => {
    setIsLoading(true);
    sessionStorage.setItem('currentTenantId', id);
    // Clear stale data; consuming components using useGetTenantQuery(id) will refetch.
    setCurrentTenantState(null);
    setIsLoading(false);
  }, []);

  return (
    <TenantContext.Provider value={{ currentTenant, setCurrentTenant, switchTenant, isLoading }}>
      {children}
    </TenantContext.Provider>
  );
}

/**
 * Hook to access the current tenant state and actions.
 * Must be used within a TenantProvider.
 */
export function useTenant() {
  const context = useContext(TenantContext);
  if (!context) throw new Error('useTenant must be used within a TenantProvider');
  return context;
}
