import { api } from '@/lib/api/client';
import type { OrderItem, OrderStatusTab } from '@/types/order';

export type PlaceOrderPaymentMethod = 'Paynow' | 'Ecocash';

export type PlaceOrderRequest = {
  deliveryAddress: string;
  paymentMethod: PlaceOrderPaymentMethod;
  items: Array<{
    productId: string;
    quantity: number;
  }>;
};

export type PlaceOrderResponse = {
  orderId: string;
  paymentRedirectUrl: string;
};

type RawOrder = {
  orderId?: string | number;
  id?: string | number;
  status?: string;
  createdAt?: string;
  createdOn?: string;
};

type RawPlaceOrderResponse = {
  orderId?: string | number;
  id?: string | number;
  paymentRedirectUrl?: string;
  redirectUrl?: string;
  paymentUrl?: string;
};

type RawOrdersListResponse = {
  items?: RawOrder[];
  data?: RawOrder[];
  orders?: RawOrder[];
};

const normalizePlaceOrderResponse = (raw: RawPlaceOrderResponse): PlaceOrderResponse => {
  const orderId = String(raw.orderId ?? raw.id ?? '');
  const paymentRedirectUrl = raw.paymentRedirectUrl ?? raw.redirectUrl ?? raw.paymentUrl ?? '';

  if (orderId.length === 0 || paymentRedirectUrl.length === 0) {
    throw new Error('Order created, but payment information is incomplete.');
  }

  return {
    orderId,
    paymentRedirectUrl,
  };
};

const normalizeStatus = (value: string | undefined): OrderStatusTab => {
  const normalized = (value ?? '').trim().toLowerCase();

  if (['completed', 'delivered', 'success'].includes(normalized)) {
    return 'Completed';
  }

  if (['cancelled', 'canceled', 'failed'].includes(normalized)) {
    return 'Cancelled';
  }

  return 'Active';
};

const normalizeOrder = (raw: RawOrder): OrderItem => ({
  id: String(raw.orderId ?? raw.id ?? 'unknown'),
  status: normalizeStatus(raw.status),
  createdAt: raw.createdAt ?? raw.createdOn ?? new Date().toISOString(),
});

export const ordersService = {
  async placeOrder(payload: PlaceOrderRequest): Promise<PlaceOrderResponse> {
    const response = await api.post<RawPlaceOrderResponse>('/api/v1/orders', payload);
    return normalizePlaceOrderResponse(response.data);
  },
  async list(status: OrderStatusTab): Promise<OrderItem[]> {
    const response = await api.get<RawOrdersListResponse>('/api/v1/orders', {
      params: {
        status,
      },
    });

    const rawItems = response.data.items ?? response.data.data ?? response.data.orders ?? [];
    return rawItems.map(normalizeOrder).filter((item) => item.status === status);
  },
};
