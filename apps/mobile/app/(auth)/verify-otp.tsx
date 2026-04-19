import { useMemo, useState } from 'react';
import { Pressable, StyleSheet, TextInput } from 'react-native';
import { router, useLocalSearchParams } from 'expo-router';

import { Text, View } from '@/components/Themed';
import { useAuth } from '@/hooks/useAuth';

const readParam = (value: string | string[] | undefined): string => {
  if (Array.isArray(value)) {
    return value[0] ?? '';
  }

  return value ?? '';
};

export default function VerifyOtpScreen() {
  const { register, isLoading, clearError, error } = useAuth();
  const [otp, setOtp] = useState('');
  const [screenError, setScreenError] = useState<string | null>(null);
  const params = useLocalSearchParams();

  const registrationPayload = useMemo(
    () => ({
      email: readParam(params.email).trim().toLowerCase(),
      phone: readParam(params.phone).trim(),
      password: readParam(params.password),
      name: readParam(params.name).trim(),
      role: 'Customer',
    }),
    [params.email, params.phone, params.password, params.name]
  );

  const handleVerify = async () => {
    clearError();
    setScreenError(null);

    if (registrationPayload.email.length === 0 || registrationPayload.password.length === 0) {
      setScreenError('Registration details are missing. Please restart registration.');
      return;
    }

    if (!/^\d{6}$/.test(otp.trim())) {
      setScreenError('Enter the 6-digit OTP sent to your phone.');
      return;
    }

    try {
      await register(registrationPayload);
      router.replace('/(customer)');
    } catch {
      // Store-level auth error already contains a user-friendly message.
    }
  };

  return (
    <View style={styles.container}>
      <Text style={styles.heading}>Verify your phone</Text>
      <Text style={styles.subtitle}>Enter the 6-digit code sent to {registrationPayload.phone}.</Text>

      <TextInput
        style={styles.input}
        value={otp}
        placeholder="123456"
        keyboardType="number-pad"
        maxLength={6}
        onChangeText={setOtp}
      />

      {screenError ? <Text style={styles.error}>{screenError}</Text> : null}
      {error ? <Text style={styles.error}>{error}</Text> : null}

      <Pressable style={[styles.primaryButton, isLoading ? styles.disabledButton : null]} onPress={handleVerify} disabled={isLoading}>
        <Text style={styles.primaryButtonText}>{isLoading ? 'Creating account...' : 'Verify and continue'}</Text>
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
  subtitle: {
    fontSize: 14,
    lineHeight: 20,
  },
  input: {
    borderWidth: 1,
    borderColor: '#d4d4d8',
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 10,
    fontSize: 20,
    letterSpacing: 8,
    textAlign: 'center',
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
});
