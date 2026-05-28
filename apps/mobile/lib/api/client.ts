import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';

import { env } from '@/lib/config/env';
import { navigateToLogin } from '@/lib/navigation/auth-navigation';
import { authService } from '@/lib/services/auth-service';
import { useAuthStore } from '@/store/auth-store';

type RetryableRequestConfig = InternalAxiosRequestConfig & {
  _retry?: boolean;
};

const REFRESH_ENDPOINT = '/auth/refresh';

const api = axios.create({
  baseURL: env.EXPO_PUBLIC_API_BASE_URL,
  timeout: 15000,
});

let refreshPromise: Promise<string | null> | null = null;

const refreshAccessToken = async (): Promise<string | null> => {
  const { accessToken, refreshToken, clearAuth, setSession } = useAuthStore.getState();

  if (!accessToken || !refreshToken) {
    clearAuth();
    return null;
  }

  try {
    const response = await authService.refresh({ accessToken, refreshToken });
    const newAccessToken = response.accessToken;
    const newRefreshToken = response.refreshToken ?? refreshToken;
    setSession({
      accessToken: newAccessToken,
      refreshToken: newRefreshToken,
      kycStatus: response.kycStatus,
    });

    return newAccessToken;
  } catch {
    clearAuth();
    return null;
  }
};

api.interceptors.request.use(
  (config) => {
    const nextConfig = config;
    const { accessToken } = useAuthStore.getState();

    if (accessToken) {
      nextConfig.headers.set('Authorization', `Bearer ${accessToken}`);
    }

    return nextConfig;
  },
  (error: unknown) => Promise.reject(error)
);

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as RetryableRequestConfig | undefined;
    const { clearAuth } = useAuthStore.getState();

    if (
      error.response?.status !== 401 ||
      !originalRequest ||
      originalRequest._retry ||
      originalRequest.url?.includes(REFRESH_ENDPOINT)
    ) {
      return Promise.reject(error);
    }

    originalRequest._retry = true;

    try {
      refreshPromise ??= refreshAccessToken().finally(() => {
        refreshPromise = null;
      });

      const newAccessToken = await refreshPromise;

      if (!newAccessToken) {
        clearAuth();
        navigateToLogin();
        return Promise.reject(error);
      }

      originalRequest.headers.set('Authorization', `Bearer ${newAccessToken}`);
      return api.request(originalRequest);
    } catch (refreshError) {
      clearAuth();
      navigateToLogin();
      return Promise.reject(refreshError);
    }
  }
);

export { api };
