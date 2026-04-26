export type SellerOrderListItem = {
  id: string;
  status: string;
  paymentStatus: string;
  totalUsd: number;
  sellerLineItemCount: number;
  createdAt: string;
};

export type SellerOrderDetailItem = {
  productId: string;
  productTitle: string;
  quantity: number;
  unitPriceUsd: number;
  lineTotalUsd: number;
};

export type SellerOrderDetail = {
  id: string;
  status: string;
  paymentStatus: string;
  totalUsd: number;
  customerCity: string;
  items: SellerOrderDetailItem[];
};

