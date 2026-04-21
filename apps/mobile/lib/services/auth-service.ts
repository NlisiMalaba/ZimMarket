import axios from 'axios';

import { env } from '@/lib/config/env';
import type {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  AuthTokens,
  KycStatus,
  UserRole,
} from '@/types/auth';

const authApi = axios.create({
  baseURL: env.EXPO_PUBLIC_API_BASE_URL,
  timeout: 15000,
});

type ApiEnvelope<T> = {
  data?: T;
};

type RawAuthTokensResponse = {
  accessToken?: string;
  refreshToken?: string;
  kycStatus?: string;
};

const normalizeKycStatus = (value: unknown): KycStatus | undefined => {
  if (typeof value !== 'string') {
    return undefined;
  }

  const normalized = value.trim();
  return normalized.length > 0 ? normalized : undefined;
};

const unwrapAuthTokens = (payload: unknown): Required<Pick<AuthResponse, 'accessToken' | 'refreshToken'>> & Pick<AuthResponse, 'kycStatus'> => {
  const source =
    payload && typeof payload === 'object' && 'data' in payload
      ? (payload as ApiEnvelope<RawAuthTokensResponse>).data
      : (payload as RawAuthTokensResponse | undefined);

  const accessToken = source?.accessToken?.trim();
  const refreshToken = source?.refreshToken?.trim();

  if (!accessToken || !refreshToken) {
    throw new Error('Authentication response is missing tokens.');
  }

  return {
    accessToken,
    refreshToken,
    kycStatus: normalizeKycStatus(source?.kycStatus),
  };
};

const resolveRegisterEndpoint = (role: UserRole | undefined): '/auth/register/customer' | '/auth/register/seller' | '/auth/register/driver' => {
  const normalizedRole = role?.trim().toLowerCase();
  if (normalizedRole === 'seller') {
    return '/auth/register/seller';
  }

  if (normalizedRole === 'driver') {
    return '/auth/register/driver';
  }

  return '/auth/register/customer';
};

const buildAuthError = (message: string, error: unknown): Error => {
  console.error(message, error);
  return new Error(message);
};

export const authService = {
  async login(payload: LoginRequest): Promise<AuthResponse> {
    try {
      const response = await authApi.post<ApiEnvelope<RawAuthTokensResponse>>('/auth/login', payload);
      return unwrapAuthTokens(response.data);
    } catch (error) {
      throw buildAuthError('Login failed. Please verify your credentials.', error);
    }
  },

  async register(payload: RegisterRequest): Promise<AuthResponse> {
    try {
      const endpoint = resolveRegisterEndpoint(payload.role);
      const requestBody =
        endpoint === '/auth/register/seller'
          ? {
              email: payload.email,
              phone: payload.phone,
              password: payload.password,
              fullName: payload.name,
              businessName: payload.businessName,
            }
          : endpoint === '/auth/register/driver'
            ? {
                email: payload.email,
                phone: payload.phone,
                password: payload.password,
                fullName: payload.name,
              }
            : {
                email: payload.email,
                phone: payload.phone,
                password: payload.password,
                fullName: payload.name,
                pushToken: null,
              };

      const response = await authApi.post<ApiEnvelope<RawAuthTokensResponse>>(endpoint, requestBody);
      return unwrapAuthTokens(response.data);
    } catch (error) {
      throw buildAuthError('Registration failed. Please try again.', error);
    }
  },

  async refresh(payload: { accessToken: string; refreshToken: string }): Promise<AuthTokens> {
    try {
      const response = await authApi.post<ApiEnvelope<RawAuthTokensResponse>>('/auth/refresh', payload);
      return unwrapAuthTokens(response.data);
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
