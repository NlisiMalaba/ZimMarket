import { ActivityIndicator, FlatList, StyleSheet } from 'react-native';
import { useLocalSearchParams } from 'expo-router';
import { useQuery } from '@tanstack/react-query';

import { Text, View } from '@/components/Themed';
import { sellerOrdersService } from '@/lib/services/seller-orders-service';

const readParam = (value: string | string[] | undefined): string => {
  if (Array.isArray(value)) {
    return value[0] ?? '';
  }

  return value ?? '';
};

const formatUsd = (value: number): string =>
  new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);

export default function SellerOrderDetailScreen() {
  const params = useLocalSearchParams();
  const orderId = readParam(params.orderId);

  const orderQuery = useQuery({
    queryKey: ['seller-order-detail', orderId],
    queryFn: () => sellerOrdersService.getById(orderId),
    enabled: orderId.length > 0,
  });

  if (orderQuery.isLoading) {
    return (
      <View style={styles.stateContainer}>
        <ActivityIndicator size="large" color="#0f766e" />
      </View>
    );
  }

  if (orderQuery.isError || !orderQuery.data) {
    return (
      <View style={styles.stateContainer}>
        <Text style={styles.errorText}>Unable to load order details.</Text>
      </View>
    );
  }

  const order = orderQuery.data;

  return (
    <View style={styles.container}>
      <View style={styles.summaryCard}>
        <Text style={styles.heading}>Order #{order.id}</Text>
        <Text style={styles.meta}>Status: {order.status}</Text>
        <Text style={styles.meta}>Payment: {order.paymentStatus}</Text>
        <Text style={styles.meta}>Customer city: {order.customerCity}</Text>
        <Text style={styles.total}>Total: {formatUsd(order.totalUsd)}</Text>
      </View>

      <Text style={styles.sectionTitle}>Your order items</Text>
      <FlatList
        data={order.items}
        keyExtractor={(item) => `${item.productId}-${item.productTitle}`}
        contentContainerStyle={styles.listContent}
        renderItem={({ item }) => (
          <View style={styles.itemRow}>
            <View style={styles.itemInfo}>
              <Text style={styles.itemTitle}>{item.productTitle}</Text>
              <Text style={styles.itemMeta}>
                Qty {item.quantity} • Unit {formatUsd(item.unitPriceUsd)}
              </Text>
            </View>
            <Text style={styles.itemAmount}>{formatUsd(item.lineTotalUsd)}</Text>
          </View>
        )}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 16,
    gap: 12,
  },
  summaryCard: {
    borderWidth: 1,
    borderColor: '#e5e7eb',
    borderRadius: 12,
    padding: 12,
    gap: 4,
  },
  heading: {
    fontSize: 18,
    fontWeight: '700',
  },
  meta: {
    color: '#475569',
    fontSize: 13,
  },
  total: {
    marginTop: 6,
    fontWeight: '700',
    color: '#0f766e',
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '700',
  },
  listContent: {
    gap: 8,
    paddingBottom: 16,
  },
  itemRow: {
    borderWidth: 1,
    borderColor: '#e5e7eb',
    borderRadius: 10,
    padding: 10,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  itemInfo: {
    flex: 1,
  },
  itemTitle: {
    fontWeight: '700',
  },
  itemMeta: {
    marginTop: 4,
    color: '#6b7280',
    fontSize: 12,
  },
  itemAmount: {
    fontWeight: '700',
  },
  stateContainer: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
  },
  errorText: {
    color: '#dc2626',
    textAlign: 'center',
  },
});

