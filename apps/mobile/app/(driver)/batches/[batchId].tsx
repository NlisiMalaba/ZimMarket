import { useMemo, useState } from 'react';
import { ActivityIndicator, FlatList, Linking, Pressable, StyleSheet } from 'react-native';
import { router, useLocalSearchParams } from 'expo-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { Text, View } from '@/components/Themed';
import { driverBatchesService } from '@/lib/services/driver-batches-service';
import { driverLocationTrackingService } from '@/lib/services/driver-location-tracking-service';

const buildGoogleMapsUrl = (address: string): string =>
  `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(address)}`;

const extractBatchId = (value: string | string[] | undefined): string => {
  if (!value) {
    return '';
  }

  return Array.isArray(value) ? value[0] : value;
};

export default function DriverBatchDetailScreen() {
  const { batchId: rawBatchId } = useLocalSearchParams<{ batchId: string | string[] }>();
  const batchId = useMemo(() => extractBatchId(rawBatchId), [rawBatchId]);
  const queryClient = useQueryClient();
  const [permissionMessage, setPermissionMessage] = useState<string | null>(null);

  const batchQuery = useQuery({
    queryKey: ['driver-batch-detail', batchId],
    queryFn: () => driverBatchesService.getById(batchId),
    enabled: batchId.length > 0,
  });

  const markCollectedMutation = useMutation({
    mutationFn: () => driverBatchesService.markCollected(batchId),
    onSuccess: async () => {
      setPermissionMessage(null);
      const permission = await driverLocationTrackingService.ensurePermissions();
      if (!permission.granted) {
        setPermissionMessage(permission.message);
      } else {
        await driverLocationTrackingService.startTracking();
      }
      await queryClient.invalidateQueries({ queryKey: ['driver-batch-detail', batchId] });
    },
  });

  if (!batchId) {
    return (
      <View style={styles.stateContainer}>
        <Text style={styles.error}>Batch id is missing.</Text>
      </View>
    );
  }

  if (batchQuery.isLoading) {
    return (
      <View style={styles.stateContainer}>
        <ActivityIndicator size="large" color="#0f766e" />
      </View>
    );
  }

  if (batchQuery.isError || !batchQuery.data) {
    return (
      <View style={styles.stateContainer}>
        <Text style={styles.error}>Unable to load batch details.</Text>
        <Pressable style={styles.secondaryButton} onPress={() => void batchQuery.refetch()}>
          <Text style={styles.secondaryButtonText}>Retry</Text>
        </Pressable>
      </View>
    );
  }

  const batch = batchQuery.data;
  const mapsUrl = buildGoogleMapsUrl(batch.pickupWarehouseAddress);
  const canMarkCollected = batch.status.trim().toLowerCase() === 'created' && !markCollectedMutation.isPending;

  return (
    <View style={styles.container}>
      <View style={styles.card}>
        <Text style={styles.sectionTitle}>Pickup warehouse</Text>
        <Text style={styles.address}>{batch.pickupWarehouseAddress}</Text>
        <Pressable style={styles.secondaryButton} onPress={() => void Linking.openURL(mapsUrl)}>
          <Text style={styles.secondaryButtonText}>Open in Google Maps</Text>
        </Pressable>
      </View>

      <View style={styles.listHeader}>
        <Text style={styles.sectionTitle}>Delivery orders by area</Text>
      </View>
      <FlatList
        data={batch.groupedOrders}
        keyExtractor={(item) => item.area}
        contentContainerStyle={styles.listContent}
        renderItem={({ item }) => (
          <View style={styles.card}>
            <Text style={styles.areaTitle}>{item.area}</Text>
            <Text style={styles.meta}>{item.orderIds.length} order(s)</Text>
            {item.orderIds.map((orderId) => (
              <Text style={styles.orderId} key={orderId}>
                • Order #{orderId}
              </Text>
            ))}
          </View>
        )}
      />

      <Pressable
        style={[styles.primaryButton, !canMarkCollected ? styles.disabledButton : null]}
        disabled={!canMarkCollected}
        onPress={() => markCollectedMutation.mutate()}
      >
        <Text style={styles.primaryButtonText}>
          {markCollectedMutation.isPending ? 'Marking as collected...' : 'Mark as Collected'}
        </Text>
      </Pressable>

      <Pressable
        style={styles.secondaryButton}
        onPress={() =>
          router.push({
            pathname: '/(driver)/active-delivery/[batchId]' as never,
            params: { batchId },
          })
        }
      >
        <Text style={styles.secondaryButtonText}>Open active delivery screen</Text>
      </Pressable>

      {permissionMessage ? <Text style={styles.permissionText}>{permissionMessage}</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 16,
    gap: 10,
  },
  stateContainer: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
    gap: 12,
  },
  listHeader: {
    marginTop: 4,
  },
  listContent: {
    gap: 10,
    paddingBottom: 12,
  },
  card: {
    borderWidth: 1,
    borderColor: '#e5e7eb',
    borderRadius: 12,
    padding: 12,
    gap: 6,
  },
  sectionTitle: {
    fontSize: 17,
    fontWeight: '700',
  },
  address: {
    color: '#334155',
    lineHeight: 20,
  },
  areaTitle: {
    fontSize: 15,
    fontWeight: '700',
  },
  meta: {
    color: '#64748b',
    fontSize: 13,
  },
  orderId: {
    color: '#0f172a',
    fontSize: 13,
  },
  primaryButton: {
    backgroundColor: '#0f766e',
    borderRadius: 12,
    paddingVertical: 14,
    alignItems: 'center',
    marginTop: 'auto',
  },
  primaryButtonText: {
    color: '#ffffff',
    fontWeight: '700',
    fontSize: 15,
  },
  secondaryButton: {
    borderWidth: 1,
    borderColor: '#0f766e',
    borderRadius: 10,
    paddingVertical: 10,
    alignItems: 'center',
  },
  secondaryButtonText: {
    color: '#0f766e',
    fontWeight: '700',
  },
  disabledButton: {
    opacity: 0.6,
  },
  error: {
    color: '#dc2626',
    textAlign: 'center',
  },
  permissionText: {
    color: '#b45309',
    textAlign: 'center',
    fontSize: 13,
  },
});
