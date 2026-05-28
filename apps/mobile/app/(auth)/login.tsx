import { useMemo, useState } from 'react';
import { Pressable, StyleSheet, TextInput } from 'react-native';
import { router } from 'expo-router';

import { Text, View } from '@/components/Themed';
import { resolveSellerOnboardingRoute } from '@/lib/seller-kyc';
import { useAuth } from '@/hooks/useAuth';
import { useAuthStore } from '@/store/auth-store';

const isValidEmail = (value: string): boolean => /\S+@\S+\.\S+/.test(value);

const normalizeRole = (role: string | undefined): string => role?.trim().toLowerCase() ?? '';

export default function LoginScreen() {
  const { login, isLoading, error, clearError } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [screenError, setScreenError] = useState<string | null>(null);

  const canSubmit = useMemo(
    () => isValidEmail(email.trim()) && password.length >= 8,
    [email, password]
  );

  const handleSubmit = async () => {
    clearError();
    setScreenError(null);

    if (!canSubmit) {
      setScreenError('Enter a valid email and password (at least 8 characters).');
      return;
    }

    try {
      await login({
        email: email.trim().toLowerCase(),
        password,
      });

      const { user, kycStatus } = useAuthStore.getState();
      const role = normalizeRole(typeof user?.role === 'string' ? user.role : undefined);

      if (role === 'seller') {
        router.replace(resolveSellerOnboardingRoute(kycStatus) as never);
        return;
      }

      if (role === 'driver') {
        router.replace('/(driver)' as never);
        return;
      }

      router.replace('/(customer)' as never);
    } catch {
      // Auth store surfaces the error message.
    }
  };

  return (
    <View style={styles.container}>
      <Text style={styles.heading}>Sign in</Text>
      <Text style={styles.description}>Access your ZimMarket account.</Text>

      <TextInput
        style={styles.input}
        value={email}
        placeholder="Email address"
        keyboardType="email-address"
        autoCapitalize="none"
        onChangeText={(value) => {
          setEmail(value);
          if (screenError) {
            setScreenError(null);
          }
        }}
      />
      <TextInput
        style={styles.input}
        value={password}
        placeholder="Password"
        secureTextEntry
        autoCapitalize="none"
        onChangeText={(value) => {
          setPassword(value);
          if (screenError) {
            setScreenError(null);
          }
        }}
      />

      {screenError ? <Text style={styles.error}>{screenError}</Text> : null}
      {error ? <Text style={styles.error}>{error}</Text> : null}

      <Pressable
        style={[styles.primaryButton, (!canSubmit || isLoading) ? styles.disabledButton : null]}
        onPress={handleSubmit}
        disabled={!canSubmit || isLoading}
      >
        <Text style={styles.primaryButtonText}>{isLoading ? 'Signing in...' : 'Sign in'}</Text>
      </Pressable>

      <Pressable onPress={() => router.push('/(auth)/register')}>
        <Text style={styles.link}>New here? Create a customer account</Text>
      </Pressable>
      <Pressable onPress={() => router.push('/(auth)/register-seller' as never)}>
        <Text style={styles.link}>Register as a seller</Text>
      </Pressable>
      <Pressable onPress={() => router.push('/(auth)/register-driver' as never)}>
        <Text style={styles.link}>Register as a driver</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 24,
    gap: 12,
    justifyContent: 'center',
  },
  heading: {
    fontSize: 24,
    fontWeight: '700',
  },
  description: {
    color: '#334155',
    marginBottom: 8,
  },
  input: {
    borderWidth: 1,
    borderColor: '#d4d4d8',
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 10,
    fontSize: 16,
  },
  error: {
    color: '#dc2626',
    fontWeight: '500',
  },
  primaryButton: {
    backgroundColor: '#0f766e',
    borderRadius: 12,
    paddingVertical: 14,
    alignItems: 'center',
    marginTop: 8,
  },
  disabledButton: {
    opacity: 0.6,
  },
  primaryButtonText: {
    color: '#ffffff',
    fontSize: 16,
    fontWeight: '700',
  },
  link: {
    color: '#0f766e',
    fontWeight: '600',
    textAlign: 'center',
  },
});
