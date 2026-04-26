import * as SecureStore from 'expo-secure-store';

export type DriverHomeStatus = 'Available' | 'Offline' | 'OnDelivery';

export type DriverBatchSummary = {
  id: string;
  warehouseAddress: string;
  totalOrders: number;
  deliveryArea: string;
};

export type DriverHomeData = {
  status: DriverHomeStatus;
  activeBatch: DriverBatchSummary | null;
  availableBatches: DriverBatchSummary[];
};

const DRIVER_STATUS_STORAGE_KEY = 'zimmarket.driver.status';

const mockAvailableBatches: DriverBatchSummary[] = [
  {
    id: '30000000-0000-4000-8000-000000000001',
    warehouseAddress: '12 Graniteside Road, Harare',
    totalOrders: 6,
    deliveryArea: 'Avondale',
  },
  {
    id: '30000000-0000-4000-8000-000000000002',
    warehouseAddress: '45 Simon Mazorodze Rd, Harare',
    totalOrders: 4,
    deliveryArea: 'Borrowdale',
  },
];

const parseStatus = (value: string | null): DriverHomeStatus => {
  if (value === 'Available' || value === 'Offline' || value === 'OnDelivery') {
    return value;
  }

  return 'Offline';
};

export const driverDashboardService = {
  async getHomeData(): Promise<DriverHomeData> {
    const storedStatus = await SecureStore.getItemAsync(DRIVER_STATUS_STORAGE_KEY);
    const status = parseStatus(storedStatus);

    return {
      status,
      activeBatch:
        status === 'OnDelivery'
          ? {
              id: '30000000-0000-4000-8000-000000000003',
              warehouseAddress: '12 Graniteside Road, Harare',
              totalOrders: 5,
              deliveryArea: 'CBD',
            }
          : null,
      availableBatches: status === 'Available' ? mockAvailableBatches : [],
    };
  },

  async setAvailabilityStatus(nextStatus: Extract<DriverHomeStatus, 'Available' | 'Offline'>): Promise<void> {
    await SecureStore.setItemAsync(DRIVER_STATUS_STORAGE_KEY, nextStatus);
  },
};
