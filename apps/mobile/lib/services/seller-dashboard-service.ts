import { api } from '@/lib/api/client';

type ApiEnvelope<T> = {
  data?: T;
};

type SellerProduct = {
  productId?: string;
  id?: string;
};

type SellerProductsResponse = {
  items?: SellerProduct[];
  totalCount?: number;
};

type SellerRecentOrder = {
  id: string;
  status: string;
  createdAt: string;
  totalUsd: number;
};

type SellerOrderRaw = {
  orderId?: string;
  id?: string;
  status?: string;
  createdAt?: string;
  createdOn?: string;
  totalUsd?: number;
  amountUsd?: number;
  orderTotalUsd?: number;
};

type SellerOrdersResponse = {
  items?: SellerOrderRaw[];
  orders?: SellerOrderRaw[];
  totalCount?: number;
};

export type SellerDashboardStats = {
  activeListings: number;
  ordersPending: number;
  totalEarnedUsd: number;
  recentOrders: SellerRecentOrder[];
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

const normalizeOrder = (raw: SellerOrderRaw): SellerRecentOrder => ({
  id: String(raw.orderId ?? raw.id ?? 'unknown'),
  status: raw.status?.trim() || 'Unknown',
  createdAt: raw.createdAt ?? raw.createdOn ?? new Date().toISOString(),
  totalUsd: Number(raw.totalUsd ?? raw.amountUsd ?? raw.orderTotalUsd ?? 0),
});

const isPendingStatus = (status: string): boolean => {
  const normalized = status.trim().toLowerCase();
  return ['pending', 'created', 'paid', 'packed', 'outfordelivery', 'out_for_delivery'].includes(
    normalized
  );
};

const listRecentOrders = async (): Promise<SellerRecentOrder[]> => {
  try {
    const sellerResponse = await api.get<ApiEnvelope<SellerOrdersResponse>>('/orders', {
      params: { page: 1, pageSize: 5 },
    });
    const sellerData = readEnvelopeData(sellerResponse.data);
    const sellerItems = sellerData.items ?? sellerData.orders ?? [];
    return sellerItems.map(normalizeOrder);
  } catch {
    return [];
  }
};

export const sellerDashboardService = {
  async getStats(): Promise<SellerDashboardStats> {
    const productsResponse = await api.get<ApiEnvelope<SellerProductsResponse>>('/products/my', {
      params: { page: 1, pageSize: 1 },
    });
    const productsData = readEnvelopeData(productsResponse.data);
    const activeListings = Number(productsData.totalCount ?? productsData.items?.length ?? 0);

    const recentOrders = await listRecentOrders();
    const ordersPending = recentOrders.filter((order) => isPendingStatus(order.status)).length;
    const totalEarnedUsd = recentOrders.reduce((sum, order) => sum + order.totalUsd, 0);

    return {
      activeListings,
      ordersPending,
      totalEarnedUsd,
      recentOrders,
    };
  },
};
