import { useState } from 'react';
import { Pressable, StyleSheet, TextInput } from 'react-native';
import { router } from 'expo-router';

import { Text, View } from '@/components/Themed';

type RegisterFormState = {
  email: string;
  phone: string;
  password: string;
  name: string;
};

const initialFormState: RegisterFormState = {
  email: '',
  phone: '',
  password: '',
  name: '',
};

const isValidEmail = (value: string): boolean => /\S+@\S+\.\S+/.test(value);
const isValidPhone = (value: string): boolean => value.trim().length >= 9;
const isValidPassword = (value: string): boolean => value.length >= 8;

export default function RegisterScreen() {
  const [form, setForm] = useState<RegisterFormState>(initialFormState);
  const [error, setError] = useState<string | null>(null);

  const handleChange = (key: keyof RegisterFormState, value: string) => {
    setForm((previous) => ({ ...previous, [key]: value }));
    if (error) {
      setError(null);
    }
  };

  const handleContinue = () => {
    if (!isValidEmail(form.email)) {
      setError('Enter a valid email address.');
      return;
    }

    if (!isValidPhone(form.phone)) {
      setError('Enter a valid phone number.');
      return;
    }

    if (!isValidPassword(form.password)) {
      setError('Password must be at least 8 characters.');
      return;
    }

    if (form.name.trim().length < 2) {
      setError('Enter your full name.');
      return;
    }

    router.push({
      pathname: '/(auth)/verify-otp',
      params: {
        email: form.email.trim().toLowerCase(),
        phone: form.phone.trim(),
        password: form.password,
        name: form.name.trim(),
      },
    });
  };

  return (
    <View style={styles.container}>
      <Text style={styles.heading}>Create your customer account</Text>

      <TextInput
        style={styles.input}
        value={form.name}
        placeholder="Full name"
        autoCapitalize="words"
        onChangeText={(value) => handleChange('name', value)}
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

      {error ? <Text style={styles.error}>{error}</Text> : null}

      <Pressable style={styles.primaryButton} onPress={handleContinue}>
        <Text style={styles.primaryButtonText}>Continue to verification</Text>
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
  primaryButtonText: {
    color: '#ffffff',
    fontSize: 16,
    fontWeight: '700',
  },
});
