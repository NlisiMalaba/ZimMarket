import { Redirect } from 'expo-router';

import { useAuth } from '@/hooks/useAuth';
import { resolveSellerOnboardingRoute } from '@/lib/seller-kyc';
import { useAuthStore } from '@/store/auth-store';

const normalizeRole = (role: string | undefined): string => {
  if (!role) {
    return '';
  }

  return role.trim().toLowerCase();
};

export default function RootIndexScreen() {
  const { isHydrated, isAuthenticated, user } = useAuth();
  const kycStatus = useAuthStore((state) => state.kycStatus);

  if (!isHydrated) {
    return null;
  }

  if (!isAuthenticated) {
    return <Redirect href="/(auth)/splash" />;
  }

  const role = normalizeRole(typeof user?.role === 'string' ? user.role : undefined);

  if (role === 'seller') {
    return <Redirect href={resolveSellerOnboardingRoute(kycStatus)} />;
  }

  if (role === 'driver') {
    return <Redirect href="/(driver)" />;
  }

  return <Redirect href="/(customer)" />;
}
