import Link from "next/link";

import { env } from "@/lib/env";

export function DriverHeader() {
  return (
    <header className="border-b border-emerald-200 bg-white">
      <div className="mx-auto flex max-w-5xl flex-wrap items-center justify-between gap-3 px-4 py-4 sm:px-6">
        <Link href="/" className="text-lg font-semibold tracking-tight text-emerald-950">
          ZimMarket <span className="text-emerald-700">Drivers</span>
        </Link>
        <nav className="flex flex-wrap items-center gap-4 text-sm font-medium">
          <a href={env.customerSiteUrl} className="text-neutral-600 hover:text-neutral-900">
            Shop as customer
          </a>
          <Link href="/login" className="text-neutral-600 hover:text-neutral-900">
            Sign in
          </Link>
          <Link
            href="/register"
            className="rounded-md bg-emerald-700 px-3 py-1.5 text-white hover:bg-emerald-800"
          >
            Apply to drive
          </Link>
        </nav>
      </div>
    </header>
  );
}
