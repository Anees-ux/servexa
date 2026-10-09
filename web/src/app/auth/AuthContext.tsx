import React, { createContext, useContext, useEffect, useState, useCallback } from 'react';
import { api, ApiError } from '../../shared/api/apiClient';
import type { CurrentUserDto } from '../../shared/api/types';

interface AuthContextType {
  user: CurrentUserDto | null;
  isLoading: boolean;
  error: string | null;
  loginDevelopment: (userType?: 'admin' | 'dispatcher') => Promise<void>;
  logout: () => void;
  retryAuth: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<CurrentUserDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const initAuth = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      if (api.getToken()) {
        try {
          const profile = await api.getCurrentUser();
          setUser(profile);
          setIsLoading(false);
          return;
        } catch {
          // Stored token expired or invalid; clear and try dev session
          api.setToken(null);
        }
      }

      // In local dev mode, authenticate with verified development identity
      const auth = await api.loginDevelopmentSession('admin');
      setUser({
        tenantId: auth.tenantId,
        userId: auth.userId,
        email: auth.email,
        displayName: auth.displayName,
        roles: auth.roles,
        permissions: auth.permissions,
        isAuthenticated: true,
      });
    } catch (err: unknown) {
      const msg = err instanceof ApiError
        ? err.message
        : err instanceof Error
          ? err.message
          : 'Authentication failed';
      setError(msg);
      // STRICT: Never fabricate fake authenticated sessions when authentication fails
      setUser(null);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    initAuth();
  }, [initAuth]);

  const loginDevelopment = async (userType: 'admin' | 'dispatcher' = 'admin') => {
    setIsLoading(true);
    setError(null);
    try {
      const auth = await api.loginDevelopmentSession(userType);
      setUser({
        tenantId: auth.tenantId,
        userId: auth.userId,
        email: auth.email,
        displayName: auth.displayName,
        roles: auth.roles,
        permissions: auth.permissions,
        isAuthenticated: true,
      });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Login failed';
      setError(msg);
      setUser(null);
      throw err;
    } finally {
      setIsLoading(false);
    }
  };

  const logout = () => {
    api.setToken(null);
    setUser(null);
    setError(null);
  };

  const retryAuth = () => {
    initAuth();
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        error,
        loginDevelopment,
        logout,
        retryAuth,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
