import Link from "next/link";
import { notFound } from "next/navigation";

import { ProductGrid } from "@/components/store/product-grid";
import { getCategoryBySlug, STOREFRONT_PRODUCTS } from "@/lib/storefront-data";

type PageProps = {
  params: Promise<{ slug: string }>;
  searchParams: Promise<{ q?: string }>;
};

export async function generateMetadata({ params }: PageProps) {
  const { slug } = await params;
  const decoded = decodeURIComponent(slug);
  const category = getCategoryBySlug(decoded);
  const label = category?.name ?? decoded.replace(/-/g, " ");
  return { title: label.charAt(0).toUpperCase() + label.slice(1) };
}

export default async function CategoryPage({ params, searchParams }: PageProps) {
  const { slug } = await params;
  const sp = await searchParams;
  const q = (sp.q ?? "").trim().toLowerCase();
  const qDisplay = (sp.q ?? "").trim();
  const decoded = decodeURIComponent(slug);
  const category = getCategoryBySlug(decoded);
  if (!category) notFound();

  const products = q
    ? STOREFRONT_PRODUCTS.filter((p) => `${p.name} ${p.sellerName}`.toLowerCase().includes(q))
    : STOREFRONT_PRODUCTS;

  return (
    <div className="pb-16">
      <div className="border-b border-border bg-page-elevated">
        <div className="container-store py-8 sm:py-10">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.2em] text-brand">Category</p>
              <h1 className="mt-2 font-display text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">
                {category.name}
              </h1>
              <p className="mt-3 max-w-2xl text-sm text-muted sm:text-base">{category.description}</p>
              {q ? (
                <p className="mt-2 text-sm text-muted">
                  Filtered by search: <span className="font-semibold text-foreground">&ldquo;{qDisplay}&rdquo;</span>
                </p>
              ) : null}
            </div>
            <Link href="/categories" className="text-sm font-semibold text-brand hover:underline">
              All categories
            </Link>
          </div>
        </div>
      </div>

      <div className="container-store py-8 sm:py-10">
        <div className="flex flex-col gap-6 lg:flex-row">
          <aside className="w-full shrink-0 space-y-4 lg:max-w-xs">
            <div className="rounded-[var(--radius-card)] border border-border bg-page-elevated p-4 shadow-[var(--shadow-card)]">
              <p className="text-sm font-semibold text-foreground">Refine</p>
              <div className="mt-4 space-y-3 text-sm text-muted">
                <label className="flex items-center gap-2">
                  <input type="checkbox" className="rounded border-border text-brand" readOnly /> Verified sellers only
                </label>
                <label className="flex items-center gap-2">
                  <input type="checkbox" className="rounded border-border text-brand" readOnly /> Warehouse verified
                </label>
                <label className="flex items-center gap-2">
                  <input type="checkbox" className="rounded border-border text-brand" readOnly /> Next-day delivery
                </label>
              </div>
            </div>
            <div className="rounded-[var(--radius-card)] border border-border bg-gradient-to-br from-success/10 via-page-elevated to-brand/10 p-4">
              <p className="text-sm font-semibold text-foreground">Trust on every aisle</p>
              <p className="mt-2 text-xs leading-relaxed text-muted">
                Badges reflect seller verification and fulfilment signals. Wire your catalogue rules to drive these
                states automatically.
              </p>
            </div>
          </aside>
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border pb-4">
              <p className="text-sm text-muted">
                Showing <span className="font-semibold text-foreground">{products.length}</span> curated
                items{q ? " (filtered)" : " (demo)"}
              </p>
              <label className="flex items-center gap-2 text-sm text-muted">
                Sort
                <select className="rounded-[12px] border border-border bg-page-elevated px-3 py-2 text-sm font-medium text-foreground">
                  <option>Featured</option>
                  <option>Price: Low to High</option>
                  <option>Price: High to Low</option>
                  <option>Customer rating</option>
                </select>
              </label>
            </div>
            <div className="mt-6">
              <ProductGrid products={products} />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
