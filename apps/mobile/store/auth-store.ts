import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';

import { getClaimsFromAccessToken } from '@/lib/auth/jwt';
import { navigateToLogin } from '@/lib/navigation/auth-navigation';
import { authService } from '@/lib/services/auth-service';
import { secureStorage } from '@/lib/storage/secure-storage';
import { normalizeKycStatus } from '@/lib/seller-kyc';
import type { AuthResponse, AuthUser, KycStatus, LoginRequest, RegisterRequest } from '@/types/auth';

type AuthState = {
  accessToken: string | null;
  refreshToken: string | null;
  kycStatus: KycStatus | null;
  user: AuthUser | null;
  isHydrated: boolean;
  isAuthLoading: boolean;
  authError: string | null;
  setSession: (session: Partial<AuthResponse>) => void;
  clearAuth: () => void;
  setHydrated: (hydrated: boolean) => void;
  clearAuthError: () => void;
  updateProfile: (payload: Partial<Pick<AuthUser, 'name' | 'phone'>>) => void;
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
      kycStatus: null,
      user: null,
      isHydrated: false,
      isAuthLoading: false,
      authError: null,

      setSession: ({ accessToken, refreshToken, kycStatus, user }) => {
        set((state) => {
          const nextAccessToken = accessToken ?? state.accessToken;
          const claims = nextAccessToken ? getClaimsFromAccessToken(nextAccessToken) : null;
          const resolvedKycStatus =
            kycStatus ??
            (claims?.kycStatus
              ? (normalizeKycStatus(claims.kycStatus) as KycStatus)
              : state.kycStatus);

          const resolvedUser =
            user ??
            (claims && nextAccessToken
              ? {
                  id: claims.userId ?? state.user?.id ?? 'self',
                  email: claims.email ?? state.user?.email ?? '',
                  role: claims.role ?? state.user?.role ?? 'Customer',
                  name: state.user?.name,
                  phone: state.user?.phone,
                }
              : state.user);

          return {
            accessToken: nextAccessToken,
            refreshToken: refreshToken ?? state.refreshToken,
            kycStatus: resolvedKycStatus,
            user: resolvedUser,
            authError: null,
          };
        });
      },

      clearAuth: () => {
        set({
          accessToken: null,
          refreshToken: null,
          kycStatus: null,
          user: null,
          authError: null,
        });
      },

      setHydrated: (hydrated) => set({ isHydrated: hydrated }),

      clearAuthError: () => set({ authError: null }),

      updateProfile: ({ name, phone }) => {
        set((state) => {
          if (!state.user) {
            return state;
          }

          return {
            user: {
              ...state.user,
              name: name ?? state.user.name,
              phone: phone ?? state.user.phone,
            },
          };
        });
      },

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
          get().setSession({
            ...session,
            user:
              session.user ??
              (payload.role
                ? {
                    id: 'self',
                    email: payload.email,
                    role: payload.role,
                    name: payload.name,
                    phone: payload.phone,
                  }
                : undefined),
          });
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
          kycStatus: state.kycStatus,
          user: state.user,
        }) satisfies Pick<AuthState, 'accessToken' | 'refreshToken' | 'kycStatus' | 'user'>,
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
