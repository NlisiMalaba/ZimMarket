export type ProductCategory =
  | 'All'
  | 'Electronics'
  | 'Phones'
  | 'Fashion'
  | 'Home'
  | 'Beauty'
  | 'Automotive'
  | 'Household';

export type Product = {
  id: string;
  title: string;
  description?: string;
  sellerName?: string;
  imageUrl?: string;
  imageUrls?: string[];
  priceUsd: number;
  priceZwl?: number;
  stockQuantity?: number;
  category?: string;
};

export type ProductListResponse = {
  items: Product[];
  page: number;
  pageSize: number;
  hasNextPage: boolean;
  usdToZwlRate?: number;
};
