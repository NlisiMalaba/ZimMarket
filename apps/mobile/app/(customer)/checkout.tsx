import { useMemo, useState } from 'react';
import { ActivityIndicator, Alert, Pressable, ScrollView, StyleSheet, TextInput } from 'react-native';
import { router } from 'expo-router';

import { Text, View } from '@/components/Themed';
import { ordersService } from '@/lib/services/orders-service';
import { useCartStore } from '@/store/cart-store';

type PaymentMethod = 'Paynow' | 'Ecocash';

const formatCurrency = (value: number, currency: 'USD' | 'ZWL'): string =>
  new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
  }).format(value);

const initialAddresses = ['221 Samora Machel Ave, Harare', '14 Josiah Tongogara St, Bulawayo'];

export default function CheckoutScreen() {
  const { items, clearCart } = useCartStore((state) => ({
    items: state.items,
    clearCart: state.clearCart,
  }));

  const [selectedAddress, setSelectedAddress] = useState(initialAddresses[0] ?? '');
  const [customAddress, setCustomAddress] = useState('');
  const [addresses, setAddresses] = useState<string[]>(initialAddresses);
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>('Paynow');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const subtotalUsd = useMemo(
    () => items.reduce((sum, item) => sum + item.unitPriceUsd * item.quantity, 0),
    [items]
  );
  const subtotalZwl = useMemo(
    () => items.reduce((sum, item) => sum + item.unitPriceZwl * item.quantity, 0),
    [items]
  );

  const addAddress = () => {
    const normalized = customAddress.trim();
    if (normalized.length < 8) {
      Alert.alert('Invalid address', 'Please enter a full delivery address.');
      return;
    }

    setAddresses((previous) => [normalized, ...previous.filter((item) => item !== normalized)]);
    setSelectedAddress(normalized);
    setCustomAddress('');
  };

  const placeOrder = async () => {
    if (items.length === 0) {
      Alert.alert('Cart is empty', 'Please add items before checkout.');
      return;
    }

    if (selectedAddress.trim().length < 8) {
      Alert.alert('Address required', 'Select or add a valid delivery address.');
      return;
    }

    setIsSubmitting(true);
    try {
      const order = await ordersService.placeOrder({
        deliveryAddress: selectedAddress.trim(),
        paymentMethod,
        items: items.map((item) => ({
          productId: item.productId,
          quantity: item.quantity,
        })),
      });

      clearCart();
      router.replace({
        pathname: '/(customer)/payment',
        params: {
          orderId: order.orderId,
          redirectUrl: order.paymentRedirectUrl,
        },
      });
    } catch (error) {
      const message =
        error instanceof Error && error.message.length > 0
          ? error.message
          : 'Unable to place order right now.';
      Alert.alert('Order failed', message);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <Text style={styles.sectionTitle}>Delivery address</Text>
      <View style={styles.card}>
        {addresses.map((address) => {
          const selected = selectedAddress === address;
          return (
            <Pressable
              key={address}
              style={[styles.optionRow, selected ? styles.optionRowSelected : null]}
              onPress={() => setSelectedAddress(address)}
            >
              <Text style={[styles.optionText, selected ? styles.optionTextSelected : null]}>{address}</Text>
            </Pressable>
          );
        })}
        <TextInput
          style={styles.input}
          value={customAddress}
          onChangeText={setCustomAddress}
          placeholder="Add new delivery address"
        />
        <Pressable style={styles.secondaryButton} onPress={addAddress}>
          <Text style={styles.secondaryButtonText}>Add address</Text>
        </Pressable>
      </View>

      <Text style={styles.sectionTitle}>Payment method</Text>
      <View style={styles.card}>
        {(['Paynow', 'Ecocash'] as const).map((method) => {
          const selected = paymentMethod === method;
          return (
            <Pressable
              key={method}
              style={[styles.optionRow, selected ? styles.optionRowSelected : null]}
              onPress={() => setPaymentMethod(method)}
            >
              <Text style={[styles.optionText, selected ? styles.optionTextSelected : null]}>{method}</Text>
            </Pressable>
          );
        })}
      </View>

      <Text style={styles.sectionTitle}>Order summary</Text>
      <View style={styles.card}>
        <Text style={styles.summaryText}>Items: {items.length}</Text>
        <Text style={styles.summaryTotalUsd}>{formatCurrency(subtotalUsd, 'USD')}</Text>
        <Text style={styles.summaryTotalZwl}>{formatCurrency(subtotalZwl, 'ZWL')}</Text>
      </View>

      <Pressable
        style={[styles.placeOrderButton, isSubmitting ? styles.disabledButton : null]}
        onPress={placeOrder}
        disabled={isSubmitting}
      >
        {isSubmitting ? (
          <ActivityIndicator size="small" color="#ffffff" />
        ) : (
          <Text style={styles.placeOrderText}>Place Order</Text>
        )}
      </Pressable>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    padding: 16,
    gap: 10,
    paddingBottom: 28,
  },
  sectionTitle: {
    fontSize: 17,
    fontWeight: '700',
    marginTop: 8,
  },
  card: {
    borderWidth: 1,
    borderColor: '#e5e7eb',
    borderRadius: 12,
    padding: 12,
    gap: 8,
  },
  optionRow: {
    borderWidth: 1,
    borderColor: '#d1d5db',
    borderRadius: 10,
    paddingHorizontal: 10,
    paddingVertical: 10,
  },
  optionRowSelected: {
    borderColor: '#0f766e',
    backgroundColor: '#f0fdfa',
  },
  optionText: {
    fontSize: 14,
    color: '#374151',
  },
  optionTextSelected: {
    color: '#0f766e',
    fontWeight: '700',
  },
  input: {
    borderWidth: 1,
    borderColor: '#d1d5db',
    borderRadius: 10,
    paddingHorizontal: 10,
    paddingVertical: 10,
    fontSize: 14,
  },
  secondaryButton: {
    backgroundColor: '#0f766e',
    borderRadius: 10,
    alignItems: 'center',
    paddingVertical: 10,
  },
  secondaryButtonText: {
    color: '#ffffff',
    fontWeight: '700',
  },
  summaryText: {
    color: '#4b5563',
    fontSize: 14,
  },
  summaryTotalUsd: {
    fontSize: 24,
    fontWeight: '800',
  },
  summaryTotalZwl: {
    fontSize: 14,
    color: '#4b5563',
  },
  placeOrderButton: {
    marginTop: 12,
    backgroundColor: '#0f766e',
    borderRadius: 12,
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 14,
    minHeight: 48,
  },
  placeOrderText: {
    color: '#ffffff',
    fontWeight: '700',
    fontSize: 16,
  },
  disabledButton: {
    opacity: 0.7,
  },
});
