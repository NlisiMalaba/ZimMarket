import Link from "next/link";

import { CategoryCard } from "@/components/store/category-card";
import { STOREFRONT_CATEGORIES } from "@/lib/storefront-data";

export const metadata = {
  title: "Categories",
};

export default function CategoriesPage() {
  return (
    <div className="pb-16">
      <div className="border-b border-border bg-page-elevated">
        <div className="container-store py-10 sm:py-12">
          <p className="text-xs font-semibold uppercase tracking-[0.2em] text-brand">Browse</p>
          <h1 className="mt-2 font-display text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">
            All categories
          </h1>
          <p className="mt-3 max-w-2xl text-sm text-muted sm:text-base">
            Premium cards with iconography, subtle motion, and clear hierarchy—optimised for fast scanning on mobile
            and desktop.
          </p>
        </div>
      </div>

      <div className="container-store py-10 sm:py-12">
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {STOREFRONT_CATEGORIES.map((c) => (
            <CategoryCard key={c.slug} category={c} />
          ))}
        </div>

        <div className="mt-12 rounded-none border border-border bg-page-elevated p-6 shadow-[var(--shadow-card)] sm:p-8">
          <h2 className="font-display text-lg font-semibold text-foreground">Need something specific?</h2>
          <p className="mt-2 text-sm text-muted">
            Use search for SKUs and brands, or open deals for promoted offers. Category taxonomy will map to your
            catalogue API.
          </p>
          <div className="mt-5 flex flex-wrap gap-3">
            <Link
              href="/search"
              className="inline-flex items-center justify-center rounded-none bg-brand px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-brand-hover"
            >
              Open search
            </Link>
            <Link
              href="/deals"
              className="inline-flex items-center justify-center rounded-none border border-border bg-page px-5 py-2.5 text-sm font-semibold text-foreground transition hover:border-brand/30"
            >
              View deals
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
