import Link from "next/link";

import { DealsOfTheDay } from "@/components/store/deals-of-the-day";
import { BuyerProtectionBanner } from "@/components/store/trust-strip";
import { CategoriesBento } from "@/components/store/categories-bento";
import { HeroCarousel } from "@/components/store/hero-carousel";
import { HorizontalRail, RailItem } from "@/components/store/horizontal-rail";
import { ProductCard } from "@/components/store/product-card";
import { ProductGrid } from "@/components/store/product-grid";
import { TopSellersRow } from "@/components/store/top-sellers-row";
import {
  getProductsByBadge,
  HERO_SLIDES,
  HOME_BENTO_CATEGORIES,
  STOREFRONT_PRODUCTS,
} from "@/lib/storefront-data";

export default function HomePage() {
  const trending = getProductsByBadge("trending");
  const deals = getProductsByBadge("deal");
  const fresh = getProductsByBadge("new");
  const recommended = STOREFRONT_PRODUCTS.slice(0, 4);
  const dealOfDayProducts = (() => {
    const ids = new Set(deals.map((d) => d.id));
    const extras = STOREFRONT_PRODUCTS.filter((p) => p.compareAtUsd && !ids.has(p.id));
    return [...deals, ...extras].slice(0, 8);
  })();

  return (
    <div className="pb-16">
      <HeroCarousel slides={HERO_SLIDES} />
      <DealsOfTheDay products={dealOfDayProducts} />

      <CategoriesBento categories={HOME_BENTO_CATEGORIES} />

      <HorizontalRail
        id="trending"
        title="What people keep opening"
        subtitle="Not “algorithm magic”—just items that sell steadily and don’t attract drama in reviews."
        action={
          <Link href="/search?sort=trending" className="text-sm font-medium text-brand underline-offset-4 hover:underline">
            Show more
          </Link>
        }
      >
        {trending.map((p) => (
          <RailItem key={p.id}>
            <ProductCard product={p} />
          </RailItem>
        ))}
        {(() => {
          const ids = new Set(trending.map((t) => t.id));
          return STOREFRONT_PRODUCTS.filter((p) => !ids.has(p.id))
            .slice(0, 2)
            .map((p) => (
              <RailItem key={p.id}>
                <ProductCard product={p} />
              </RailItem>
            ));
        })()}
      </HorizontalRail>

      <section className="border-y border-border bg-page-elevated py-12 sm:py-14 dark:border-slate-800/80 dark:bg-slate-950/35">
        <div className="container-store">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
            <div className="max-w-2xl">
              <h2 className="font-display text-2xl font-semibold tracking-tight text-foreground sm:text-3xl">Worth grabbing on sale</h2>
              <p className="mt-2 text-sm leading-relaxed text-muted sm:text-base">
                If the price looks too good, the page should still tell you what you’re getting. These are demo listings,
                but the layout is meant for real compare-at pricing.
              </p>
            </div>
            <Link href="/deals" className="shrink-0 text-sm font-medium text-cta underline-offset-4 hover:underline">
              All deals
            </Link>
          </div>
          <div className="mt-8">
            <ProductGrid
              products={(() => {
                const ids = new Set(deals.map((d) => d.id));
                const extras = STOREFRONT_PRODUCTS.filter((p) => p.compareAtUsd && !ids.has(p.id));
                return [...deals, ...extras].slice(0, 8);
              })()}
              priorityCount={6}
            />
          </div>
        </div>
      </section>

      <HorizontalRail
        title="New in the catalogue"
        subtitle="Fresh doesn’t always mean “best”—but it does mean fewer stale photos and sellers who still remember listing it."
        action={
          <Link href="/search?tag=new" className="text-sm font-medium text-brand underline-offset-4 hover:underline">
            New only
          </Link>
        }
      >
        {fresh.map((p) => (
          <RailItem key={p.id}>
            <ProductCard product={p} />
          </RailItem>
        ))}
        {STOREFRONT_PRODUCTS.slice(3, 6).map((p) => (
          <RailItem key={p.id}>
            <ProductCard product={p} />
          </RailItem>
        ))}
      </HorizontalRail>

      <section className="container-store py-12 sm:py-14">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
          <div className="max-w-2xl">
            <h2 className="font-display text-2xl font-semibold tracking-tight text-foreground sm:text-3xl">A few picks</h2>
            <p className="mt-2 text-sm leading-relaxed text-muted sm:text-base">
              Pretend this is personalised later. For now it’s just a tight grid that respects photos and doesn’t yell at
              you with badges.
            </p>
          </div>
          <Link href="/search" className="shrink-0 text-sm font-medium text-brand underline-offset-4 hover:underline">
            Search instead
          </Link>
        </div>
        <div className="mt-8">
          <ProductGrid products={recommended} />
        </div>
      </section>

      <section className="border-t border-border bg-page-elevated py-12 sm:py-14 dark:border-slate-800/80 dark:bg-slate-950/35">
        <div className="container-store">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
            <div className="max-w-2xl">
              <h2 className="font-display text-2xl font-semibold tracking-tight text-foreground sm:text-3xl">Shops people come back to</h2>
              <p className="mt-2 text-sm leading-relaxed text-muted sm:text-base">
                Ratings aren’t everything, but repeat buyers usually mean fewer surprises. These are placeholder seller
                cards until your seller graph is wired in.
              </p>
            </div>
            <Link href="/search?filter=top-sellers" className="shrink-0 text-sm font-medium text-brand underline-offset-4 hover:underline">
              Find a seller
            </Link>
          </div>
          <div className="mt-8">
            <TopSellersRow />
          </div>
        </div>
      </section>

      <BuyerProtectionBanner />

      <section className="container-store pb-6">
        <div className="rounded-none border border-border bg-page-elevated p-6 sm:p-8 dark:border-slate-800/80 dark:bg-slate-900/40">
          <div className="flex flex-col gap-6 lg:flex-row lg:items-start lg:justify-between">
            <div className="max-w-2xl">
              <p className="text-sm font-medium text-muted">Delivery</p>
              <h2 className="mt-2 font-display text-xl font-semibold tracking-tight text-foreground sm:text-2xl">
                Routes change. Communication shouldn’t vanish when they do.
              </h2>
              <p className="mt-3 text-sm leading-relaxed text-muted">
                You should see where an order is, and what happens if a driver is rerouted. No cheerful “on the way”
                forever—just specifics, even when they’re boring.
              </p>
            </div>
            <div className="flex shrink-0 flex-col gap-3 sm:flex-row lg:flex-col">
              <Link
                href="/orders"
                className="inline-flex items-center justify-center rounded-none bg-brand px-6 py-3 text-sm font-semibold text-white shadow-md shadow-brand/20 transition hover:bg-brand-hover"
              >
                Track an order
              </Link>
              <Link
                href="/help"
                className="inline-flex items-center justify-center rounded-none border border-border bg-page px-6 py-3 text-sm font-semibold text-foreground transition hover:border-brand/30 dark:border-slate-700 dark:bg-slate-950/60"
              >
                Delivery questions
              </Link>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
