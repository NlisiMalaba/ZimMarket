import { api } from '@/lib/api/client';
import { fileUploadService, type UploadableImage } from '@/lib/services/file-upload-service';
import type { Category } from '@/types/catalogue';
import type { Product } from '@/types/product';

type ApiEnvelope<T> = {
  data?: T;
};

type CategoryRaw = {
  categoryId?: string;
  name?: string;
  slug?: string;
  parentCategoryId?: string | null;
};

type ProductDetailRaw = {
  productId?: string;
  title?: string;
  description?: string;
  priceAmount?: number;
  priceUsd?: number;
  stockQuantity?: number;
  categoryId?: string;
  categoryName?: string;
  pickupStreet?: string;
  pickupSuburb?: string;
  pickupCity?: string;
  pickupCountry?: string;
  imageUrls?: string[];
};

export type ProductDetail = Product & {
  categoryId?: string;
  categoryName?: string;
  pickupAddress?: {
    street: string;
    suburb: string;
    city: string;
    country: string;
  };
  imageUrls?: string[];
};

type CreateProductRequest = {
  title: string;
  description: string;
  priceUsd: number;
  categoryId: string;
  stockQuantity: number;
  imageKeys: string[];
  pickupAddress: {
    street: string;
    suburb: string;
    city: string;
    country: string;
  };
};

type UpdateProductRequest = Omit<CreateProductRequest, 'stockQuantity'>;

const PRODUCT_IMAGE_FILE_TYPE = 1;

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

const normalizeCategory = (raw: CategoryRaw): Category => ({
  id: String(raw.categoryId ?? ''),
  name: raw.name?.trim() || 'Unknown',
  slug: raw.slug?.trim() || '',
  parentCategoryId: raw.parentCategoryId ?? null,
});

const ensureGuidLike = (value: string): void => {
  if (!/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value)) {
    throw new Error('Invalid category selection.');
  }
};

const uploadImages = async (images: UploadableImage[]): Promise<string[]> => {
  const keys: string[] = [];

  for (const image of images) {
    const presigned = await fileUploadService.getPresignedUploadUrl({
      fileType: PRODUCT_IMAGE_FILE_TYPE,
      contentType: image.contentType,
      fileSizeBytes: image.fileSizeBytes,
    });

    await fileUploadService.uploadToPresignedUrl({
      uploadUrl: presigned.uploadUrl,
      file: image,
    });

    keys.push(presigned.fileKey);
  }

  return keys;
};

export const sellerProductsService = {
  async listCategories(): Promise<Category[]> {
    const response = await api.get<ApiEnvelope<CategoryRaw[]>>('/products/categories');
    const data = readEnvelopeData(response.data);
    return (data ?? []).map(normalizeCategory).filter((category) => category.id.length > 0);
  },

  async createListing(params: {
    title: string;
    description: string;
    priceUsd: number;
    categoryId: string;
    stockQuantity: number;
    pickupAddress: { street: string; suburb: string; city: string; country: string };
    images: UploadableImage[];
  }): Promise<void> {
    ensureGuidLike(params.categoryId);

    const imageKeys = await uploadImages(params.images);
    const payload: CreateProductRequest = {
      title: params.title,
      description: params.description,
      priceUsd: params.priceUsd,
      categoryId: params.categoryId,
      stockQuantity: params.stockQuantity,
      imageKeys,
      pickupAddress: params.pickupAddress,
    };

    await api.post('/products', payload);
  },

  async updateListing(params: {
    productId: string;
    title: string;
    description: string;
    priceUsd: number;
    categoryId: string;
    pickupAddress: { street: string; suburb: string; city: string; country: string };
    images: UploadableImage[];
    previousStockQuantity?: number;
    nextStockQuantity?: number;
  }): Promise<void> {
    ensureGuidLike(params.categoryId);

    const imageKeys = await uploadImages(params.images);
    const payload: UpdateProductRequest = {
      title: params.title,
      description: params.description,
      priceUsd: params.priceUsd,
      categoryId: params.categoryId,
      imageKeys,
      pickupAddress: params.pickupAddress,
    };

    await api.put(`/products/${params.productId}`, payload);

    if (
      typeof params.previousStockQuantity === 'number' &&
      typeof params.nextStockQuantity === 'number' &&
      params.previousStockQuantity !== params.nextStockQuantity
    ) {
      const delta = params.nextStockQuantity - params.previousStockQuantity;
      await api.patch(`/products/${params.productId}/stock`, { delta });
    }
  },

  async getProduct(productId: string): Promise<ProductDetail> {
    const response = await api.get<ApiEnvelope<ProductDetailRaw>>(`/products/${productId}`);
    const data = readEnvelopeData(response.data);

    return {
      id: String(data.productId ?? productId),
      title: data.title?.trim() || 'Untitled product',
      description: data.description ?? undefined,
      priceUsd: Number(data.priceAmount ?? data.priceUsd ?? 0),
      stockQuantity: typeof data.stockQuantity === 'number' ? data.stockQuantity : undefined,
      category: data.categoryName ?? undefined,
      categoryId: data.categoryId ?? undefined,
      categoryName: data.categoryName ?? undefined,
      pickupAddress: {
        street: data.pickupStreet?.trim() || '',
        suburb: data.pickupSuburb?.trim() || '',
        city: data.pickupCity?.trim() || '',
        country: data.pickupCountry?.trim() || 'Zimbabwe',
      },
      imageUrls: Array.isArray(data.imageUrls) ? data.imageUrls : undefined,
    };
  },
};

