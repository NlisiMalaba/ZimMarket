import * as SecureStore from 'expo-secure-store';

import { api } from '@/lib/api/client';
import { driverBatchesService } from '@/lib/services/driver-batches-service';
import { fileUploadService, type UploadableImage } from '@/lib/services/file-upload-service';

export type ActiveDeliveryOrder = {
  orderId: string;
  area: string;
  deliveryAddress: string;
};

export type ActiveDeliveryData = {
  batchId: string;
  totalOrders: number;
  deliveredCount: number;
  nextOrder: ActiveDeliveryOrder | null;
  pendingOrders: ActiveDeliveryOrder[];
};

const deliveredStorageKey = (batchId: string): string => `zimmarket.driver.batch.${batchId}.delivered`;

const mockAddressByArea: Record<string, string> = {
  CBD: 'Samora Machel Ave, Harare CBD',
  Avondale: 'King George Rd, Avondale, Harare',
  Borrowdale: 'Borrowdale Rd, Harare',
  Highlands: 'Enterprise Rd, Highlands, Harare',
  Belvedere: 'Sam Nujoma St, Belvedere, Harare',
};

const readDeliveredSet = async (batchId: string): Promise<Set<string>> => {
  const raw = await SecureStore.getItemAsync(deliveredStorageKey(batchId));
  if (!raw) {
    return new Set<string>();
  }

  try {
    const parsed = JSON.parse(raw) as string[];
    return new Set(parsed.map((value) => String(value)));
  } catch {
    return new Set<string>();
  }
};

const writeDeliveredSet = async (batchId: string, deliveredOrderIds: Set<string>): Promise<void> => {
  await SecureStore.setItemAsync(deliveredStorageKey(batchId), JSON.stringify(Array.from(deliveredOrderIds)));
};

const flattenBatchOrders = async (batchId: string): Promise<ActiveDeliveryOrder[]> => {
  const detail = await driverBatchesService.getById(batchId);
  return detail.groupedOrders.flatMap((group) =>
    group.orderIds.map((orderId) => ({
      orderId,
      area: group.area,
      deliveryAddress: mockAddressByArea[group.area] ?? `${group.area}, Harare`,
    }))
  );
};

export const driverActiveDeliveryService = {
  async getActiveDeliveryData(batchId: string): Promise<ActiveDeliveryData> {
    const [allOrders, deliveredOrderIds] = await Promise.all([flattenBatchOrders(batchId), readDeliveredSet(batchId)]);
    const pendingOrders = allOrders.filter((order) => !deliveredOrderIds.has(order.orderId));

    return {
      batchId,
      totalOrders: allOrders.length,
      deliveredCount: allOrders.length - pendingOrders.length,
      nextOrder: pendingOrders[0] ?? null,
      pendingOrders,
    };
  },

  async uploadDeliveryPhoto(file: UploadableImage): Promise<string> {
    const presigned = await fileUploadService.getPresignedUploadUrl({
      fileType: 6,
      contentType: file.contentType,
      fileSizeBytes: file.fileSizeBytes,
    });

    await fileUploadService.uploadToPresignedUrl({
      uploadUrl: presigned.uploadUrl,
      file,
    });

    return presigned.fileKey;
  },

  async confirmDelivery(params: { batchId: string; orderId: string; deliveryPhotoKey: string }): Promise<void> {
    await api.post(`/drivers/batches/${params.batchId}/orders/${params.orderId}/delivered`, {
      deliveryPhotoKey: params.deliveryPhotoKey,
    });

    const deliveredOrderIds = await readDeliveredSet(params.batchId);
    deliveredOrderIds.add(params.orderId);
    await writeDeliveredSet(params.batchId, deliveredOrderIds);
  },
};
