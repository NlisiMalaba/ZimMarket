import { Pressable, StyleSheet } from 'react-native';
import { router } from 'expo-router';

import { Text, View } from '@/components/Themed';

export default function SplashScreen() {
  return (
    <View style={styles.container}>
      <Text style={styles.brand}>ZimMarket</Text>
      <Text style={styles.tagline}>Fresh groceries delivered quickly and safely.</Text>

      <Pressable style={styles.primaryButton} onPress={() => router.push('/(auth)/register')}>
        <Text style={styles.primaryButtonText}>Get started</Text>
      </Pressable>

      <Pressable onPress={() => router.push('/(auth)/login')}>
        <Text style={styles.secondaryAction}>Already have an account? Login</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: 'center',
    paddingHorizontal: 24,
    gap: 20,
  },
  brand: {
    fontSize: 34,
    fontWeight: '800',
    textAlign: 'center',
  },
  tagline: {
    textAlign: 'center',
    fontSize: 16,
    lineHeight: 24,
  },
  primaryButton: {
    backgroundColor: '#0f766e',
    borderRadius: 12,
    paddingVertical: 14,
    alignItems: 'center',
  },
  primaryButtonText: {
    color: '#ffffff',
    fontSize: 16,
    fontWeight: '700',
  },
  secondaryAction: {
    textAlign: 'center',
    fontSize: 14,
    color: '#0f766e',
    fontWeight: '600',
  },
});
