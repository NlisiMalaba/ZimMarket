import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';

import { navigateToLogin } from '@/lib/navigation/auth-navigation';
import { authService } from '@/lib/services/auth-service';
import { secureStorage } from '@/lib/storage/secure-storage';
import type { AuthResponse, AuthUser, LoginRequest, RegisterRequest } from '@/types/auth';

type AuthState = {
  accessToken: string | null;
  refreshToken: string | null;
  user: AuthUser | null;
  isHydrated: boolean;
  isAuthLoading: boolean;
  authError: string | null;
  setSession: (session: Partial<AuthResponse>) => void;
  clearAuth: () => void;
  setHydrated: (hydrated: boolean) => void;
  clearAuthError: () => void;
  login: (payload: LoginRequest) => Promise<void>;
  register: (payload: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
};

const normalizeAuthError = (error: unknown): string => {
  if (error instanceof Error && error.message.length > 0) {
    return error.message;
  }

  return 'Authentication request failed.';
};

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      accessToken: null,
      refreshToken: null,
      user: null,
      isHydrated: false,
      isAuthLoading: false,
      authError: null,

      setSession: ({ accessToken, refreshToken, user }) => {
        set((state) => ({
          accessToken: accessToken ?? state.accessToken,
          refreshToken: refreshToken ?? state.refreshToken,
          user: user ?? state.user,
          authError: null,
        }));
      },

      clearAuth: () => {
        set({
          accessToken: null,
          refreshToken: null,
          user: null,
          authError: null,
        });
      },

      setHydrated: (hydrated) => set({ isHydrated: hydrated }),

      clearAuthError: () => set({ authError: null }),

      login: async (payload) => {
        set({ isAuthLoading: true, authError: null });

        try {
          const session = await authService.login(payload);
          get().setSession(session);
        } catch (error) {
          const message = normalizeAuthError(error);
          set({ authError: message });
          throw error;
        } finally {
          set({ isAuthLoading: false });
        }
      },

      register: async (payload) => {
        set({ isAuthLoading: true, authError: null });

        try {
          const session = await authService.register(payload);
          get().setSession(session);
        } catch (error) {
          const message = normalizeAuthError(error);
          set({ authError: message });
          throw error;
        } finally {
          set({ isAuthLoading: false });
        }
      },

      logout: async () => {
        const { refreshToken } = get();
        set({ isAuthLoading: true, authError: null });

        try {
          if (refreshToken) {
            await authService.logout({ refreshToken });
          }
        } finally {
          get().clearAuth();
          set({ isAuthLoading: false });
          navigateToLogin();
        }
      },
    }),
    {
      name: 'auth-store',
      storage: createJSONStorage(() => secureStorage),
      partialize: (state) =>
        ({
          accessToken: state.accessToken,
          refreshToken: state.refreshToken,
          user: state.user,
        }) satisfies Pick<AuthState, 'accessToken' | 'refreshToken' | 'user'>,
      onRehydrateStorage: () => (state, error) => {
        if (error) {
          console.error('Failed to rehydrate auth store.', error);
        }

        state?.setHydrated(true);
      },
    }
  )
);

export const selectAuthTokens = (
  state: AuthState
): { accessToken: string | null; refreshToken: string | null } => ({
  accessToken: state.accessToken,
  refreshToken: state.refreshToken,
});
