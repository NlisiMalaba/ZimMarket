import Link from "next/link";

import { ProductGrid } from "@/components/store/product-grid";
import { STOREFRONT_PRODUCTS } from "@/lib/storefront-data";

export const metadata = {
  title: "Search",
};

type PageProps = {
  searchParams: Promise<{ q?: string; sort?: string; tag?: string }>;
};

export default async function SearchPage({ searchParams }: PageProps) {
  const sp = await searchParams;
  const q = (sp.q ?? "").trim();
  const tag = (sp.tag ?? "").trim();

  const filtered = STOREFRONT_PRODUCTS.filter((p) => {
    const hay = `${p.name} ${p.sellerName}`.toLowerCase();
    const matchesQ = !q || hay.includes(q.toLowerCase());
    const matchesTag =
      !tag ||
      (tag === "new" && p.badge === "new") ||
      (tag === "deal" && p.badge === "deal") ||
      (tag === "trending" && p.badge === "trending");
    return matchesQ && matchesTag;
  });

  return (
    <div className="pb-16">
      <div className="border-b border-border bg-page-elevated">
        <div className="container-store py-8 sm:py-10">
          <h1 className="font-display text-2xl font-semibold tracking-tight text-foreground sm:text-3xl">Search results</h1>
          <p className="mt-2 text-sm text-muted">
            {q ? (
              <>
                Query: <span className="font-semibold text-foreground">&ldquo;{q}&rdquo;</span>
              </>
            ) : (
              "Browse the full demo catalogue or enter a query in the header."
            )}
            {tag ? (
              <>
                {" "}
                · Tag: <span className="font-semibold text-foreground">{tag}</span>
              </>
            ) : null}
          </p>
        </div>
      </div>

      <div className="container-store py-8 sm:py-10">
        <div className="flex flex-col gap-8 lg:flex-row">
          <aside className="w-full shrink-0 space-y-4 lg:max-w-xs">
            <form className="rounded-[var(--radius-card)] border border-border bg-page-elevated p-4 shadow-[var(--shadow-card)]" action="/search" method="get">
              <p className="text-sm font-semibold text-foreground">Filters</p>
              <label className="mt-4 block text-xs font-semibold uppercase tracking-wide text-muted" htmlFor="q-side">
                Keyword
              </label>
              <input
                id="q-side"
                name="q"
                defaultValue={q}
                className="mt-2 w-full rounded-[12px] border border-border bg-page px-3 py-2 text-sm text-foreground"
              />
              <input type="hidden" name="tag" value={tag} />
              <button
                type="submit"
                className="mt-3 w-full rounded-[12px] bg-brand py-2 text-sm font-semibold text-white transition hover:bg-brand-hover"
              >
                Apply
              </button>
            </form>
            <div className="rounded-[var(--radius-card)] border border-border bg-page-elevated p-4 text-sm text-muted shadow-[var(--shadow-card)]">
              <p className="font-semibold text-foreground">Trust filters</p>
              <p className="mt-2 leading-relaxed">
                Verified sellers, warehouse checks, and delivery badges will map to your search index.
              </p>
            </div>
            <Link href="/deals" className="block rounded-[var(--radius-card)] border border-cta/30 bg-cta/10 p-4 text-sm font-semibold text-foreground transition hover:border-cta/50">
              Jump to deals →
            </Link>
          </aside>
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border pb-4">
              <p className="text-sm text-muted">
                <span className="font-semibold text-foreground">{filtered.length}</span> results
              </p>
              <label className="flex items-center gap-2 text-sm text-muted">
                Sort
                <select
                  name="sort"
                  className="rounded-[12px] border border-border bg-page-elevated px-3 py-2 text-sm font-medium text-foreground"
                  defaultValue={sp.sort ?? "featured"}
                >
                  <option value="featured">Featured</option>
                  <option value="price-asc">Price: Low to High</option>
                  <option value="price-desc">Price: High to Low</option>
                  <option value="rating">Top rated</option>
                </select>
              </label>
            </div>
            <div className="mt-6">
              {filtered.length ? (
                <ProductGrid products={filtered} />
              ) : (
                <div className="rounded-[var(--radius-lg)] border border-dashed border-border bg-page-elevated p-10 text-center">
                  <p className="font-display text-lg font-semibold text-foreground">No matches yet</p>
                  <p className="mt-2 text-sm text-muted">Try a broader keyword or clear filters—API wiring will enrich results.</p>
                  <Link href="/categories" className="mt-6 inline-flex rounded-[14px] bg-brand px-5 py-2.5 text-sm font-semibold text-white hover:bg-brand-hover">
                    Browse categories
                  </Link>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
