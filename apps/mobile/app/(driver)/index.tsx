import { ActivityIndicator, FlatList, Pressable, StyleSheet } from 'react-native';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { router } from 'expo-router';

import { Text, View } from '@/components/Themed';
import { driverDashboardService } from '@/lib/services/driver-dashboard-service';

export default function DriverHomeScreen() {
  const queryClient = useQueryClient();
  const homeQuery = useQuery({
    queryKey: ['driver-home'],
    queryFn: () => driverDashboardService.getHomeData(),
  });

  const updateStatusMutation = useMutation({
    mutationFn: (nextStatus: 'Available' | 'Offline') => driverDashboardService.setAvailabilityStatus(nextStatus),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['driver-home'] });
    },
  });

  if (homeQuery.isLoading) {
    return (
      <View style={styles.stateContainer}>
        <ActivityIndicator size="large" color="#0f766e" />
      </View>
    );
  }

  if (homeQuery.isError || !homeQuery.data) {
    return (
      <View style={styles.stateContainer}>
        <Text style={styles.error}>Failed to load driver home.</Text>
        <Pressable style={styles.refreshButton} onPress={() => void homeQuery.refetch()}>
          <Text style={styles.refreshButtonText}>Retry</Text>
        </Pressable>
      </View>
    );
  }

  const { status, activeBatch, availableBatches } = homeQuery.data;
  const canChangeAvailability = status !== 'OnDelivery' && !updateStatusMutation.isPending;

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Driver home</Text>

      <View style={styles.statusCard}>
        <Text style={styles.label}>Current status</Text>
        <Text style={styles.statusValue}>{status}</Text>
      </View>

      <View style={styles.toggleRow}>
        <Pressable
          style={[
            styles.toggleButton,
            status === 'Available' ? styles.toggleButtonActive : null,
            !canChangeAvailability ? styles.toggleButtonDisabled : null,
          ]}
          disabled={!canChangeAvailability}
          onPress={() => updateStatusMutation.mutate('Available')}
        >
          <Text style={[styles.toggleText, status === 'Available' ? styles.toggleTextActive : null]}>Available</Text>
        </Pressable>
        <Pressable
          style={[
            styles.toggleButton,
            status === 'Offline' ? styles.toggleButtonActive : null,
            !canChangeAvailability ? styles.toggleButtonDisabled : null,
          ]}
          disabled={!canChangeAvailability}
          onPress={() => updateStatusMutation.mutate('Offline')}
        >
          <Text style={[styles.toggleText, status === 'Offline' ? styles.toggleTextActive : null]}>Offline</Text>
        </Pressable>
      </View>

      {status === 'OnDelivery' && activeBatch ? (
        <Pressable
          style={styles.batchCard}
          onPress={() =>
            router.push({
              pathname: '/(driver)/batches/[batchId]' as never,
              params: { batchId: activeBatch.id },
            })
          }
        >
          <Text style={styles.sectionTitle}>Active batch</Text>
          <Text style={styles.batchId}>Batch #{activeBatch.id}</Text>
          <Text style={styles.batchMeta}>{activeBatch.totalOrders} orders</Text>
          <Text style={styles.batchMeta}>{activeBatch.deliveryArea}</Text>
          <Text style={styles.batchMeta}>{activeBatch.warehouseAddress}</Text>
        </Pressable>
      ) : null}

      {status === 'Available' ? (
        <View style={styles.listContainer}>
          <Text style={styles.sectionTitle}>Available batches</Text>
          <FlatList
            data={availableBatches}
            keyExtractor={(item) => item.id}
            contentContainerStyle={styles.listContent}
            renderItem={({ item }) => (
              <Pressable
                style={styles.batchCard}
                onPress={() =>
                  router.push({
                    pathname: '/(driver)/batches/[batchId]' as never,
                    params: { batchId: item.id },
                  })
                }
              >
                <Text style={styles.batchId}>Batch #{item.id}</Text>
                <Text style={styles.batchMeta}>{item.totalOrders} orders</Text>
                <Text style={styles.batchMeta}>{item.deliveryArea}</Text>
                <Text style={styles.batchMeta}>{item.warehouseAddress}</Text>
              </Pressable>
            )}
            ListEmptyComponent={<Text style={styles.emptyText}>No available batches right now.</Text>}
          />
        </View>
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
    padding: 24,
    alignItems: 'center',
    justifyContent: 'center',
    gap: 12,
  },
  title: {
    fontSize: 24,
    fontWeight: '700',
  },
  statusCard: {
    borderWidth: 1,
    borderColor: '#d4d4d8',
    borderRadius: 12,
    padding: 12,
    gap: 6,
  },
  label: {
    color: '#64748b',
    fontSize: 12,
    textTransform: 'uppercase',
    letterSpacing: 0.6,
  },
  statusValue: {
    fontSize: 20,
    fontWeight: '700',
    color: '#0f172a',
  },
  toggleRow: {
    flexDirection: 'row',
    gap: 8,
  },
  toggleButton: {
    flex: 1,
    borderWidth: 1,
    borderColor: '#0f766e',
    borderRadius: 10,
    paddingVertical: 10,
    alignItems: 'center',
  },
  toggleButtonActive: {
    backgroundColor: '#0f766e',
  },
  toggleButtonDisabled: {
    opacity: 0.6,
  },
  toggleText: {
    color: '#0f766e',
    fontWeight: '700',
  },
  toggleTextActive: {
    color: '#ffffff',
  },
  sectionTitle: {
    fontSize: 17,
    fontWeight: '700',
  },
  listContainer: {
    flex: 1,
    gap: 8,
  },
  listContent: {
    gap: 8,
    paddingBottom: 12,
  },
  batchCard: {
    borderWidth: 1,
    borderColor: '#e5e7eb',
    borderRadius: 12,
    padding: 12,
    gap: 6,
  },
  batchId: {
    fontWeight: '700',
    fontSize: 14,
  },
  batchMeta: {
    color: '#334155',
    fontSize: 13,
  },
  emptyText: {
    color: '#64748b',
    textAlign: 'center',
    marginTop: 12,
  },
  error: {
    color: '#dc2626',
    textAlign: 'center',
  },
  refreshButton: {
    backgroundColor: '#0f766e',
    borderRadius: 10,
    paddingVertical: 10,
    paddingHorizontal: 16,
  },
  refreshButtonText: {
    color: '#ffffff',
    fontWeight: '700',
  },
});
