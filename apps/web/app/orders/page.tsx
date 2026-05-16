import Link from "next/link";

export const metadata = {
  title: "Your orders",
};

export default function OrdersPage() {
  return (
    <div className="pb-16">
      <div className="border-b border-border bg-page-elevated">
        <div className="container-store py-8 sm:py-10">
          <h1 className="font-display text-2xl font-semibold tracking-tight text-foreground sm:text-3xl">Your orders</h1>
          <p className="mt-2 text-sm text-muted">
            Order history will load from the customer orders API. This page demonstrates tracking-first hierarchy.
          </p>
        </div>
      </div>
      <div className="container-store py-10 sm:py-12">
        <div className="rounded-none border border-dashed border-border bg-page-elevated p-10 text-center shadow-[var(--shadow-card)]">
          <p className="font-display text-lg font-semibold text-foreground">No orders to display</p>
          <p className="mt-2 text-sm text-muted">When live, each row opens a timeline with map tracking and secure payment receipts.</p>
          <Link href="/products/wireless-noise-headphones" className="mt-6 inline-flex rounded-none bg-brand px-5 py-2.5 text-sm font-semibold text-white hover:bg-brand-hover">
            Explore a sample product
          </Link>
        </div>
      </div>
    </div>
  );
}
