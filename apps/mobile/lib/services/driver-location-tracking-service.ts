import * as Location from 'expo-location';
import * as SecureStore from 'expo-secure-store';
import * as TaskManager from 'expo-task-manager';

import { env } from '@/lib/config/env';

const AUTH_STORAGE_KEY = 'zimmarket.auth.auth-store';
const TRACKING_TASK_NAME = 'zimmarket-driver-background-location';

type AuthStoreShape = {
  state?: {
    accessToken?: string | null;
  };
};

type EnsurePermissionResult =
  | { granted: true }
  | {
      granted: false;
      message: string;
    };

const readAccessToken = async (): Promise<string | null> => {
  const raw = await SecureStore.getItemAsync(AUTH_STORAGE_KEY);
  if (!raw) {
    return null;
  }

  try {
    const parsed = JSON.parse(raw) as AuthStoreShape;
    const token = parsed.state?.accessToken;
    return typeof token === 'string' && token.trim().length > 0 ? token : null;
  } catch {
    return null;
  }
};

const postDriverLocation = async (latitude: number, longitude: number): Promise<void> => {
  const accessToken = await readAccessToken();
  if (!accessToken) {
    return;
  }

  const baseUrl = env.EXPO_PUBLIC_API_BASE_URL.replace(/\/+$/, '');
  const url = `${baseUrl}/drivers/location`;
  const payload = JSON.stringify({ latitude, longitude });
  const headers = {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${accessToken}`,
  };

  const putResponse = await fetch(url, {
    method: 'PUT',
    headers,
    body: payload,
  });

  if (putResponse.ok) {
    return;
  }

  // Backward-compat fallback for current backend route shape.
  await fetch(url, {
    method: 'POST',
    headers,
    body: payload,
  });
};

TaskManager.defineTask(TRACKING_TASK_NAME, async ({ data, error }) => {
  if (error) {
    console.error('Driver location task failed.', error);
    return;
  }

  const locations = (data as { locations?: Location.LocationObject[] } | undefined)?.locations;
  if (!locations || locations.length === 0) {
    return;
  }

  const latest = locations[locations.length - 1];
  const { latitude, longitude } = latest.coords;
  try {
    await postDriverLocation(latitude, longitude);
  } catch (taskError) {
    console.error('Failed to post background driver location update.', taskError);
  }
});

export const driverLocationTrackingService = {
  async ensurePermissions(): Promise<EnsurePermissionResult> {
    const foreground = await Location.requestForegroundPermissionsAsync();
    if (!foreground.granted) {
      return {
        granted: false,
        message: 'Location is required to route deliveries. Please enable location permission to continue.',
      };
    }

    const background = await Location.requestBackgroundPermissionsAsync();
    if (!background.granted) {
      return {
        granted: false,
        message:
          'Background location is required while deliveries are in progress so customers can track orders live. Please allow "Always" location access.',
      };
    }

    return { granted: true };
  },

  async startTracking(): Promise<void> {
    const alreadyStarted = await Location.hasStartedLocationUpdatesAsync(TRACKING_TASK_NAME);
    if (alreadyStarted) {
      return;
    }

    await Location.startLocationUpdatesAsync(TRACKING_TASK_NAME, {
      accuracy: Location.Accuracy.Balanced,
      timeInterval: 30_000,
      distanceInterval: 0,
      pausesUpdatesAutomatically: false,
      showsBackgroundLocationIndicator: true,
      foregroundService: {
        notificationTitle: 'ZimMarket delivery tracking',
        notificationBody: 'Sharing your location while you complete deliveries.',
      },
    });
  },

  async stopTracking(): Promise<void> {
    const started = await Location.hasStartedLocationUpdatesAsync(TRACKING_TASK_NAME);
    if (!started) {
      return;
    }

    await Location.stopLocationUpdatesAsync(TRACKING_TASK_NAME);
  },
};
