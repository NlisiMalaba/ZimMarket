import { Image, Pressable, StyleSheet } from 'react-native';
import { router } from 'expo-router';

import { Text, View } from '@/components/Themed';
import { useCartStore } from '@/store/cart-store';

const formatCurrency = (value: number, currency: 'USD' | 'ZWL'): string =>
  new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
  }).format(value);

export default function CartScreen() {
  const { items, incrementItem, decrementItem, removeItem } = useCartStore((state) => ({
    items: state.items,
    incrementItem: state.incrementItem,
    decrementItem: state.decrementItem,
    removeItem: state.removeItem,
  }));

  const subtotalUsd = items.reduce((sum, item) => sum + item.unitPriceUsd * item.quantity, 0);
  const subtotalZwl = items.reduce((sum, item) => sum + item.unitPriceZwl * item.quantity, 0);

  if (items.length === 0) {
    return (
      <View style={styles.emptyContainer}>
        <Text style={styles.emptyTitle}>Your cart is empty</Text>
        <Pressable style={styles.browseButton} onPress={() => router.replace('/(customer)')}>
          <Text style={styles.browseButtonText}>Browse products</Text>
        </Pressable>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.itemsContainer}>
        {items.map((item) => (
          <View key={item.productId} style={styles.itemRow}>
            <Image
              source={{
                uri: item.imageUrl && item.imageUrl.length > 0 ? item.imageUrl : 'https://placehold.co/200x200/png',
              }}
              style={styles.itemImage}
              resizeMode="cover"
            />
            <View style={styles.itemInfo}>
              <Text style={styles.itemTitle} numberOfLines={2}>
                {item.title}
              </Text>
              <Text style={styles.itemPrice}>
                {formatCurrency(item.unitPriceUsd, 'USD')} / {formatCurrency(item.unitPriceZwl, 'ZWL')}
              </Text>
              <View style={styles.controlsRow}>
                <Pressable style={styles.qtyButton} onPress={() => decrementItem(item.productId)}>
                  <Text style={styles.qtyButtonText}>-</Text>
                </Pressable>
                <Text style={styles.qtyText}>{item.quantity}</Text>
                <Pressable style={styles.qtyButton} onPress={() => incrementItem(item.productId)}>
                  <Text style={styles.qtyButtonText}>+</Text>
                </Pressable>
                <Pressable style={styles.removeButton} onPress={() => removeItem(item.productId)}>
                  <Text style={styles.removeButtonText}>Remove</Text>
                </Pressable>
              </View>
            </View>
          </View>
        ))}
      </View>

      <View style={styles.summaryCard}>
        <Text style={styles.summaryTitle}>Subtotal</Text>
        <Text style={styles.subtotalUsd}>{formatCurrency(subtotalUsd, 'USD')}</Text>
        <Text style={styles.subtotalZwl}>{formatCurrency(subtotalZwl, 'ZWL')}</Text>
        <Pressable style={styles.checkoutButton} onPress={() => router.push('/(customer)/checkout')}>
          <Text style={styles.checkoutButtonText}>Proceed to Checkout</Text>
        </Pressable>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 16,
    gap: 12,
  },
  itemsContainer: {
    gap: 10,
  },
  itemRow: {
    flexDirection: 'row',
    gap: 10,
    borderWidth: 1,
    borderColor: '#e5e7eb',
    borderRadius: 12,
    padding: 10,
  },
  itemImage: {
    width: 70,
    height: 70,
    borderRadius: 8,
    backgroundColor: '#f3f4f6',
  },
  itemInfo: {
    flex: 1,
    gap: 4,
  },
  itemTitle: {
    fontSize: 14,
    fontWeight: '700',
  },
  itemPrice: {
    fontSize: 12,
    color: '#4b5563',
  },
  controlsRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginTop: 4,
  },
  qtyButton: {
    width: 28,
    height: 28,
    borderRadius: 6,
    borderWidth: 1,
    borderColor: '#d1d5db',
    alignItems: 'center',
    justifyContent: 'center',
  },
  qtyButtonText: {
    fontSize: 16,
    fontWeight: '700',
  },
  qtyText: {
    minWidth: 18,
    textAlign: 'center',
    fontWeight: '700',
  },
  removeButton: {
    marginLeft: 'auto',
  },
  removeButtonText: {
    color: '#dc2626',
    fontWeight: '600',
  },
  summaryCard: {
    borderWidth: 1,
    borderColor: '#e5e7eb',
    borderRadius: 12,
    padding: 12,
    gap: 6,
    marginTop: 'auto',
  },
  summaryTitle: {
    fontSize: 14,
    fontWeight: '600',
    color: '#4b5563',
  },
  subtotalUsd: {
    fontSize: 24,
    fontWeight: '800',
  },
  subtotalZwl: {
    fontSize: 14,
    color: '#4b5563',
  },
  checkoutButton: {
    marginTop: 8,
    backgroundColor: '#0f766e',
    borderRadius: 10,
    paddingVertical: 12,
    alignItems: 'center',
  },
  checkoutButtonText: {
    color: '#ffffff',
    fontWeight: '700',
    fontSize: 15,
  },
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 24,
    gap: 12,
  },
  emptyTitle: {
    fontSize: 22,
    fontWeight: '700',
    textAlign: 'center',
  },
  browseButton: {
    backgroundColor: '#0f766e',
    borderRadius: 10,
    paddingHorizontal: 14,
    paddingVertical: 10,
  },
  browseButtonText: {
    color: '#ffffff',
    fontWeight: '700',
  },
});
