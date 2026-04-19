import type { Product } from '@/types/product';

export type CartItem = {
  productId: string;
  title: string;
  imageUrl?: string;
  unitPriceUsd: number;
  unitPriceZwl: number;
  quantity: number;
  maxQuantity?: number;
};

export const toCartItemFromProduct = (product: Product, usdToZwlRate = 30): CartItem => ({
  productId: product.id,
  title: product.title,
  imageUrl: product.imageUrl,
  unitPriceUsd: product.priceUsd,
  unitPriceZwl: product.priceZwl ?? product.priceUsd * usdToZwlRate,
  quantity: 1,
  maxQuantity: product.stockQuantity,
});
