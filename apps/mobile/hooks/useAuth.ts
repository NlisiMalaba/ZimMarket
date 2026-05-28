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
  kycStatus: ReturnType<typeof useAuthStore.getState>['kycStatus'];
};

export const useAuth = (): UseAuthResult => {
  const accessToken = useAuthStore((state) => state.accessToken);
  const user = useAuthStore((state) => state.user);
  const kycStatus = useAuthStore((state) => state.kycStatus);
  const isHydrated = useAuthStore((state) => state.isHydrated);
  const isAuthLoading = useAuthStore((state) => state.isAuthLoading);
  const authError = useAuthStore((state) => state.authError);
  const login = useAuthStore((state) => state.login);
  const logout = useAuthStore((state) => state.logout);
  const register = useAuthStore((state) => state.register);
  const updateProfile = useAuthStore((state) => state.updateProfile);
  const clearAuthError = useAuthStore((state) => state.clearAuthError);

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
      kycStatus,
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
      kycStatus,
    ]
  );
};
