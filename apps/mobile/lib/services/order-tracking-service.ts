import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';

import { env } from '@/lib/config/env';

export type DriverLocation = {
  latitude: number;
  longitude: number;
};

export type OrderTrackingUpdate = {
  status?: string;
  location?: DriverLocation;
};

type TrackingHandlers = {
  onLocationUpdated: (location: DriverLocation) => void;
  onStatusUpdated: (status: string) => void;
};

const isValidLocation = (value: unknown): value is DriverLocation => {
  if (!value || typeof value !== 'object') {
    return false;
  }

  const candidate = value as Partial<DriverLocation>;
  return typeof candidate.latitude === 'number' && typeof candidate.longitude === 'number';
};

const readLocation = (payload: unknown): DriverLocation | null => {
  if (isValidLocation(payload)) {
    return payload;
  }

  if (!payload || typeof payload !== 'object') {
    return null;
  }

  const candidate = payload as Record<string, unknown>;
  const location = candidate.location;
  if (isValidLocation(location)) {
    return location;
  }

  return null;
};

const readStatus = (payload: unknown): string | null => {
  if (typeof payload === 'string' && payload.trim().length > 0) {
    return payload.trim();
  }

  if (!payload || typeof payload !== 'object') {
    return null;
  }

  const candidate = payload as Record<string, unknown>;
  const status = candidate.status;

  return typeof status === 'string' && status.trim().length > 0 ? status.trim() : null;
};

const getOrderHubUrl = (): string => {
  const url = new URL(env.EXPO_PUBLIC_API_BASE_URL);
  url.pathname = '/hubs/orders';
  url.search = '';
  url.hash = '';
  return url.toString();
};

class OrderTrackingService {
  private connection: HubConnection | null = null;
  private activeOrderChannel: string | null = null;

  async connect(orderId: string, handlers: TrackingHandlers): Promise<void> {
    await this.disconnect();

    const channel = `order:${orderId}`;
    const connection = new HubConnectionBuilder()
      .withUrl(getOrderHubUrl())
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('LocationUpdated', (payload: unknown) => {
      const location = readLocation(payload);
      if (location) {
        handlers.onLocationUpdated(location);
      }
    });

    connection.on('OrderStatusUpdated', (payload: unknown) => {
      const status = readStatus(payload);
      if (status) {
        handlers.onStatusUpdated(status);
      }
    });

    await connection.start();

    try {
      await connection.invoke('SubscribeToOrder', channel);
    } catch {
      try {
        await connection.invoke('Subscribe', channel);
      } catch {
        console.warn(`SignalR subscribe failed for channel "${channel}".`);
      }
    }

    this.connection = connection;
    this.activeOrderChannel = channel;
  }

  async disconnect(): Promise<void> {
    if (!this.connection) {
      return;
    }

    const connection = this.connection;
    const channel = this.activeOrderChannel;
    this.connection = null;
    this.activeOrderChannel = null;

    if (connection.state === HubConnectionState.Connected && channel) {
      try {
        await connection.invoke('UnsubscribeFromOrder', channel);
      } catch {
        // Connection close should continue even if unsubscribe fails.
      }
    }

    try {
      await connection.stop();
    } catch {
      console.warn('Failed to stop SignalR order tracking connection cleanly.');
    }
  }
}

export const orderTrackingService = new OrderTrackingService();
