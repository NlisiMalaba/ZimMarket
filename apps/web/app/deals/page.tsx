import Link from "next/link";

import { ProductGrid } from "@/components/store/product-grid";
import { getProductsByBadge, STOREFRONT_PRODUCTS } from "@/lib/storefront-data";

export const metadata = {
  title: "Deals",
};

export default function DealsPage() {
  const deals = getProductsByBadge("deal");
  const boosted = STOREFRONT_PRODUCTS.filter((p) => p.compareAtUsd);

  return (
    <div className="pb-16">
      <div className="border-b border-border bg-gradient-to-r from-cta/15 via-page-elevated to-brand/10">
        <div className="container-store py-10 sm:py-12">
          <p className="text-xs font-semibold uppercase tracking-[0.2em] text-cta">Promotions</p>
          <h1 className="mt-2 font-display text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">
            Today&apos;s best deals
          </h1>
          <p className="mt-3 max-w-2xl text-sm text-muted sm:text-base">
            Warm CTA accents highlight savings without cheapening the UI. Connect your promotions engine to populate this
            hub.
          </p>
          <div className="mt-6 flex flex-wrap gap-3">
            <Link
              href="/search?tag=deal"
              className="inline-flex items-center justify-center rounded-none bg-cta px-5 py-2.5 text-sm font-semibold text-white shadow-md shadow-cta/25 transition hover:bg-cta-hover"
            >
              Shop all deals
            </Link>
            <Link
              href="/categories"
              className="inline-flex items-center justify-center rounded-none border border-border bg-page-elevated px-5 py-2.5 text-sm font-semibold text-foreground transition hover:border-brand/30"
            >
              Browse categories
            </Link>
          </div>
        </div>
      </div>

      <div className="container-store py-10 sm:py-12">
        <div className="grid gap-6 lg:grid-cols-3">
          <div className="rounded-none border border-border bg-page-elevated p-6 shadow-[var(--shadow-card)] lg:col-span-2">
            <h2 className="font-display text-lg font-semibold text-foreground">Spotlight offers</h2>
            <p className="mt-2 text-sm text-muted">Hero-style promo surface for campaigns and seasonal pushes.</p>
            <div className="mt-6">
              <ProductGrid products={[...deals, ...boosted].filter((p, i, arr) => arr.findIndex((x) => x.id === p.id) === i)} />
            </div>
          </div>
          <aside className="space-y-4">
            <div className="rounded-none border border-border bg-page-elevated p-5 shadow-[var(--shadow-card)]">
              <p className="text-sm font-semibold text-foreground">Secure checkout</p>
              <p className="mt-2 text-sm text-muted">PCI-minded flows with buyer protection messaging at every step.</p>
            </div>
            <div className="rounded-none border border-success/25 bg-success/10 p-5">
              <p className="text-sm font-semibold text-success">Delivery guarantees</p>
              <p className="mt-2 text-sm text-muted">
                Prominent delivery badges reduce anxiety on high-velocity promotional traffic.
              </p>
            </div>
          </aside>
        </div>
      </div>
    </div>
  );
}
