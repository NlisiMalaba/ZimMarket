import { ActivityIndicator, FlatList, Pressable, StyleSheet } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { router } from 'expo-router';

import { Text, View } from '@/components/Themed';
import { sellerListingsService } from '@/lib/services/seller-listings-service';

const formatUsd = (value: number): string =>
  new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);

const normalizeStatus = (status: string): string => {
  const value = status.trim().toLowerCase();
  if (value === 'active') {
    return 'Active';
  }

  if (value === 'inactive') {
    return 'Inactive';
  }

  if (value === 'outofstock' || value === 'out_of_stock') {
    return 'Out of stock';
  }

  return status;
};

const badgeStylesByStatus = (status: string) => {
  const normalized = status.trim().toLowerCase();
  if (normalized === 'active') {
    return {
      container: styles.badgeActive,
      text: styles.badgeTextActive,
    };
  }

  if (normalized === 'inactive') {
    return {
      container: styles.badgeInactive,
      text: styles.badgeTextInactive,
    };
  }

  return {
    container: styles.badgeUnknown,
    text: styles.badgeTextUnknown,
  };
};

export default function SellerListingsScreen() {
  const listingsQuery = useQuery({
    queryKey: ['seller-listings'],
    queryFn: () => sellerListingsService.listMine(),
  });

  return (
    <View style={styles.container}>
      {listingsQuery.isLoading ? (
        <View style={styles.stateContainer}>
          <ActivityIndicator size="large" color="#0f766e" />
        </View>
      ) : listingsQuery.isError ? (
        <View style={styles.stateContainer}>
          <Text style={styles.errorText}>Failed to load your listings.</Text>
          <Pressable style={styles.retryButton} onPress={() => void listingsQuery.refetch()}>
            <Text style={styles.retryButtonText}>Retry</Text>
          </Pressable>
        </View>
      ) : (
        <FlatList
          data={listingsQuery.data ?? []}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.listContent}
          renderItem={({ item }) => {
            const badge = badgeStylesByStatus(item.status);

            return (
              <Pressable
                style={styles.row}
                onPress={() =>
                  router.push({
                    pathname: '/(seller)/edit-listing/[id]' as never,
                    params: { id: item.id },
                  })
                }
              >
                <View style={styles.rowText}>
                  <Text style={styles.title}>{item.title}</Text>
                  <Text style={styles.meta}>
                    {formatUsd(item.priceUsd)} • Stock {item.stockQuantity} • {item.categoryName}
                  </Text>
                </View>
                <Text style={[styles.badge, badge.container, badge.text]}>{normalizeStatus(item.status)}</Text>
              </Pressable>
            );
          }}
          ListEmptyComponent={
            <View style={styles.stateContainer}>
              <Text style={styles.emptyText}>No listings yet. Tap + to create your first listing.</Text>
            </View>
          }
          refreshing={listingsQuery.isRefetching}
          onRefresh={() => {
            void listingsQuery.refetch();
          }}
        />
      )}

      <Pressable
        style={styles.fab}
        onPress={() => router.push('/(seller)/create-listing' as never)}
        accessibilityLabel="Create new listing"
      >
        <Text style={styles.fabText}>+</Text>
      </Pressable>
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
    paddingBottom: 90,
  },
  row: {
    borderWidth: 1,
    borderColor: '#e5e7eb',
    borderRadius: 12,
    padding: 12,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    gap: 12,
  },
  rowText: {
    flex: 1,
  },
  title: {
    fontSize: 15,
    fontWeight: '700',
  },
  meta: {
    marginTop: 4,
    color: '#6b7280',
    fontSize: 12,
  },
  badge: {
    borderRadius: 999,
    paddingHorizontal: 10,
    paddingVertical: 6,
    fontSize: 12,
    fontWeight: '700',
  },
  badgeActive: {
    backgroundColor: '#dcfce7',
  },
  badgeInactive: {
    backgroundColor: '#fee2e2',
  },
  badgeUnknown: {
    backgroundColor: '#e0f2fe',
  },
  badgeTextActive: {
    color: '#166534',
  },
  badgeTextInactive: {
    color: '#b91c1c',
  },
  badgeTextUnknown: {
    color: '#075985',
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
    marginBottom: 12,
  },
  emptyText: {
    color: '#334155',
    textAlign: 'center',
  },
  retryButton: {
    backgroundColor: '#0f766e',
    borderRadius: 10,
    paddingHorizontal: 14,
    paddingVertical: 10,
  },
  retryButtonText: {
    color: '#ffffff',
    fontWeight: '700',
  },
  fab: {
    position: 'absolute',
    right: 20,
    bottom: 20,
    width: 56,
    height: 56,
    borderRadius: 28,
    backgroundColor: '#0f766e',
    alignItems: 'center',
    justifyContent: 'center',
    elevation: 4,
  },
  fabText: {
    color: '#ffffff',
    fontSize: 30,
    lineHeight: 30,
    marginTop: -2,
  },
});
