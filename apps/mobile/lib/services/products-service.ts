import { api } from '@/lib/api/client';
import type { Product, ProductListResponse } from '@/types/product';

type RawProduct = {
  id?: string | number;
  title?: string;
  name?: string;
  description?: string;
  sellerName?: string;
  seller?: { name?: string };
  imageUrl?: string;
  image?: string;
  imageUrls?: string[];
  images?: string[];
  priceUsd?: number;
  priceUSD?: number;
  price?: number;
  priceZwl?: number;
  stockQuantity?: number;
  stock?: number;
  category?: string;
};

type RawProductResponse = {
  items?: RawProduct[];
  data?: RawProduct[];
  products?: RawProduct[];
  page?: number;
  pageNumber?: number;
  pageSize?: number;
  hasNextPage?: boolean;
  nextPage?: number | null;
  usdToZwlRate?: number;
};

type RawProductDetailResponse = {
  item?: RawProduct;
  data?: RawProduct;
  product?: RawProduct;
};

export type ListProductsParams = {
  page: number;
  pageSize: number;
  search?: string;
  category?: string;
};

const mapProduct = (raw: RawProduct): Product => {
  const fallbackId = `${raw.title ?? raw.name ?? 'product'}-${raw.price ?? raw.priceUsd ?? 0}`;

  return {
    id: String(raw.id ?? fallbackId),
    title: raw.title ?? raw.name ?? 'Untitled product',
    description: raw.description,
    sellerName: raw.sellerName ?? raw.seller?.name,
    imageUrl: raw.imageUrl ?? raw.image,
    imageUrls: raw.imageUrls ?? raw.images,
    priceUsd: Number(raw.priceUsd ?? raw.priceUSD ?? raw.price ?? 0),
    priceZwl: raw.priceZwl != null ? Number(raw.priceZwl) : undefined,
    stockQuantity:
      raw.stockQuantity != null
        ? Number(raw.stockQuantity)
        : raw.stock != null
          ? Number(raw.stock)
          : undefined,
    category: raw.category,
  };
};

const toProductListResponse = (
  data: RawProductResponse,
  params: ListProductsParams
): ProductListResponse => {
  const rawItems = data.items ?? data.data ?? data.products ?? [];
  const items = rawItems.map(mapProduct);
  const page = Number(data.page ?? data.pageNumber ?? params.page);
  const pageSize = Number(data.pageSize ?? params.pageSize);
  const hasNextPage =
    typeof data.hasNextPage === 'boolean'
      ? data.hasNextPage
      : typeof data.nextPage === 'number'
        ? true
        : items.length >= pageSize;

  return {
    items,
    page,
    pageSize,
    hasNextPage,
    usdToZwlRate: data.usdToZwlRate,
  };
};

export const productsService = {
  async list(params: ListProductsParams): Promise<ProductListResponse> {
    const response = await api.get<RawProductResponse>('/products', {
      params: {
        page: params.page,
        pageSize: params.pageSize,
        search: params.search?.trim() || undefined,
        category:
          params.category && params.category.toLowerCase() !== 'all'
            ? params.category
            : undefined,
      },
    });

    return toProductListResponse(response.data, params);
  },
  async getById(productId: string): Promise<Product> {
    const response = await api.get<RawProductDetailResponse>(`/products/${productId}`);
    const rawProduct = response.data.item ?? response.data.data ?? response.data.product;

    if (!rawProduct) {
      throw new Error('Product not found.');
    }

    return mapProduct(rawProduct);
  },
};
