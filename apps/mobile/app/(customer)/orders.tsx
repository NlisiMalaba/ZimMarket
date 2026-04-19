import { useState } from 'react';
import { ActivityIndicator, FlatList, Pressable, StyleSheet } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { router } from 'expo-router';

import { Text, View } from '@/components/Themed';
import { ordersService } from '@/lib/services/orders-service';
import type { OrderItem, OrderStatusTab } from '@/types/order';

const tabs: OrderStatusTab[] = ['Active', 'Completed', 'Cancelled'];

const formatDateTime = (value: string): string => {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return date.toLocaleString();
};

const getBadgeStyle = (status: OrderStatusTab) => {
  if (status === 'Completed') {
    return styles.badgeCompleted;
  }

  if (status === 'Cancelled') {
    return styles.badgeCancelled;
  }

  return styles.badgeActive;
};

const getBadgeTextStyle = (status: OrderStatusTab) => {
  if (status === 'Completed') {
    return styles.badgeTextCompleted;
  }

  if (status === 'Cancelled') {
    return styles.badgeTextCancelled;
  }

  return styles.badgeTextActive;
};

const OrderRow = ({ item }: { item: OrderItem }) => (
  <Pressable
    style={styles.orderRow}
    onPress={() =>
      router.push({
        pathname: '/(customer)/orders/[orderId]/tracking',
        params: {
          orderId: item.id,
        },
      })
    }
  >
    <View>
      <Text style={styles.orderId}>Order #{item.id}</Text>
      <Text style={styles.orderTimestamp}>{formatDateTime(item.createdAt)}</Text>
    </View>
    <Text style={[styles.badge, getBadgeStyle(item.status), getBadgeTextStyle(item.status)]}>
      {item.status}
    </Text>
  </Pressable>
);

export default function OrdersScreen() {
  const [activeTab, setActiveTab] = useState<OrderStatusTab>('Active');

  const ordersQuery = useQuery({
    queryKey: ['orders', activeTab],
    queryFn: async () => ordersService.list(activeTab),
  });

  return (
    <View style={styles.container}>
      <View style={styles.tabsRow}>
        {tabs.map((tab) => {
          const selected = tab === activeTab;
          return (
            <Pressable
              key={tab}
              style={[styles.tabButton, selected ? styles.tabButtonActive : null]}
              onPress={() => setActiveTab(tab)}
            >
              <Text style={[styles.tabButtonText, selected ? styles.tabButtonTextActive : null]}>{tab}</Text>
            </Pressable>
          );
        })}
      </View>

      {ordersQuery.isLoading ? (
        <View style={styles.stateContainer}>
          <ActivityIndicator size="large" color="#0f766e" />
        </View>
      ) : ordersQuery.isError ? (
        <View style={styles.stateContainer}>
          <Text style={styles.errorText}>Unable to load orders for {activeTab}.</Text>
        </View>
      ) : (
        <FlatList
          data={ordersQuery.data ?? []}
          keyExtractor={(item) => `${item.id}-${item.createdAt}`}
          contentContainerStyle={styles.listContent}
          renderItem={({ item }) => <OrderRow item={item} />}
          refreshing={ordersQuery.isRefetching}
          onRefresh={() => {
            void ordersQuery.refetch();
          }}
          ListEmptyComponent={
            <View style={styles.emptyContainer}>
              <Text>No {activeTab.toLowerCase()} orders yet.</Text>
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
    gap: 12,
  },
  tabsRow: {
    flexDirection: 'row',
    gap: 8,
  },
  tabButton: {
    flex: 1,
    borderWidth: 1,
    borderColor: '#d1d5db',
    borderRadius: 10,
    alignItems: 'center',
    paddingVertical: 10,
  },
  tabButtonActive: {
    borderColor: '#0f766e',
    backgroundColor: '#f0fdfa',
  },
  tabButtonText: {
    fontWeight: '600',
    color: '#4b5563',
  },
  tabButtonTextActive: {
    color: '#0f766e',
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
  },
  orderId: {
    fontSize: 15,
    fontWeight: '700',
  },
  orderTimestamp: {
    marginTop: 4,
    fontSize: 12,
    color: '#6b7280',
  },
  badge: {
    borderRadius: 999,
    paddingHorizontal: 10,
    paddingVertical: 6,
    fontSize: 12,
    fontWeight: '700',
  },
  badgeActive: {
    backgroundColor: '#dbeafe',
  },
  badgeCompleted: {
    backgroundColor: '#dcfce7',
  },
  badgeCancelled: {
    backgroundColor: '#fee2e2',
  },
  badgeTextActive: {
    color: '#1d4ed8',
  },
  badgeTextCompleted: {
    color: '#166534',
  },
  badgeTextCancelled: {
    color: '#b91c1c',
  },
  stateContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 24,
  },
  errorText: {
    color: '#dc2626',
    textAlign: 'center',
  },
  emptyContainer: {
    paddingVertical: 24,
    alignItems: 'center',
  },
});
