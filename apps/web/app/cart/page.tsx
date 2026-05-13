import Link from "next/link";

export const metadata = {
  title: "Shopping cart",
};

export default function CartPage() {
  return (
    <div className="pb-16">
      <div className="border-b border-border bg-page-elevated">
        <div className="container-store py-8 sm:py-10">
          <h1 className="font-display text-2xl font-semibold tracking-tight text-foreground sm:text-3xl">Shopping cart</h1>
          <p className="mt-2 text-sm text-muted">
            Checkout will connect to the same flows as the mobile shopper app—this page previews the premium empty state.
          </p>
        </div>
      </div>

      <div className="container-store py-10 sm:py-12">
        <div className="grid gap-8 lg:grid-cols-12">
          <div className="rounded-none border border-dashed border-border bg-page-elevated p-10 text-center shadow-[var(--shadow-card)] lg:col-span-8">
            <p className="font-display text-lg font-semibold text-foreground">Your cart is empty</p>
            <p className="mt-2 text-sm text-muted">
              Save items with one tap, see delivery estimates inline, and checkout with encrypted payments.
            </p>
            <div className="mt-6 flex flex-wrap justify-center gap-3">
              <Link
                href="/deals"
                className="inline-flex items-center justify-center rounded-none bg-cta px-5 py-2.5 text-sm font-semibold text-white shadow-md shadow-cta/20 transition hover:bg-cta-hover"
              >
                Shop deals
              </Link>
              <Link
                href="/categories"
                className="inline-flex items-center justify-center rounded-none border border-border bg-page px-5 py-2.5 text-sm font-semibold text-foreground transition hover:border-brand/30"
              >
                Browse categories
              </Link>
            </div>
          </div>
          <aside className="space-y-4 lg:col-span-4">
            <div className="rounded-none border border-border bg-page-elevated p-5 shadow-[var(--shadow-card)]">
              <p className="text-sm font-semibold text-foreground">Order summary</p>
              <dl className="mt-4 space-y-2 text-sm">
                <div className="flex justify-between text-muted">
                  <dt>Subtotal</dt>
                  <dd className="font-medium text-foreground">$0.00</dd>
                </div>
                <div className="flex justify-between text-muted">
                  <dt>Delivery</dt>
                  <dd className="font-medium text-foreground">Calculated at checkout</dd>
                </div>
                <div className="flex justify-between border-t border-border pt-3 text-foreground">
                  <dt className="font-semibold">Estimated total</dt>
                  <dd className="font-semibold">$0.00</dd>
                </div>
              </dl>
              <button
                type="button"
                disabled
                className="mt-5 w-full rounded-none bg-brand py-3 text-sm font-semibold text-white opacity-60"
              >
                Proceed to checkout
              </button>
              <p className="mt-3 text-center text-xs text-muted">Secure payment · Buyer protection on eligible orders</p>
            </div>
            <div className="rounded-none border border-success/25 bg-success/10 p-5 text-sm text-muted">
              <p className="font-semibold text-success">Delivery clarity</p>
              <p className="mt-2">Slot-level ETAs appear here once items are added—mirrors production checkout UX.</p>
            </div>
          </aside>
        </div>
      </div>
    </div>
  );
}
