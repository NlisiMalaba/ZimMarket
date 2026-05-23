import { api } from "@/lib/api";

type ApiSuccessResponse<T> = {
  data: T;
};

type PagedList<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type SellerOrderStatusGroup = "Completed" | "Processing" | "Pending" | "Cancelled";

export type SellerOrderSummary = {
  orderId: string;
  status: number | string;
  paymentStatus: number | string;
  totalUsd: number;
  sellerTotalUsd: number;
  sellerLineItemCount: number;
  createdAt: string;
  customerName: string;
  customerEmail: string;
  primaryProductTitle: string;
};

export type SellerOrderDetailItem = {
  productId: string;
  productTitle: string;
  quantity: number;
  unitPriceUsd: number;
  lineTotalUsd: number;
};

export type SellerOrderDetail = {
  orderId: string;
  status: number | string;
  paymentStatus: number | string;
  totalUsd: number;
  customerCity: string;
  items: SellerOrderDetailItem[];
};

type SellerOrderListRaw = {
  orderId?: string;
  status?: number | string;
  paymentStatus?: number | string;
  totalUsd?: number;
  sellerTotalUsd?: number;
  sellerLineItemCount?: number;
  createdAt?: string;
  customerName?: string;
  customerEmail?: string;
  primaryProductTitle?: string;
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
  status?: number | string;
  paymentStatus?: number | string;
  totalUsd?: number;
  customerCity?: string;
  items?: SellerOrderDetailItemRaw[];
};

function normalizeOrder(raw: SellerOrderListRaw): SellerOrderSummary {
  return {
    orderId: String(raw.orderId ?? ""),
    status: raw.status ?? 0,
    paymentStatus: raw.paymentStatus ?? 0,
    totalUsd: Number(raw.totalUsd ?? 0),
    sellerTotalUsd: Number(raw.sellerTotalUsd ?? raw.totalUsd ?? 0),
    sellerLineItemCount: Number(raw.sellerLineItemCount ?? 0),
    createdAt: raw.createdAt ?? new Date().toISOString(),
    customerName: raw.customerName?.trim() || "Unknown customer",
    customerEmail: raw.customerEmail?.trim() || "",
    primaryProductTitle: raw.primaryProductTitle?.trim() || "Untitled product",
  };
}

function normalizeDetailItem(raw: SellerOrderDetailItemRaw): SellerOrderDetailItem {
  return {
    productId: String(raw.productId ?? ""),
    productTitle: raw.productTitle?.trim() || "Untitled",
    quantity: Number(raw.quantity ?? 0),
    unitPriceUsd: Number(raw.unitPriceUsd ?? 0),
    lineTotalUsd: Number(raw.lineTotalUsd ?? 0),
  };
}

function normalizeDetail(raw: SellerOrderDetailRaw, orderId: string): SellerOrderDetail {
  return {
    orderId: String(raw.orderId ?? orderId),
    status: raw.status ?? 0,
    paymentStatus: raw.paymentStatus ?? 0,
    totalUsd: Number(raw.totalUsd ?? 0),
    customerCity: raw.customerCity?.trim() || "Unknown city",
    items: (raw.items ?? []).map(normalizeDetailItem),
  };
}

export function formatOrderReference(orderId: string): string {
  const compact = orderId.replaceAll("-", "").slice(0, 8).toUpperCase();
  return compact ? `ORD-${compact}` : "ORD-UNKNOWN";
}

export function getCustomerInitials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return "?";
  }

  if (parts.length === 1) {
    return parts[0].slice(0, 2).toUpperCase();
  }

  return `${parts[0][0] ?? ""}${parts[parts.length - 1][0] ?? ""}`.toUpperCase();
}

export const sellerOrdersService = {
  async listOrders(params: {
    page: number;
    pageSize: number;
    statusGroup?: SellerOrderStatusGroup;
  }): Promise<PagedList<SellerOrderSummary>> {
    const response = await api.get<ApiSuccessResponse<PagedList<SellerOrderListRaw>>>(
      "/api/v1/orders/seller",
      {
        query: {
          page: params.page,
          pageSize: params.pageSize,
          statusGroup: params.statusGroup,
        },
      },
    );

    return {
      ...response.data,
      items: response.data.items.map(normalizeOrder),
    };
  },

  async getOrderById(orderId: string): Promise<SellerOrderDetail> {
    const response = await api.get<ApiSuccessResponse<SellerOrderDetailRaw>>(
      `/api/v1/orders/seller/${orderId}`,
    );

    return normalizeDetail(response.data, orderId);
  },
};
