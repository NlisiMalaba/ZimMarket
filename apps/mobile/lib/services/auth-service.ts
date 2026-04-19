import axios from 'axios';

import { env } from '@/lib/config/env';
import type {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  AuthTokens,
} from '@/types/auth';

const authApi = axios.create({
  baseURL: env.EXPO_PUBLIC_API_BASE_URL,
  timeout: 15000,
});

const buildAuthError = (message: string, error: unknown): Error => {
  console.error(message, error);
  return new Error(message);
};

export const authService = {
  async login(payload: LoginRequest): Promise<AuthResponse> {
    try {
      const response = await authApi.post<AuthResponse>('/auth/login', payload);
      return response.data;
    } catch (error) {
      throw buildAuthError('Login failed. Please verify your credentials.', error);
    }
  },

  async register(payload: RegisterRequest): Promise<AuthResponse> {
    try {
      const response = await authApi.post<AuthResponse>('/auth/register', payload);
      return response.data;
    } catch (error) {
      throw buildAuthError('Registration failed. Please try again.', error);
    }
  },

  async refresh(payload: Pick<AuthTokens, 'refreshToken'>): Promise<AuthTokens> {
    try {
      const response = await authApi.post<AuthTokens>('/auth/refresh', payload);
      return response.data;
    } catch (error) {
      throw buildAuthError('Session refresh failed.', error);
    }
  },

  async logout(payload: Pick<AuthTokens, 'refreshToken'>): Promise<void> {
    try {
      await authApi.post('/auth/logout', payload);
    } catch (error) {
      // Logout failures should not block local token cleanup.
      console.warn('Logout request failed.', error);
    }
  },
};
