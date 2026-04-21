import { useMemo, useState } from 'react';
import { Pressable, StyleSheet, TextInput } from 'react-native';
import { router } from 'expo-router';

import { Text, View } from '@/components/Themed';
import { useAuth } from '@/hooks/useAuth';

type RegisterDriverFormState = {
  fullName: string;
  email: string;
  phone: string;
  password: string;
};

const initialFormState: RegisterDriverFormState = {
  fullName: '',
  email: '',
  phone: '',
  password: '',
};

const isValidEmail = (value: string): boolean => /\S+@\S+\.\S+/.test(value);

export default function RegisterDriverScreen() {
  const { register, isLoading, clearError, error } = useAuth();
  const [form, setForm] = useState<RegisterDriverFormState>(initialFormState);
  const [screenError, setScreenError] = useState<string | null>(null);

  const canSubmit = useMemo(
    () =>
      form.fullName.trim().length >= 2 &&
      isValidEmail(form.email.trim()) &&
      form.phone.trim().length >= 9 &&
      form.password.length >= 8,
    [form]
  );

  const handleChange = (key: keyof RegisterDriverFormState, value: string) => {
    setForm((previous) => ({ ...previous, [key]: value }));
    if (screenError) {
      setScreenError(null);
    }
  };

  const handleSubmit = async () => {
    clearError();
    setScreenError(null);

    if (!canSubmit) {
      setScreenError('Please complete all fields with valid details.');
      return;
    }

    try {
      await register({
        role: 'Driver',
        email: form.email.trim().toLowerCase(),
        phone: form.phone.trim(),
        password: form.password,
        name: form.fullName.trim(),
      });
      router.replace('/(driver)/kyc-upload' as never);
    } catch {
      // Store-level auth error is shown below.
    }
  };

  return (
    <View style={styles.container}>
      <Text style={styles.heading}>Create driver account</Text>

      <TextInput
        style={styles.input}
        value={form.fullName}
        placeholder="Full name"
        autoCapitalize="words"
        onChangeText={(value) => handleChange('fullName', value)}
      />
      <TextInput
        style={styles.input}
        value={form.email}
        placeholder="Email address"
        keyboardType="email-address"
        autoCapitalize="none"
        onChangeText={(value) => handleChange('email', value)}
      />
      <TextInput
        style={styles.input}
        value={form.phone}
        placeholder="Phone number"
        keyboardType="phone-pad"
        onChangeText={(value) => handleChange('phone', value)}
      />
      <TextInput
        style={styles.input}
        value={form.password}
        placeholder="Password"
        secureTextEntry
        autoCapitalize="none"
        onChangeText={(value) => handleChange('password', value)}
      />

      {screenError ? <Text style={styles.error}>{screenError}</Text> : null}
      {error ? <Text style={styles.error}>{error}</Text> : null}

      <Pressable
        style={[styles.primaryButton, (!canSubmit || isLoading) ? styles.disabledButton : null]}
        onPress={handleSubmit}
        disabled={!canSubmit || isLoading}
      >
        <Text style={styles.primaryButtonText}>{isLoading ? 'Creating account...' : 'Continue to document upload'}</Text>
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
});
