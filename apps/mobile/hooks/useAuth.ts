import { useMemo } from 'react';

import { useAuthStore } from '@/store/auth-store';
import type { LoginRequest, RegisterRequest } from '@/types/auth';

type UseAuthResult = {
  login: (payload: LoginRequest) => Promise<void>;
  logout: () => Promise<void>;
  register: (payload: RegisterRequest) => Promise<void>;
  isAuthenticated: boolean;
  isHydrated: boolean;
  isLoading: boolean;
  error: string | null;
  clearError: () => void;
  updateProfile: (payload: { name?: string; phone?: string }) => void;
  user: ReturnType<typeof useAuthStore.getState>['user'];
};

export const useAuth = (): UseAuthResult => {
  const {
    accessToken,
    user,
    isHydrated,
    isAuthLoading,
    authError,
    login,
    logout,
    register,
    updateProfile,
    clearAuthError,
  } = useAuthStore((state) => ({
    accessToken: state.accessToken,
    user: state.user,
    isHydrated: state.isHydrated,
    isAuthLoading: state.isAuthLoading,
    authError: state.authError,
    login: state.login,
    logout: state.logout,
    register: state.register,
    updateProfile: state.updateProfile,
    clearAuthError: state.clearAuthError,
  }));

  return useMemo(
    () => ({
      login,
      logout,
      register,
      updateProfile,
      isAuthenticated: Boolean(accessToken),
      isHydrated,
      isLoading: isAuthLoading,
      error: authError,
      clearError: clearAuthError,
      user,
    }),
    [
      login,
      logout,
      register,
      updateProfile,
      accessToken,
      isHydrated,
      isAuthLoading,
      authError,
      clearAuthError,
      user,
    ]
  );
};
