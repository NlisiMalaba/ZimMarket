import { useEffect, useMemo, useRef, useState, type ElementRef } from 'react';
import { ActivityIndicator, StyleSheet } from 'react-native';
import MapView, { MarkerAnimated } from 'react-native-maps';
import { useLocalSearchParams } from 'expo-router';

import { Text, View } from '@/components/Themed';
import {
  DriverLocation,
  orderTrackingService,
} from '@/lib/services/order-tracking-service';

const TIMELINE_STEPS = ['Created', 'Paid', 'Packed', 'OutForDelivery', 'Delivered'] as const;
const INITIAL_LOCATION: DriverLocation = {
  latitude: -17.824858,
  longitude: 31.053028,
};

const readParam = (value: string | string[] | undefined): string => {
  if (Array.isArray(value)) {
    return value[0] ?? '';
  }

  return value ?? '';
};

const normalizeStatus = (value: string): string => {
  const normalized = value.trim().toLowerCase();
  if (normalized.length === 0) {
    return 'Created';
  }

  return normalized
    .replace(/\s+/g, '')
    .replace('_', '')
    .replace('outfordelivery', 'OutForDelivery')
    .replace('delivered', 'Delivered')
    .replace('packed', 'Packed')
    .replace('paid', 'Paid')
    .replace('created', 'Created');
};

const getStepIndex = (status: string): number => {
  const mapped = normalizeStatus(status);
  const index = TIMELINE_STEPS.findIndex((step) => step === mapped);
  return index >= 0 ? index : 0;
};

export default function OrderTrackingScreen() {
  const params = useLocalSearchParams();
  const orderId = readParam(params.orderId);
  const [isConnecting, setIsConnecting] = useState(true);
  const [connectionError, setConnectionError] = useState<string | null>(null);
  const [status, setStatus] = useState('Created');
  const [driverLocation, setDriverLocation] = useState<DriverLocation>(INITIAL_LOCATION);
  const markerRef = useRef<ElementRef<typeof MarkerAnimated> | null>(null);

  useEffect(() => {
    let isMounted = true;

    const connectToHub = async () => {
      if (orderId.length === 0) {
        setConnectionError('Missing order ID for tracking.');
        setIsConnecting(false);
        return;
      }

      setIsConnecting(true);
      setConnectionError(null);
      try {
        await orderTrackingService.connect(orderId, {
          onLocationUpdated: (location) => {
            setDriverLocation(location);
            markerRef.current?.animateMarkerToCoordinate(location, 600);
          },
          onStatusUpdated: (nextStatus) => {
            setStatus(nextStatus);
          },
        });
      } catch {
        if (isMounted) {
          setConnectionError('Unable to connect to live tracking updates.');
        }
      } finally {
        if (isMounted) {
          setIsConnecting(false);
        }
      }
    };

    void connectToHub();

    return () => {
      isMounted = false;
      void orderTrackingService.disconnect();
    };
  }, [orderId]);

  const activeStep = useMemo(() => getStepIndex(status), [status]);

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Order Tracking</Text>
      <Text style={styles.orderLabel}>Order #{orderId || 'Unknown'}</Text>

      <View style={styles.mapContainer}>
        <MapView
          style={styles.map}
          region={{
            latitude: driverLocation.latitude,
            longitude: driverLocation.longitude,
            latitudeDelta: 0.02,
            longitudeDelta: 0.02,
          }}
        >
          <MarkerAnimated
            ref={markerRef}
            coordinate={driverLocation}
            title="Driver"
            description="Live location update"
          />
        </MapView>
        {isConnecting ? (
          <View style={styles.mapOverlay}>
            <ActivityIndicator size="small" color="#0f766e" />
            <Text>Connecting to live updates...</Text>
          </View>
        ) : null}
      </View>

      {connectionError ? <Text style={styles.errorText}>{connectionError}</Text> : null}

      <View style={styles.timelineContainer}>
        <Text style={styles.timelineTitle}>Delivery status</Text>
        {TIMELINE_STEPS.map((step, index) => {
          const isDone = index <= activeStep;
          return (
            <View key={step} style={styles.timelineRow}>
              <View style={[styles.timelineDot, isDone ? styles.timelineDotDone : null]} />
              <Text style={[styles.timelineText, isDone ? styles.timelineTextDone : null]}>{step}</Text>
            </View>
          );
        })}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 16,
    gap: 10,
  },
  title: {
    fontSize: 24,
    fontWeight: '700',
  },
  orderLabel: {
    color: '#4b5563',
    fontSize: 13,
    marginBottom: 4,
  },
  mapContainer: {
    borderWidth: 1,
    borderColor: '#e5e7eb',
    borderRadius: 12,
    overflow: 'hidden',
    height: 260,
  },
  map: {
    flex: 1,
  },
  mapOverlay: {
    position: 'absolute',
    top: 10,
    left: 10,
    right: 10,
    backgroundColor: '#ffffffee',
    borderRadius: 8,
    paddingVertical: 8,
    paddingHorizontal: 10,
    alignItems: 'center',
    gap: 4,
  },
  errorText: {
    color: '#dc2626',
    fontWeight: '600',
  },
  timelineContainer: {
    borderWidth: 1,
    borderColor: '#e5e7eb',
    borderRadius: 12,
    padding: 12,
    gap: 8,
  },
  timelineTitle: {
    fontSize: 15,
    fontWeight: '700',
  },
  timelineRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  timelineDot: {
    width: 10,
    height: 10,
    borderRadius: 999,
    backgroundColor: '#d1d5db',
  },
  timelineDotDone: {
    backgroundColor: '#0f766e',
  },
  timelineText: {
    color: '#6b7280',
    fontSize: 13,
  },
  timelineTextDone: {
    color: '#0f766e',
    fontWeight: '700',
  },
});
