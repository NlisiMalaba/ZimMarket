import { useMemo } from 'react';
import { ActivityIndicator, FlatList, Pressable, StyleSheet } from 'react-native';
import { router } from 'expo-router';
import { useQuery } from '@tanstack/react-query';

import { Text, View } from '@/components/Themed';
import { formatKycStatusLabel, isSellerKycApproved, normalizeKycStatus } from '@/lib/seller-kyc';
import { sellerDashboardService } from '@/lib/services/seller-dashboard-service';
import { useAuthStore } from '@/store/auth-store';

const formatUsd = (value: number): string =>
  new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);

const formatDateTime = (value: string): string => {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return date.toLocaleString();
};

export default function SellerHomeScreen() {
  const kycStatus = useAuthStore((state) => state.kycStatus);
  const kycApproved = isSellerKycApproved(kycStatus);
  const normalizedKyc = normalizeKycStatus(kycStatus);

  const dashboardQuery = useQuery({
    queryKey: ['seller-dashboard'],
    queryFn: () => sellerDashboardService.getStats(),
  });

  const stats = useMemo(
    () => ({
      activeListings: dashboardQuery.data?.activeListings ?? 0,
      ordersPending: dashboardQuery.data?.ordersPending ?? 0,
      totalEarnedUsd: dashboardQuery.data?.totalEarnedUsd ?? 0,
    }),
    [dashboardQuery.data]
  );

  if (dashboardQuery.isLoading) {
    return (
      <View style={styles.stateContainer}>
        <ActivityIndicator size="large" color="#0f766e" />
      </View>
    );
  }

  if (dashboardQuery.isError) {
    return (
      <View style={styles.stateContainer}>
        <Text style={styles.errorText}>Failed to load seller dashboard.</Text>
        <Pressable style={styles.button} onPress={() => void dashboardQuery.refetch()}>
          <Text style={styles.buttonText}>Retry</Text>
        </Pressable>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Seller dashboard</Text>

      {!kycApproved ? (
        <View style={styles.verificationBanner}>
          <Text style={styles.verificationTitle}>Verification: {formatKycStatusLabel(kycStatus)}</Text>
          <Text style={styles.verificationBody}>
            {normalizedKyc === 'pendingReview'
              ? 'Your documents are under admin review. You can manage orders but cannot create listings yet.'
              : 'Upload your national ID and proof of residence to start selling on ZimMarket.'}
          </Text>
          <Pressable
            style={styles.verificationButton}
            onPress={() => {
              if (normalizedKyc === 'pendingReview') {
                router.push('/(seller)/application-submitted' as never);
                return;
              }

              router.push('/(seller)/kyc-upload' as never);
            }}
          >
            <Text style={styles.verificationButtonText}>
              {normalizedKyc === 'pendingReview' ? 'View application status' : 'Complete verification'}
            </Text>
          </Pressable>
        </View>
      ) : null}

      <View style={styles.statsGrid}>
        <View style={styles.statCard}>
          <Text style={styles.statLabel}>Active listings</Text>
          <Text style={styles.statValue}>{stats.activeListings}</Text>
        </View>
        <View style={styles.statCard}>
          <Text style={styles.statLabel}>Orders pending</Text>
          <Text style={styles.statValue}>{stats.ordersPending}</Text>
        </View>
        <View style={styles.statCard}>
          <Text style={styles.statLabel}>Total earned</Text>
          <Text style={styles.statValue}>{formatUsd(stats.totalEarnedUsd)}</Text>
        </View>
      </View>

      <View style={styles.sectionHeader}>
        <Text style={styles.sectionTitle}>Recent orders</Text>
        <View style={styles.headerActions}>
          <Pressable onPress={() => router.push('/(seller)/orders' as never)}>
            <Text style={styles.refreshAction}>Orders</Text>
          </Pressable>
          <Pressable onPress={() => router.push('/(seller)/listings' as never)}>
            <Text style={styles.refreshAction}>Listings</Text>
          </Pressable>
          <Pressable onPress={() => void dashboardQuery.refetch()}>
            <Text style={styles.refreshAction}>Refresh</Text>
          </Pressable>
        </View>
      </View>

      <FlatList
        data={dashboardQuery.data?.recentOrders ?? []}
        keyExtractor={(item) => `${item.id}-${item.createdAt}`}
        contentContainerStyle={styles.listContent}
        renderItem={({ item }) => (
          <View style={styles.orderRow}>
            <View>
              <Text style={styles.orderId}>Order #{item.id}</Text>
              <Text style={styles.orderMeta}>{formatDateTime(item.createdAt)}</Text>
            </View>
            <View style={styles.orderSummary}>
              <Text style={styles.orderStatus}>{item.status}</Text>
              <Text style={styles.orderAmount}>{formatUsd(item.totalUsd)}</Text>
            </View>
          </View>
        )}
        ListEmptyComponent={
          <View style={styles.emptyContainer}>
            <Text style={styles.description}>No recent orders available yet.</Text>
          </View>
        }
      />

      {kycApproved ? (
        <Pressable style={styles.button} onPress={() => router.push('/(seller)/listings' as never)}>
          <Text style={styles.buttonText}>Manage listings</Text>
        </Pressable>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 24,
    gap: 12,
  },
  stateContainer: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
    gap: 12,
  },
  title: {
    fontSize: 24,
    fontWeight: '700',
  },
  verificationBanner: {
    borderWidth: 1,
    borderColor: '#fcd34d',
    backgroundColor: '#fffbeb',
    borderRadius: 12,
    padding: 12,
    gap: 8,
  },
  verificationTitle: {
    fontWeight: '700',
    color: '#92400e',
  },
  verificationBody: {
    color: '#78350f',
    lineHeight: 20,
  },
  verificationButton: {
    alignSelf: 'flex-start',
    backgroundColor: '#0f766e',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 8,
  },
  verificationButtonText: {
    color: '#ffffff',
    fontWeight: '700',
  },
  statsGrid: {
    gap: 8,
  },
  statCard: {
    borderWidth: 1,
    borderColor: '#e5e7eb',
    borderRadius: 12,
    padding: 12,
  },
  statLabel: {
    color: '#6b7280',
    fontSize: 13,
    marginBottom: 4,
  },
  statValue: {
    fontSize: 22,
    fontWeight: '700',
    color: '#111827',
  },
  sectionHeader: {
    marginTop: 4,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  headerActions: {
    flexDirection: 'row',
    gap: 12,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: '700',
  },
  refreshAction: {
    color: '#0f766e',
    fontWeight: '700',
  },
  listContent: {
    gap: 8,
    paddingBottom: 12,
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
    fontWeight: '700',
    fontSize: 14,
  },
  orderMeta: {
    color: '#6b7280',
    fontSize: 12,
    marginTop: 4,
  },
  orderSummary: {
    alignItems: 'flex-end',
    gap: 4,
  },
  orderStatus: {
    fontSize: 12,
    color: '#0f766e',
    fontWeight: '700',
    textTransform: 'uppercase',
  },
  orderAmount: {
    fontWeight: '700',
  },
  description: {
    textAlign: 'center',
    color: '#334155',
  },
  emptyContainer: {
    paddingVertical: 20,
    alignItems: 'center',
  },
  errorText: {
    color: '#dc2626',
    textAlign: 'center',
  },
  button: {
    backgroundColor: '#0f766e',
    borderRadius: 12,
    paddingHorizontal: 16,
    paddingVertical: 12,
    alignItems: 'center',
  },
  buttonText: {
    color: '#ffffff',
    fontWeight: '700',
  },
});
