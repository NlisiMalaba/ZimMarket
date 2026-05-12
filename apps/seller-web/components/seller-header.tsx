import Link from "next/link";

import { env } from "@/lib/env";

export function SellerHeader() {
  return (
    <header className="border-b border-slate-200 bg-white">
      <div className="mx-auto flex max-w-5xl flex-wrap items-center justify-between gap-3 px-4 py-4 sm:px-6">
        <Link href="/" className="text-lg font-semibold tracking-tight text-slate-900">
          ZimMarket <span className="text-slate-500">Sellers</span>
        </Link>
        <nav className="flex flex-wrap items-center gap-4 text-sm font-medium">
          <a href={env.customerSiteUrl} className="text-slate-600 hover:text-slate-900">
            Shop as customer
          </a>
          <Link href="/login" className="text-slate-600 hover:text-slate-900">
            Sign in
          </Link>
          <Link
            href="/register"
            className="rounded-md bg-slate-900 px-3 py-1.5 text-white hover:bg-slate-800"
          >
            Start selling
          </Link>
        </nav>
      </div>
    </header>
  );
}
