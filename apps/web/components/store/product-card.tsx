import Image from "next/image";
import Link from "next/link";

import type { StorefrontProduct } from "@/lib/storefront-data";

function formatUsd(n: number) {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD", maximumFractionDigits: 0 }).format(n);
}

function formatReviewCount(n: number) {
  return new Intl.NumberFormat("en-US").format(n);
}

export function ProductCard({ product, priority }: { product: StorefrontProduct; priority?: boolean }) {
  const href = `/products/${encodeURIComponent(product.slug)}`;
  const showVerified = product.verifiedSeller || product.warehouseVerified;

  return (
    <article className="group flex h-full flex-col overflow-hidden rounded-[16px] border border-border/80 bg-page-elevated shadow-sm transition duration-300 hover:shadow-md dark:border-slate-700/80">
      <div className="relative aspect-square overflow-hidden rounded-t-[16px] bg-[#f3f4f6] dark:bg-slate-800/60">
        <Link href={href} className="absolute inset-0 block">
          <Image
            src={product.image}
            alt={product.name}
            fill
            sizes="(max-width:640px) 50vw, (max-width:1024px) 33vw, 25vw"
            className="object-contain p-5 transition duration-500 group-hover:scale-[1.03] sm:p-6"
            priority={priority}
          />
        </Link>

        {showVerified ? (
          <span className="absolute left-3 top-3 z-10 inline-flex items-center gap-1 rounded-[8px] bg-emerald-500/15 px-2 py-1 text-[10px] font-semibold uppercase tracking-wide text-emerald-700 backdrop-blur-sm dark:bg-emerald-500/25 dark:text-emerald-300">
            <VerifiedIcon className="h-3 w-3" />
            Verified
          </span>
        ) : null}

        <Link
          href="/account"
          aria-label="Save to wishlist"
          className="absolute right-3 top-3 z-10 inline-flex h-9 w-9 items-center justify-center rounded-full border border-border/60 bg-page-elevated/95 text-muted shadow-sm transition hover:border-border-strong hover:text-foreground dark:border-slate-600 dark:bg-slate-900/90"
        >
          <HeartIcon className="h-4 w-4" />
        </Link>
      </div>

      <div className="flex flex-1 flex-col p-4 sm:p-5">
        <div className="flex items-center gap-1.5 text-sm">
          <StarIcon className="h-4 w-4 shrink-0 text-cta" />
          <span className="font-medium text-foreground">{product.rating.toFixed(1)}</span>
          <span className="text-muted">({formatReviewCount(product.reviewCount)})</span>
        </div>

        <Link href={href} className="mt-2 block">
          <h3 className="line-clamp-2 text-base font-semibold leading-snug text-foreground transition group-hover:text-brand">
            {product.name}
          </h3>
        </Link>

        <p className="mt-1.5 line-clamp-2 text-sm leading-relaxed text-muted">{product.summary}</p>

        <div className="mt-auto flex items-end justify-between gap-3 pt-4">
          <div className="flex flex-wrap items-baseline gap-x-2 gap-y-0.5">
            <span className="text-xl font-bold tracking-tight text-foreground sm:text-2xl">
              {formatUsd(product.priceUsd)}
            </span>
            {product.compareAtUsd ? (
              <span className="text-sm text-muted line-through">{formatUsd(product.compareAtUsd)}</span>
            ) : null}
          </div>
          <Link
            href="/cart"
            aria-label={`Add ${product.name} to cart`}
            className="inline-flex h-10 w-10 shrink-0 items-center justify-center rounded-[10px] bg-brand text-white shadow-sm transition hover:bg-brand-hover"
          >
            <CartPlusIcon className="h-5 w-5" />
          </Link>
        </div>
      </div>
    </article>
  );
}

function StarIcon({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="currentColor" aria-hidden>
      <path d="M12 2l2.9 6.26L22 9.27l-5 4.87L18.18 22 12 18.27 5.82 22 7 14.14l-5-4.87 7.1-1.01L12 2z" />
    </svg>
  );
}

function VerifiedIcon({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" aria-hidden>
      <path d="M9 12l2 2 4-4" strokeLinecap="round" strokeLinejoin="round" />
      <circle cx="12" cy="12" r="9" />
    </svg>
  );
}

function HeartIcon({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden>
      <path
        d="M12 20.5s-7-4.35-7-9.5a4 4 0 017-2.2A4 4 0 0119 11c0 5.15-7 9.5-7 9.5z"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function CartPlusIcon({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden>
      <path d="M6 6h14l-1.5 9H8L6 6zM6 6L5 3H2" strokeLinecap="round" strokeLinejoin="round" />
      <circle cx="9" cy="20" r="1.25" fill="currentColor" stroke="none" />
      <circle cx="17" cy="20" r="1.25" fill="currentColor" stroke="none" />
      <path d="M12 8v6M9 11h6" strokeLinecap="round" />
    </svg>
  );
}
