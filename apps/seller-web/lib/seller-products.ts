import { api } from "@/lib/api";
import { uploadProductImage } from "@/lib/file-upload";

type ApiSuccessResponse<T> = {
  data: T;
};

type PagedList<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type SellerProductListScope = "active" | "deleted";

export type Category = {
  id: string;
  name: string;
  slug: string;
  parentCategoryId: string | null;
};

export type SellerProductSummary = {
  productId: string;
  status: number | string;
  title: string;
  priceAmount: number;
  stockQuantity: number;
  categoryId: string;
  categoryName: string;
  primaryImageUrl: string | null;
  updatedAt: string;
};

export type SellerProductDetail = {
  productId: string;
  status: number | string;
  title: string;
  description: string;
  priceAmount: number;
  stockQuantity: number;
  categoryId: string;
  categoryName: string;
  pickupAddress: {
    street: string;
    suburb: string;
    city: string;
    country: string;
  };
  imageKeys: string[];
  imageUrls: string[];
  updatedAt: string;
};

export type ProductFormValues = {
  title: string;
  description: string;
  priceUsd: number;
  categoryId: string;
  stockQuantity: number;
  pickupAddress: {
    street: string;
    suburb: string;
    city: string;
    country: string;
  };
};

export type ExistingProductImage = {
  key: string;
  url: string;
};

export const DELETED_PRODUCT_RETENTION_DAYS = 30;
export const MAX_PRODUCT_IMAGES = 5;

function scopeToQuery(scope: SellerProductListScope): number {
  return scope === "deleted" ? 1 : 0;
}

function readData<T>(response: ApiSuccessResponse<T>): T {
  return response.data;
}

type CategoryRaw = {
  categoryId: string;
  name: string;
  slug: string;
  parentCategoryId?: string | null;
};

type CreateProductPayload = ProductFormValues & {
  imageKeys: string[];
};

type UpdateProductPayload = Omit<ProductFormValues, "stockQuantity"> & {
  imageKeys: string[];
};

export function daysUntilPermanentDeletion(updatedAt: string): number {
  const deletedAt = new Date(updatedAt);
  if (Number.isNaN(deletedAt.getTime())) {
    return DELETED_PRODUCT_RETENTION_DAYS;
  }

  const purgeAt =
    deletedAt.getTime() + DELETED_PRODUCT_RETENTION_DAYS * 24 * 60 * 60 * 1000;
  return Math.max(0, Math.ceil((purgeAt - Date.now()) / (24 * 60 * 60 * 1000)));
}

export const sellerProductsService = {
  async listCategories(): Promise<Category[]> {
    const response = await api.get<ApiSuccessResponse<CategoryRaw[]>>("/api/v1/products/categories");
    const items = readData(response) ?? [];
    return items.map((category) => ({
      id: category.categoryId,
      name: category.name,
      slug: category.slug,
      parentCategoryId: category.parentCategoryId ?? null,
    }));
  },

  async listProducts(params: {
    page: number;
    pageSize: number;
    scope: SellerProductListScope;
  }): Promise<PagedList<SellerProductSummary>> {
    const response = await api.get<ApiSuccessResponse<PagedList<SellerProductSummary>>>(
      "/api/v1/products/my",
      {
        query: {
          page: params.page,
          pageSize: params.pageSize,
          scope: scopeToQuery(params.scope),
        },
      },
    );

    return readData(response);
  },

  async getProduct(productId: string): Promise<SellerProductDetail> {
    const response = await api.get<ApiSuccessResponse<{
      productId: string;
      status: number | string;
      title: string;
      description: string;
      priceAmount: number;
      stockQuantity: number;
      categoryId: string;
      categoryName: string;
      pickupStreet: string;
      pickupSuburb: string;
      pickupCity: string;
      pickupCountry: string;
      imageKeys: string[];
      imageUrls: string[];
      updatedAt: string;
    }>>(`/api/v1/products/my/${productId}`);

    const data = readData(response);

    return {
      productId: data.productId,
      status: data.status,
      title: data.title,
      description: data.description,
      priceAmount: data.priceAmount,
      stockQuantity: data.stockQuantity,
      categoryId: data.categoryId,
      categoryName: data.categoryName,
      pickupAddress: {
        street: data.pickupStreet,
        suburb: data.pickupSuburb,
        city: data.pickupCity,
        country: data.pickupCountry,
      },
      imageKeys: data.imageKeys ?? [],
      imageUrls: data.imageUrls ?? [],
      updatedAt: data.updatedAt,
    };
  },

  async createProduct(
    values: ProductFormValues,
    newImageFiles: File[],
  ): Promise<string> {
    const imageKeys: string[] = [];
    for (const file of newImageFiles) {
      imageKeys.push(await uploadProductImage(file));
    }

    const payload: CreateProductPayload = {
      ...values,
      imageKeys,
    };

    const response = await api.post<ApiSuccessResponse<string>>("/api/v1/products", payload);
    return String(readData(response));
  },

  async updateProduct(params: {
    productId: string;
    values: ProductFormValues;
    retainedImageKeys: string[];
    newImageFiles: File[];
    previousStockQuantity: number;
  }): Promise<void> {
    const newImageKeys: string[] = [];
    for (const file of params.newImageFiles) {
      newImageKeys.push(await uploadProductImage(file));
    }

    const imageKeys = [...params.retainedImageKeys, ...newImageKeys];
    const { stockQuantity, ...rest } = params.values;

    const payload: UpdateProductPayload = {
      ...rest,
      imageKeys,
    };

    await api.put(`/api/v1/products/${params.productId}`, payload);

    const delta = stockQuantity - params.previousStockQuantity;
    if (delta !== 0) {
      await api.patch(`/api/v1/products/${params.productId}/stock`, { delta });
    }
  },

  async deleteProduct(productId: string): Promise<void> {
    await api.delete(`/api/v1/products/${productId}`);
  },
};
