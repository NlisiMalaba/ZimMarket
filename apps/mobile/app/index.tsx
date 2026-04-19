import { Redirect } from 'expo-router';

import { useAuth } from '@/hooks/useAuth';

const normalizeRole = (role: string | undefined): string => {
  if (!role) {
    return '';
  }

  return role.trim().toLowerCase();
};

export default function RootIndexScreen() {
  const { isHydrated, isAuthenticated, user } = useAuth();

  if (!isHydrated) {
    return null;
  }

  if (!isAuthenticated) {
    return <Redirect href="/(auth)/splash" />;
  }

  const role = normalizeRole(typeof user?.role === 'string' ? user.role : undefined);

  if (role === 'seller') {
    return <Redirect href="/(seller)" />;
  }

  if (role === 'driver') {
    return <Redirect href="/(driver)" />;
  }

  return <Redirect href="/(customer)" />;
}
