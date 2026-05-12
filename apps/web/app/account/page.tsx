import Link from "next/link";

export const metadata = {
  title: "Your account",
};

export default function AccountPage() {
  return (
    <div className="pb-16">
      <div className="border-b border-border bg-page-elevated">
        <div className="container-store py-8 sm:py-10">
          <h1 className="font-display text-2xl font-semibold tracking-tight text-foreground sm:text-3xl">Your account</h1>
          <p className="mt-2 text-sm text-muted">
            Customer sign-in and profile will live here (web auth BFF, same API as mobile). Below is a production-grade
            layout shell.
          </p>
        </div>
      </div>
      <div className="container-store py-10 sm:py-12">
        <div className="grid gap-6 lg:grid-cols-3">
          <aside className="space-y-3">
            {["Profile", "Addresses", "Payments", "Notifications"].map((item) => (
              <button
                key={item}
                type="button"
                className="flex w-full items-center justify-between rounded-[14px] border border-border bg-page-elevated px-4 py-3 text-left text-sm font-semibold text-foreground shadow-sm transition hover:border-brand/30"
              >
                {item}
                <span className="text-muted">›</span>
              </button>
            ))}
          </aside>
          <div className="rounded-[var(--radius-lg)] border border-border bg-page-elevated p-6 shadow-[var(--shadow-card)] lg:col-span-2">
            <p className="text-sm font-semibold text-foreground">Account overview</p>
            <p className="mt-2 text-sm text-muted">
              Wire authentication to unlock personalised recommendations, saved carts, and order history on web.
            </p>
            <div className="mt-6 flex flex-wrap gap-3">
              <Link href="/orders" className="rounded-[14px] bg-brand px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-hover">
                View orders
              </Link>
              <Link href="/" className="rounded-[14px] border border-border px-4 py-2.5 text-sm font-semibold text-foreground hover:border-brand/30">
                Continue shopping
              </Link>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
