import { useMemo, useState } from 'react';
import { ActivityIndicator, FlatList, Linking, Pressable, StyleSheet } from 'react-native';
import * as ImagePicker from 'expo-image-picker';
import { useLocalSearchParams } from 'expo-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { Text, View } from '@/components/Themed';
import { driverActiveDeliveryService } from '@/lib/services/driver-active-delivery-service';
import { driverLocationTrackingService } from '@/lib/services/driver-location-tracking-service';
import type { UploadableImage } from '@/lib/services/file-upload-service';

const buildGoogleMapsNavigationUrl = (address: string): string =>
  `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(address)}&travelmode=driving`;

const extractBatchId = (value: string | string[] | undefined): string => {
  if (!value) {
    return '';
  }

  return Array.isArray(value) ? value[0] : value;
};

const resolveMimeType = (value: string | null | undefined): UploadableImage['contentType'] | null => {
  if (value === 'image/jpeg' || value === 'image/png' || value === 'image/webp') {
    return value;
  }

  return null;
};

const captureDeliveryPhoto = async (): Promise<UploadableImage> => {
  const permission = await ImagePicker.requestCameraPermissionsAsync();
  if (!permission.granted) {
    throw new Error('Camera permission is required to confirm a delivery.');
  }

  const result = await ImagePicker.launchCameraAsync({
    mediaTypes: ImagePicker.MediaTypeOptions.Images,
    quality: 0.85,
    allowsEditing: false,
  });

  if (result.canceled) {
    throw new Error('Delivery confirmation was cancelled.');
  }

  const asset = result.assets[0];
  const mimeType = resolveMimeType(asset.mimeType);
  if (!mimeType) {
    throw new Error('Only JPG, PNG, and WEBP images are supported.');
  }

  if (!asset.fileSize || asset.fileSize <= 0) {
    throw new Error('Unable to determine captured photo size. Please retake the photo.');
  }

  return {
    uri: asset.uri,
    contentType: mimeType,
    fileSizeBytes: asset.fileSize,
  };
};

export default function ActiveDeliveryScreen() {
  const { batchId: rawBatchId } = useLocalSearchParams<{ batchId: string | string[] }>();
  const batchId = useMemo(() => extractBatchId(rawBatchId), [rawBatchId]);
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);

  const deliveryQuery = useQuery({
    queryKey: ['driver-active-delivery', batchId],
    queryFn: () => driverActiveDeliveryService.getActiveDeliveryData(batchId),
    enabled: batchId.length > 0,
  });

  const confirmDeliveryMutation = useMutation({
    mutationFn: async () => {
      if (!deliveryQuery.data?.nextOrder) {
        throw new Error('No pending delivery order found.');
      }

      const photo = await captureDeliveryPhoto();
      const deliveryPhotoKey = await driverActiveDeliveryService.uploadDeliveryPhoto(photo);
      await driverActiveDeliveryService.confirmDelivery({
        batchId,
        orderId: deliveryQuery.data.nextOrder.orderId,
        deliveryPhotoKey,
      });
    },
    onSuccess: async () => {
      const current = deliveryQuery.data;
      if (current && current.deliveredCount + 1 >= current.totalOrders) {
        await driverLocationTrackingService.stopTracking();
      }
      setError(null);
      await queryClient.invalidateQueries({ queryKey: ['driver-active-delivery', batchId] });
      await queryClient.invalidateQueries({ queryKey: ['driver-batch-detail', batchId] });
    },
    onError: (mutationError) => {
      setError(mutationError instanceof Error ? mutationError.message : 'Delivery confirmation failed.');
    },
  });

  if (!batchId) {
    return (
      <View style={styles.stateContainer}>
        <Text style={styles.error}>Batch id is missing.</Text>
      </View>
    );
  }

  if (deliveryQuery.isLoading) {
    return (
      <View style={styles.stateContainer}>
        <ActivityIndicator size="large" color="#0f766e" />
      </View>
    );
  }

  if (deliveryQuery.isError || !deliveryQuery.data) {
    return (
      <View style={styles.stateContainer}>
        <Text style={styles.error}>Unable to load active delivery data.</Text>
        <Pressable style={styles.secondaryButton} onPress={() => void deliveryQuery.refetch()}>
          <Text style={styles.secondaryButtonText}>Retry</Text>
        </Pressable>
      </View>
    );
  }

  const { totalOrders, deliveredCount, nextOrder, pendingOrders } = deliveryQuery.data;
  const progressLabel = `${deliveredCount} of ${totalOrders} delivered`;
  const hasPendingOrder = nextOrder !== null;

  return (
    <View style={styles.container}>
      <View style={styles.progressCard}>
        <Text style={styles.progressLabel}>Progress</Text>
        <Text style={styles.progressValue}>{progressLabel}</Text>
      </View>

      {nextOrder ? (
        <View style={styles.card}>
          <Text style={styles.sectionTitle}>Next delivery</Text>
          <Text style={styles.meta}>Area: {nextOrder.area}</Text>
          <Text style={styles.meta}>Order: #{nextOrder.orderId}</Text>
          <Text style={styles.address}>{nextOrder.deliveryAddress}</Text>
          <Pressable
            style={styles.secondaryButton}
            onPress={() => void Linking.openURL(buildGoogleMapsNavigationUrl(nextOrder.deliveryAddress))}
          >
            <Text style={styles.secondaryButtonText}>Navigate in Google Maps</Text>
          </Pressable>
        </View>
      ) : (
        <View style={styles.card}>
          <Text style={styles.sectionTitle}>All deliveries completed</Text>
          <Text style={styles.meta}>No pending orders in this batch.</Text>
        </View>
      )}

      <FlatList
        data={pendingOrders}
        keyExtractor={(item) => item.orderId}
        contentContainerStyle={styles.listContent}
        renderItem={({ item }) => (
          <View style={styles.orderCard}>
            <Text style={styles.orderTitle}>Order #{item.orderId}</Text>
            <Text style={styles.meta}>{item.area}</Text>
            <Text style={styles.meta}>{item.deliveryAddress}</Text>
          </View>
        )}
        ListHeaderComponent={<Text style={styles.sectionTitle}>Pending orders</Text>}
        ListEmptyComponent={<Text style={styles.meta}>No pending orders.</Text>}
      />

      {error ? <Text style={styles.error}>{error}</Text> : null}

      <Pressable
        style={[styles.primaryButton, (!hasPendingOrder || confirmDeliveryMutation.isPending) ? styles.disabledButton : null]}
        disabled={!hasPendingOrder || confirmDeliveryMutation.isPending}
        onPress={() => confirmDeliveryMutation.mutate()}
      >
        <Text style={styles.primaryButtonText}>
          {confirmDeliveryMutation.isPending ? 'Confirming delivery...' : 'Confirm Delivery'}
        </Text>
      </Pressable>
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
  progressCard: {
    borderWidth: 1,
    borderColor: '#d4d4d8',
    borderRadius: 12,
    padding: 12,
  },
  progressLabel: {
    color: '#64748b',
    textTransform: 'uppercase',
    fontSize: 12,
    letterSpacing: 0.7,
  },
  progressValue: {
    fontSize: 22,
    fontWeight: '700',
    marginTop: 4,
  },
  card: {
    borderWidth: 1,
    borderColor: '#e5e7eb',
    borderRadius: 12,
    padding: 12,
    gap: 6,
  },
  listContent: {
    gap: 8,
    paddingBottom: 12,
  },
  orderCard: {
    borderWidth: 1,
    borderColor: '#e5e7eb',
    borderRadius: 12,
    padding: 10,
    gap: 4,
  },
  sectionTitle: {
    fontSize: 17,
    fontWeight: '700',
  },
  orderTitle: {
    fontSize: 14,
    fontWeight: '700',
  },
  meta: {
    color: '#475569',
    fontSize: 13,
  },
  address: {
    color: '#0f172a',
    fontSize: 14,
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
    fontWeight: '500',
  },
});
