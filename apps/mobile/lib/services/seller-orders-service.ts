import { api } from '@/lib/api/client';
import type { SellerOrderDetail, SellerOrderDetailItem, SellerOrderListItem } from '@/types/seller-order';

type ApiEnvelope<T> = {
  data?: T;
};

type SellerOrderListRaw = {
  orderId?: string;
  id?: string;
  status?: string;
  paymentStatus?: string;
  totalUsd?: number;
  sellerLineItemCount?: number;
  createdAt?: string;
};

type SellerOrdersResponse = {
  items?: SellerOrderListRaw[];
};

type SellerOrderDetailItemRaw = {
  productId?: string;
  productTitle?: string;
  quantity?: number;
  unitPriceUsd?: number;
  lineTotalUsd?: number;
};

type SellerOrderDetailRaw = {
  orderId?: string;
  id?: string;
  status?: string;
  paymentStatus?: string;
  totalUsd?: number;
  customerCity?: string;
  items?: SellerOrderDetailItemRaw[];
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

const normalizeListItem = (raw: SellerOrderListRaw): SellerOrderListItem => ({
  id: String(raw.orderId ?? raw.id ?? 'unknown'),
  status: raw.status?.trim() || 'Unknown',
  paymentStatus: raw.paymentStatus?.trim() || 'Unknown',
  totalUsd: Number(raw.totalUsd ?? 0),
  sellerLineItemCount: Number(raw.sellerLineItemCount ?? 0),
  createdAt: raw.createdAt ?? new Date().toISOString(),
});

const normalizeDetailItem = (raw: SellerOrderDetailItemRaw): SellerOrderDetailItem => ({
  productId: String(raw.productId ?? ''),
  productTitle: raw.productTitle?.trim() || 'Untitled',
  quantity: Number(raw.quantity ?? 0),
  unitPriceUsd: Number(raw.unitPriceUsd ?? 0),
  lineTotalUsd: Number(raw.lineTotalUsd ?? 0),
});

export const sellerOrdersService = {
  async list(): Promise<SellerOrderListItem[]> {
    const response = await api.get<ApiEnvelope<SellerOrdersResponse>>('/orders/seller', {
      params: { page: 1, pageSize: 30 },
    });
    const data = readEnvelopeData(response.data);
    return (data.items ?? []).map(normalizeListItem);
  },

  async getById(orderId: string): Promise<SellerOrderDetail> {
    const response = await api.get<ApiEnvelope<SellerOrderDetailRaw>>(`/orders/seller/${orderId}`);
    const data = readEnvelopeData(response.data);

    return {
      id: String(data.orderId ?? data.id ?? orderId),
      status: data.status?.trim() || 'Unknown',
      paymentStatus: data.paymentStatus?.trim() || 'Unknown',
      totalUsd: Number(data.totalUsd ?? 0),
      customerCity: data.customerCity?.trim() || 'Unknown city',
      items: (data.items ?? []).map(normalizeDetailItem),
    };
  },
};

