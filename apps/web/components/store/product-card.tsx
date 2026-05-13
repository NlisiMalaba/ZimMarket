import Image from "next/image";
import Link from "next/link";

import type { StorefrontProduct } from "@/lib/storefront-data";

function formatUsd(n: number) {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD", maximumFractionDigits: 0 }).format(n);
}

function discountPct(price: number, compare?: number) {
  if (!compare || compare <= price) return null;
  return Math.round((1 - price / compare) * 100);
}

function StarRow({ rating }: { rating: number }) {
  const full = Math.floor(rating);
  const partial = rating - full >= 0.5;
  return (
    <div className="flex items-center gap-0.5" aria-label={`${rating} out of 5 stars`}>
      {Array.from({ length: 5 }).map((_, i) => {
        const filled = i < full || (i === full && partial);
        return (
          <span key={i} className={filled ? "text-cta" : "text-border-strong"}>
            ★
          </span>
        );
      })}
    </div>
  );
}

export function ProductCard({ product, priority }: { product: StorefrontProduct; priority?: boolean }) {
  const pct = discountPct(product.priceUsd, product.compareAtUsd);
  const href = `/products/${encodeURIComponent(product.slug)}`;

  return (
    <article className="group relative flex h-full flex-col overflow-hidden rounded-none border border-border bg-page-elevated shadow-[var(--shadow-card)] transition duration-300 hover:-translate-y-0.5 hover:border-border-strong hover:shadow-[var(--shadow-card-hover)]">
      <Link href={href} className="relative block aspect-square overflow-hidden bg-page">
        <Image
          src={product.image}
          alt={product.name}
          fill
          sizes="(max-width:640px) 50vw, (max-width:1024px) 33vw, 25vw"
          className="object-cover transition duration-500 group-hover:scale-[1.03]"
          priority={priority}
        />
        <div className="pointer-events-none absolute inset-0 bg-gradient-to-t from-[#0f172a]/25 to-transparent opacity-0 transition group-hover:opacity-100" />
        {product.badge ? (
          <span className="absolute left-3 top-3 rounded-none bg-cta px-2.5 py-1 text-[11px] font-semibold uppercase tracking-wide text-white shadow-sm">
            {product.badge === "deal" ? "Deal" : product.badge === "new" ? "New" : "Trending"}
          </span>
        ) : null}
        {pct ? (
          <span className="absolute right-3 top-3 rounded-none bg-success px-2.5 py-1 text-[11px] font-semibold text-white shadow-sm">
            -{pct}%
          </span>
        ) : null}
      </Link>

      <div className="flex flex-1 flex-col p-4">
        <Link href={href} className="block">
          <h3 className="line-clamp-2 min-h-[2.75rem] text-sm font-semibold leading-snug text-foreground transition group-hover:text-brand">
            {product.name}
          </h3>
        </Link>

        <div className="mt-3 flex items-baseline gap-2">
          <p className="text-lg font-semibold tracking-tight text-foreground">{formatUsd(product.priceUsd)}</p>
          {product.compareAtUsd ? (
            <p className="text-sm text-muted line-through">{formatUsd(product.compareAtUsd)}</p>
          ) : null}
        </div>

        <div className="mt-2 flex flex-wrap items-center gap-2 text-xs text-muted">
          <StarRow rating={product.rating} />
          <span className="text-foreground/80">
            {product.rating.toFixed(1)} <span className="text-muted">({product.reviewCount.toLocaleString()})</span>
          </span>
        </div>

        <div className="mt-3 flex flex-wrap items-center gap-2">
          {product.verifiedSeller ? (
            <span className="inline-flex items-center gap-1 rounded-none border border-success/25 bg-success/10 px-2 py-0.5 text-[11px] font-medium text-success">
              <ShieldTick className="h-3.5 w-3.5" />
              Verified seller
            </span>
          ) : (
            <span className="inline-flex items-center gap-1 rounded-none border border-border px-2 py-0.5 text-[11px] font-medium text-muted">
              Marketplace seller
            </span>
          )}
          {product.warehouseVerified ? (
            <span className="inline-flex items-center gap-1 rounded-none border border-brand/20 bg-brand/5 px-2 py-0.5 text-[11px] font-medium text-brand">
              Warehouse checked
            </span>
          ) : null}
        </div>

        <p className="mt-3 flex items-start gap-1.5 text-xs text-muted">
          <TruckIcon className="mt-0.5 h-3.5 w-3.5 shrink-0 text-brand" />
          <span>{product.deliveryEstimate}</span>
        </p>

        <div className="mt-4 flex gap-2">
          <Link
            href={href}
            className="inline-flex flex-1 items-center justify-center rounded-none border border-border bg-page px-3 py-2 text-xs font-semibold text-foreground transition hover:border-brand hover:text-brand"
          >
            View
          </Link>
          <Link
            href="/cart"
            className="inline-flex flex-1 items-center justify-center rounded-none bg-cta px-3 py-2 text-xs font-semibold text-white shadow-sm transition hover:bg-cta-hover"
          >
            Buy now
          </Link>
        </div>
      </div>
    </article>
  );
}

function ShieldTick({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden>
      <path d="M12 3l7 3v6c0 4-3 7-7 9-4-2-7-5-7-9V6l7-3z" strokeLinejoin="round" />
      <path d="M9 12l2 2 4-4" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function TruckIcon({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden>
      <path d="M3 7h11v10H3zM14 11h3l3 3v3h-6" strokeLinejoin="round" />
      <circle cx="7.5" cy="18.5" r="1.5" />
      <circle cx="17.5" cy="18.5" r="1.5" />
    </svg>
  );
}
