import { ProductCard } from "@/components/store/product-card";
import type { StorefrontProduct } from "@/lib/storefront-data";

export function ProductGrid({ products, priorityCount = 4 }: { products: StorefrontProduct[]; priorityCount?: number }) {
  return (
    <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
      {products.map((p, i) => (
        <ProductCard key={p.id} product={p} priority={i < priorityCount} />
      ))}
    </div>
  );
}
