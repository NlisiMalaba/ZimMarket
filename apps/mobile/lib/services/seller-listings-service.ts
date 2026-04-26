import { api } from '@/lib/api/client';

type ApiEnvelope<T> = {
  data?: T;
};

type SellerProductRaw = {
  productId?: string;
  id?: string;
  title?: string;
  status?: string;
  priceAmount?: number;
  priceUsd?: number;
  stockQuantity?: number;
  categoryName?: string;
};

type SellerProductsResponse = {
  items?: SellerProductRaw[];
};

export type SellerListing = {
  id: string;
  title: string;
  status: string;
  priceUsd: number;
  stockQuantity: number;
  categoryName: string;
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

const normalizeListing = (raw: SellerProductRaw): SellerListing => ({
  id: String(raw.productId ?? raw.id ?? 'unknown'),
  title: raw.title?.trim() || 'Untitled product',
  status: raw.status?.trim() || 'Unknown',
  priceUsd: Number(raw.priceAmount ?? raw.priceUsd ?? 0),
  stockQuantity: Number(raw.stockQuantity ?? 0),
  categoryName: raw.categoryName?.trim() || 'Uncategorized',
});

export const sellerListingsService = {
  async listMine(page = 1, pageSize = 20): Promise<SellerListing[]> {
    const response = await api.get<ApiEnvelope<SellerProductsResponse>>('/products/my', {
      params: { page, pageSize },
    });
    const data = readEnvelopeData(response.data);
    return (data.items ?? []).map(normalizeListing);
  },
};
