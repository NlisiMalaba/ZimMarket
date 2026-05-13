import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";

import { ProductGrid } from "@/components/store/product-grid";
import { getProductBySlug, STOREFRONT_PRODUCTS } from "@/lib/storefront-data";

type PageProps = {
  params: Promise<{ slug: string }>;
};

export function generateStaticParams() {
  return STOREFRONT_PRODUCTS.map((p) => ({ slug: p.slug }));
}

export async function generateMetadata({ params }: PageProps) {
  const { slug } = await params;
  const product = getProductBySlug(decodeURIComponent(slug));
  if (!product) return { title: "Product" };
  return { title: product.name };
}

function formatUsd(n: number) {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD", maximumFractionDigits: 0 }).format(n);
}

export default async function ProductDetailPage({ params }: PageProps) {
  const { slug } = await params;
  const product = getProductBySlug(decodeURIComponent(slug));
  if (!product) notFound();

  const related = STOREFRONT_PRODUCTS.filter((p) => p.id !== product.id).slice(0, 4);
  const pct =
    product.compareAtUsd && product.compareAtUsd > product.priceUsd
      ? Math.round((1 - product.priceUsd / product.compareAtUsd) * 100)
      : null;

  return (
    <div className="pb-16">
      <div className="border-b border-border bg-page-elevated">
        <div className="container-store py-4 text-xs text-muted">
          <Link href="/" className="font-medium text-brand hover:underline">
            Home
          </Link>
          <span className="mx-2 text-border-strong">/</span>
          <Link href="/search" className="font-medium text-brand hover:underline">
            Marketplace
          </Link>
          <span className="mx-2 text-border-strong">/</span>
          <span className="text-foreground">{product.name}</span>
        </div>
      </div>

      <div className="container-store py-10 lg:py-12">
        <div className="grid gap-10 lg:grid-cols-12 lg:gap-12">
          <div className="lg:col-span-7">
            <div className="overflow-hidden rounded-none border border-border bg-page shadow-[var(--shadow-card)]">
              <div className="relative aspect-square">
                <Image src={product.image} alt={product.name} fill className="object-cover" sizes="(max-width:1024px) 100vw, 55vw" priority />
              </div>
              <div className="grid grid-cols-4 gap-2 p-3 sm:p-4">
                {[product.image, product.image, product.image, product.image].map((src, i) => (
                  <button
                    key={`${src}-${i}`}
                    type="button"
                    className={`relative aspect-square overflow-hidden rounded-none border ${i === 0 ? "border-brand ring-2 ring-brand/20" : "border-border"} bg-page`}
                    aria-label={`Image ${i + 1}`}
                  >
                    <Image src={src} alt="" fill className="object-cover" sizes="120px" />
                  </button>
                ))}
              </div>
            </div>

            <div className="mt-8 rounded-none border border-border bg-page-elevated p-6 shadow-[var(--shadow-card)]">
              <h2 className="font-display text-lg font-semibold text-foreground">Delivery & fulfilment</h2>
              <p className="mt-2 text-sm text-muted">
                Estimates reflect your selected area. Final windows are confirmed at checkout for supported routes.
              </p>
              <div className="mt-6 grid gap-4 sm:grid-cols-3">
                <DeliveryStat label="Estimate" value={product.deliveryEstimate} />
                <DeliveryStat label="Warehouse" value={product.warehouseVerified ? "Verified pick" : "Seller ship"} />
                <DeliveryStat label="Tracking" value="Live updates" />
              </div>
              <div className="mt-6 rounded-none border border-dashed border-border bg-page p-4">
                <p className="text-xs font-semibold uppercase tracking-wide text-muted">Tracking preview</p>
                <ol className="mt-4 space-y-3 text-sm">
                  <li className="flex gap-3">
                    <span className="mt-0.5 h-2 w-2 shrink-0 rounded-none bg-success" />
                    <div>
                      <p className="font-semibold text-foreground">Order placed</p>
                      <p className="text-xs text-muted">Payment authorised · Buyer protection active</p>
                    </div>
                  </li>
                  <li className="flex gap-3">
                    <span className="mt-0.5 h-2 w-2 shrink-0 rounded-none bg-brand" />
                    <div>
                      <p className="font-semibold text-foreground">Preparing shipment</p>
                      <p className="text-xs text-muted">Seller confirms inventory and packaging</p>
                    </div>
                  </li>
                  <li className="flex gap-3">
                    <span className="mt-0.5 h-2 w-2 shrink-0 rounded-none bg-border-strong" />
                    <div>
                      <p className="font-semibold text-foreground">Out for delivery</p>
                      <p className="text-xs text-muted">Driver assigned · map tracking enabled in app</p>
                    </div>
                  </li>
                </ol>
              </div>
            </div>
          </div>

          <div className="lg:col-span-5">
            <div className="sticky top-24 space-y-6">
              <div className="rounded-none border border-border bg-page-elevated p-6 shadow-[var(--shadow-card)] sm:p-8">
                <div className="flex flex-wrap items-center gap-2">
                  {pct ? (
                    <span className="rounded-none bg-success/10 px-2.5 py-1 text-xs font-semibold text-success">
                      Save {pct}%
                    </span>
                  ) : null}
                  {product.badge === "new" ? (
                    <span className="rounded-none bg-brand/10 px-2.5 py-1 text-xs font-semibold text-brand">New</span>
                  ) : null}
                </div>
                <h1 className="mt-3 font-display text-2xl font-semibold tracking-tight text-foreground sm:text-3xl">
                  {product.name}
                </h1>
                <div className="mt-4 flex flex-wrap items-baseline gap-3">
                  <p className="text-3xl font-semibold tracking-tight text-foreground">{formatUsd(product.priceUsd)}</p>
                  {product.compareAtUsd ? (
                    <p className="text-lg text-muted line-through">{formatUsd(product.compareAtUsd)}</p>
                  ) : null}
                </div>
                <div className="mt-4 flex flex-wrap items-center gap-3 text-sm text-muted">
                  <span className="inline-flex items-center gap-1 text-cta">★ {product.rating.toFixed(1)}</span>
                  <span>({product.reviewCount.toLocaleString()} reviews)</span>
                </div>

                <div className="mt-5 flex flex-wrap gap-2">
                  {product.verifiedSeller ? (
                    <span className="inline-flex items-center gap-1.5 rounded-none border border-success/25 bg-success/10 px-3 py-1 text-xs font-semibold text-success">
                      Verified seller
                    </span>
                  ) : (
                    <span className="inline-flex items-center gap-1.5 rounded-none border border-border px-3 py-1 text-xs font-semibold text-muted">
                      Marketplace seller
                    </span>
                  )}
                  {product.warehouseVerified ? (
                    <span className="inline-flex items-center gap-1.5 rounded-none border border-brand/20 bg-brand/5 px-3 py-1 text-xs font-semibold text-brand">
                      Warehouse verified
                    </span>
                  ) : null}
                  <span className="inline-flex items-center gap-1.5 rounded-none border border-border px-3 py-1 text-xs font-semibold text-muted">
                    Authenticity checks on report
                  </span>
                </div>

                <p className="mt-6 text-sm leading-relaxed text-muted">
                  Premium marketplace listing with structured attributes, seller verification, and secure checkout rails.
                  Connect your catalogue API to replace this demo description.
                </p>

                <div className="mt-6 rounded-none border border-border bg-page p-4 text-sm">
                  <p className="font-semibold text-foreground">Sold by {product.sellerName}</p>
                  <p className="mt-1 text-muted">High fulfilment score · responsive support on eligible orders</p>
                </div>

                <div className="mt-6 flex flex-col gap-3 sm:flex-row">
                  <Link
                    href="/cart"
                    className="inline-flex flex-1 items-center justify-center rounded-none bg-cta px-6 py-3.5 text-sm font-semibold text-white shadow-md shadow-cta/25 transition hover:bg-cta-hover"
                  >
                    Buy now
                  </Link>
                  <Link
                    href="/cart"
                    className="inline-flex flex-1 items-center justify-center rounded-none border border-border bg-page px-6 py-3.5 text-sm font-semibold text-foreground transition hover:border-brand/40 hover:text-brand"
                  >
                    Add to cart
                  </Link>
                </div>

                <div className="mt-5 flex flex-wrap gap-3 text-xs text-muted">
                  <span className="inline-flex items-center gap-1.5">
                    <LockMini />
                    Secure payment
                  </span>
                  <span className="inline-flex items-center gap-1.5">
                    <ShieldMini />
                    Buyer protection
                  </span>
                  <span className="inline-flex items-center gap-1.5">
                    <TruckMini />
                    {product.deliveryEstimate}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <section className="border-t border-border bg-page-elevated py-12">
        <div className="container-store">
          <h2 className="font-display text-xl font-semibold text-foreground sm:text-2xl">Customers also viewed</h2>
          <p className="mt-2 text-sm text-muted">Modern carousel-style grid with the same premium product cards.</p>
          <div className="mt-8">
            <ProductGrid products={related} priorityCount={2} />
          </div>
        </div>
      </section>
    </div>
  );
}

function DeliveryStat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-none border border-border bg-page p-4">
      <p className="text-[11px] font-semibold uppercase tracking-wide text-muted">{label}</p>
      <p className="mt-2 text-sm font-semibold text-foreground">{value}</p>
    </div>
  );
}

function LockMini() {
  return (
    <svg className="h-3.5 w-3.5 text-success" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden>
      <rect x="5" y="11" width="14" height="10" rx="2" />
      <path d="M8 11V8a4 4 0 0 1 8 0v3" strokeLinecap="round" />
    </svg>
  );
}

function ShieldMini() {
  return (
    <svg className="h-3.5 w-3.5 text-brand" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden>
      <path d="M12 3l8 3v6c0 5-3.5 9-8 11-4.5-2-8-6-8-11V6l8-3z" strokeLinejoin="round" />
    </svg>
  );
}

function TruckMini() {
  return (
    <svg className="h-3.5 w-3.5 text-brand" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden>
      <path d="M3 7h11v10H3zM14 11h3l3 3v3h-6" strokeLinejoin="round" />
      <circle cx="7.5" cy="18.5" r="1.5" />
      <circle cx="17.5" cy="18.5" r="1.5" />
    </svg>
  );
}
