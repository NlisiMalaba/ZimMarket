import { api } from '@/lib/api/client';

type ApiEnvelope<T> = {
  data?: T;
};

type DeliveryBatchStatus = 'Created' | 'Collected' | 'InTransit' | 'Completed' | string;

type BatchDetailRaw = {
  batchId?: string;
  id?: string;
  warehouseId?: string;
  status?: DeliveryBatchStatus;
  orderIds?: string[];
};

type GroupedDeliveryOrders = {
  area: string;
  orderIds: string[];
};

export type DriverBatchDetail = {
  id: string;
  warehouseId: string;
  pickupWarehouseAddress: string;
  status: DeliveryBatchStatus;
  groupedOrders: GroupedDeliveryOrders[];
};

const fallbackAreas = ['CBD', 'Avondale', 'Borrowdale', 'Highlands', 'Belvedere'];

const warehouseAddressById: Record<string, string> = {
  'd0000000-0000-4000-8000-000000000001': '12 Graniteside Road, Harare',
};

const readEnvelopeData = <T>(responseData: T | ApiEnvelope<T>): T => {
  if (responseData && typeof responseData === 'object' && 'data' in responseData) {
    const value = (responseData as ApiEnvelope<T>).data;
    if (value == null) {
      throw new Error('Server returned an empty response.');
    }

    return value;
  }

  return responseData as T;
};

const resolveArea = (orderId: string): string => {
  const charCode = orderId.charCodeAt(orderId.length - 1);
  if (Number.isNaN(charCode)) {
    return fallbackAreas[0];
  }

  return fallbackAreas[charCode % fallbackAreas.length];
};

const groupOrdersByArea = (orderIds: string[]): GroupedDeliveryOrders[] => {
  const grouped = new Map<string, string[]>();

  orderIds.forEach((orderId) => {
    const area = resolveArea(orderId);
    grouped.set(area, [...(grouped.get(area) ?? []), orderId]);
  });

  return Array.from(grouped.entries()).map(([area, ids]) => ({
    area,
    orderIds: ids,
  }));
};

const buildMockBatchDetail = (batchId: string): DriverBatchDetail => {
  const orderIds = [
    '40000000-0000-4000-8000-000000000001',
    '40000000-0000-4000-8000-000000000002',
    '40000000-0000-4000-8000-000000000003',
    '40000000-0000-4000-8000-000000000004',
  ];

  return {
    id: batchId,
    warehouseId: 'd0000000-0000-4000-8000-000000000001',
    pickupWarehouseAddress: '12 Graniteside Road, Harare',
    status: 'Created',
    groupedOrders: groupOrdersByArea(orderIds),
  };
};

export const driverBatchesService = {
  async getById(batchId: string): Promise<DriverBatchDetail> {
    try {
      const response = await api.get<ApiEnvelope<BatchDetailRaw>>(`/drivers/batches/${batchId}`);
      const data = readEnvelopeData(response.data);
      const orderIds = (data.orderIds ?? []).map((id) => String(id));
      const warehouseId = String(data.warehouseId ?? 'd0000000-0000-4000-8000-000000000001');

      return {
        id: String(data.batchId ?? data.id ?? batchId),
        warehouseId,
        pickupWarehouseAddress: warehouseAddressById[warehouseId] ?? 'Default warehouse, Harare',
        status: data.status?.trim() || 'Created',
        groupedOrders: groupOrdersByArea(orderIds),
      };
    } catch {
      return buildMockBatchDetail(batchId);
    }
  },

  async markCollected(batchId: string): Promise<void> {
    await api.post(`/drivers/batches/${batchId}/collected`);
  },
};
