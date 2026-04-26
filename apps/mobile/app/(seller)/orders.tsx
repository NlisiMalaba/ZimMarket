import { ActivityIndicator, FlatList, Pressable, StyleSheet } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { router } from 'expo-router';

import { Text, View } from '@/components/Themed';
import { sellerOrdersService } from '@/lib/services/seller-orders-service';

const formatDateTime = (value: string): string => {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return date.toLocaleString();
};

const formatUsd = (value: number): string =>
  new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);

export default function SellerOrdersScreen() {
  const ordersQuery = useQuery({
    queryKey: ['seller-orders'],
    queryFn: () => sellerOrdersService.list(),
  });

  return (
    <View style={styles.container}>
      {ordersQuery.isLoading ? (
        <View style={styles.stateContainer}>
          <ActivityIndicator size="large" color="#0f766e" />
        </View>
      ) : ordersQuery.isError ? (
        <View style={styles.stateContainer}>
          <Text style={styles.errorText}>Unable to load seller orders.</Text>
        </View>
      ) : (
        <FlatList
          data={ordersQuery.data ?? []}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.listContent}
          refreshing={ordersQuery.isRefetching}
          onRefresh={() => {
            void ordersQuery.refetch();
          }}
          renderItem={({ item }) => (
            <Pressable
              style={styles.orderRow}
              onPress={() =>
                router.push({
                  pathname: '/(seller)/orders/[orderId]' as never,
                  params: { orderId: item.id },
                })
              }
            >
              <View style={styles.orderInfo}>
                <Text style={styles.orderId}>Order #{item.id}</Text>
                <Text style={styles.orderMeta}>
                  {item.status} • {item.paymentStatus} • {item.sellerLineItemCount} item(s)
                </Text>
                <Text style={styles.orderMeta}>{formatDateTime(item.createdAt)}</Text>
              </View>
              <Text style={styles.amount}>{formatUsd(item.totalUsd)}</Text>
            </Pressable>
          )}
          ListEmptyComponent={
            <View style={styles.stateContainer}>
              <Text style={styles.emptyText}>No seller orders yet.</Text>
            </View>
          }
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 16,
  },
  listContent: {
    gap: 10,
    paddingBottom: 16,
  },
  orderRow: {
    borderWidth: 1,
    borderColor: '#e5e7eb',
    borderRadius: 12,
    padding: 12,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    gap: 10,
  },
  orderInfo: {
    flex: 1,
  },
  orderId: {
    fontSize: 15,
    fontWeight: '700',
  },
  orderMeta: {
    marginTop: 4,
    color: '#6b7280',
    fontSize: 12,
  },
  amount: {
    fontWeight: '700',
    color: '#0f766e',
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
  emptyText: {
    color: '#334155',
    textAlign: 'center',
  },
});

