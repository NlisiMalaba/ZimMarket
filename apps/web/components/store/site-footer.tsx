import Link from "next/link";

import { env } from "@/lib/env";

export function SiteFooter() {
  return (
    <footer className="mt-auto border-t border-border bg-page-elevated text-sm text-muted">
      <div className="container-store py-12">
        <div className="grid gap-10 lg:grid-cols-4">
          <div>
            <p className="font-display text-lg font-bold text-brand">ZimMarket</p>
            <p className="mt-3 max-w-sm leading-relaxed">
              A trusted marketplace for authentic listings, secure checkout, and delivery you can track—built for scale
              across Zimbabwe.
            </p>
            <div className="mt-4 flex flex-wrap gap-2">
              <span className="rounded-none border border-success/25 bg-success/10 px-3 py-1 text-xs font-semibold text-success">
                Secure payments
              </span>
              <span className="rounded-none border border-brand/20 bg-brand/5 px-3 py-1 text-xs font-semibold text-brand">
                Verified sellers
              </span>
            </div>
          </div>
          <div>
            <p className="font-semibold text-foreground">Shop</p>
            <ul className="mt-3 space-y-2">
              <li>
                <Link href="/categories" className="transition hover:text-brand hover:underline">
                  All categories
                </Link>
              </li>
              <li>
                <Link href="/deals" className="transition hover:text-brand hover:underline">
                  Deals
                </Link>
              </li>
              <li>
                <Link href="/search" className="transition hover:text-brand hover:underline">
                  Search
                </Link>
              </li>
              <li>
                <Link href="/orders" className="transition hover:text-brand hover:underline">
                  Track orders
                </Link>
              </li>
            </ul>
          </div>
          <div>
            <p className="font-semibold text-foreground">Sell & deliver</p>
            <ul className="mt-3 space-y-2">
              <li>
                <a href={`${env.sellerSiteUrl}/register`} className="transition hover:text-brand hover:underline">
                  Sell on ZimMarket
                </a>
              </li>
              <li>
                <a href={`${env.sellerSiteUrl}`} className="transition hover:text-brand hover:underline">
                  Seller portal
                </a>
              </li>
              <li>
                <a href={`${env.driverSiteUrl}/register`} className="transition hover:text-brand hover:underline">
                  Drive for ZimMarket
                </a>
              </li>
              <li>
                <a href={`${env.driverSiteUrl}`} className="transition hover:text-brand hover:underline">
                  Driver portal
                </a>
              </li>
            </ul>
          </div>
          <div>
            <p className="font-semibold text-foreground">Support</p>
            <ul className="mt-3 space-y-2">
              <li>
                <Link href="/help" className="transition hover:text-brand hover:underline">
                  Help centre
                </Link>
              </li>
              <li>
                <Link href="/returns" className="transition hover:text-brand hover:underline">
                  Returns
                </Link>
              </li>
              <li>
                <Link href="/privacy" className="transition hover:text-brand hover:underline">
                  Privacy
                </Link>
              </li>
            </ul>
          </div>
        </div>
        <p className="mt-10 border-t border-border pt-6 text-xs text-muted">
          © {new Date().getFullYear()} ZimMarket. All rights reserved.
        </p>
      </div>
    </footer>
  );
}
