export type UserRole = 'Customer' | 'Seller' | 'Driver' | 'Admin' | string;

export type AuthUser = {
  id: string;
  email: string;
  role: UserRole;
  name?: string;
  [key: string]: unknown;
};

export type AuthTokens = {
  accessToken: string;
  refreshToken: string;
};

export type LoginRequest = {
  email: string;
  password: string;
};

export type RegisterRequest = {
  email: string;
  password: string;
  name?: string;
  role?: UserRole;
};

export type AuthResponse = {
  accessToken: string;
  refreshToken: string;
  user: AuthUser;
};
