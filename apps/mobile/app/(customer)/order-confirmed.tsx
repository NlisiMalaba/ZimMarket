import { Pressable, StyleSheet } from 'react-native';
import { router, useLocalSearchParams } from 'expo-router';

import { Text, View } from '@/components/Themed';

const readParam = (value: string | string[] | undefined): string => {
  if (Array.isArray(value)) {
    return value[0] ?? '';
  }

  return value ?? '';
};

export default function OrderConfirmedScreen() {
  const params = useLocalSearchParams();
  const orderId = readParam(params.orderId);

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Order Confirmed</Text>
      <Text style={styles.description}>
        Your payment was successful and your order is now being processed.
      </Text>
      <Text style={styles.orderId}>Order ID: {orderId || 'Unknown order'}</Text>

      <Pressable style={styles.primaryButton} onPress={() => router.replace('/(customer)')}>
        <Text style={styles.primaryButtonText}>Back to Home</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 24,
    gap: 12,
  },
  title: {
    fontSize: 28,
    fontWeight: '800',
    textAlign: 'center',
  },
  description: {
    textAlign: 'center',
    lineHeight: 22,
    color: '#4b5563',
  },
  orderId: {
    fontWeight: '600',
  },
  primaryButton: {
    marginTop: 8,
    backgroundColor: '#0f766e',
    borderRadius: 12,
    paddingHorizontal: 16,
    paddingVertical: 12,
  },
  primaryButtonText: {
    color: '#ffffff',
    fontWeight: '700',
  },
});
