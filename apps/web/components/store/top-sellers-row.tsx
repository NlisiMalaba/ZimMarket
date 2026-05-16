import Link from "next/link";

const sellers = [
  {
    name: "Verified Tech ZW",
    rating: 4.9,
    orders: "120k+",
    badge: "Top electronics",
    href: "/search?seller=verified-tech",
  },
  {
    name: "HomeNest ZW",
    rating: 4.75,
    orders: "72k+",
    badge: "Home & living",
    href: "/search?seller=homenest",
  },
  {
    name: "Harare Audio Co.",
    rating: 4.85,
    orders: "54k+",
    badge: "Audio specialist",
    href: "/search?seller=harare-audio",
  },
] as const;

export function TopSellersRow() {
  return (
    <div className="grid gap-4 md:grid-cols-3">
      {sellers.map((s) => (
        <Link
          key={s.name}
          href={s.href}
          className="group flex flex-col rounded-none border border-border bg-page-elevated p-5 shadow-[var(--shadow-card)] transition hover:-translate-y-0.5 hover:border-brand/25 hover:shadow-[var(--shadow-card-hover)]"
        >
          <div className="flex items-start justify-between gap-3">
            <div>
              <p className="text-sm font-semibold text-foreground">{s.name}</p>
              <p className="mt-1 text-xs text-muted">{s.badge}</p>
            </div>
            <span className="inline-flex items-center gap-1 rounded-none bg-success/10 px-2 py-1 text-[11px] font-semibold text-success">
              ★ {s.rating}
            </span>
          </div>
          <p className="mt-4 text-xs text-muted">Lifetime orders</p>
          <p className="text-lg font-semibold tracking-tight text-foreground">{s.orders}</p>
          <span className="mt-4 text-xs font-semibold text-brand group-hover:underline">View storefront</span>
        </Link>
      ))}
    </div>
  );
}
