"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { type FormEvent, useCallback } from "react";

import { ThemeToggle } from "@/components/store/theme-toggle";
import { env } from "@/lib/env";

const DEPARTMENTS = [
  { value: "all", label: "All" },
  { value: "electronics", label: "Electronics" },
  { value: "phones", label: "Phones" },
  { value: "fashion", label: "Fashion" },
  { value: "home-living", label: "Home & living" },
  { value: "beauty", label: "Beauty" },
  { value: "automotive", label: "Automotive" },
  { value: "deals", label: "Deals" },
] as const;

function navMuted(active: boolean) {
  return active
    ? "font-semibold text-white underline decoration-white/40 underline-offset-4"
    : "font-medium text-white/85 hover:text-white";
}

export function SiteHeader() {
  const pathname = usePathname();
  const router = useRouter();

  const homeActive = pathname === "/";
  const shopActive = pathname.startsWith("/categories");
  const dealsActive = pathname === "/deals";
  const ordersActive = pathname.startsWith("/orders");
  const accountActive = pathname.startsWith("/account");
  const moreActive = ["/help", "/returns", "/privacy"].some((p) => pathname === p || pathname.startsWith(`${p}/`));

  const onSearch = useCallback(
    (e: FormEvent<HTMLFormElement>) => {
      e.preventDefault();
      const fd = new FormData(e.currentTarget);
      const q = String(fd.get("q") ?? "").trim();
      const dept = String(fd.get("department") ?? "all");
      const qs = q ? `?q=${encodeURIComponent(q)}` : "";
      if (!dept || dept === "all") {
        router.push(q ? `/search${qs}` : "/search");
        return;
      }
      router.push(`/categories/${encodeURIComponent(dept)}${qs}`);
    },
    [router],
  );

  return (
    <header className="sticky top-0 z-50 border-b border-brand-hover/90 bg-brand text-white shadow-[0_8px_28px_rgb(30_58_138/0.35)] dark:border-slate-800 dark:bg-slate-950 dark:shadow-[0_12px_40px_rgb(0_0_0/0.45)]">
      <div className="mx-auto w-full max-w-none px-4 sm:px-6 lg:px-8 xl:px-10 2xl:px-12">
        <div className="flex flex-col gap-4 py-4 lg:flex-row lg:items-center lg:gap-8 lg:py-4">
          <div className="flex items-center justify-between gap-3 lg:contents">
            <Link
              href="/"
              className="group flex shrink-0 items-center gap-2.5 outline-none focus-visible:ring-2 focus-visible:ring-white/50 focus-visible:ring-offset-2 focus-visible:ring-offset-brand rounded-none"
            >
              <span className="grid h-10 w-10 place-items-center rounded-none bg-white/15 text-[15px] font-bold tracking-tight text-white ring-1 ring-white/25 transition group-hover:bg-white/20">
                Z
              </span>
              <span className="font-display text-[1.05rem] font-bold tracking-tight sm:text-xl">
                <span className="text-white">Zim</span>
                <span className="text-cta">Market</span>
              </span>
            </Link>

            <div className="flex items-center gap-1.5 lg:hidden">
              <ThemeToggle variant="onBrand" />
              <Link
                href="/account"
                className="relative inline-flex h-11 w-11 items-center justify-center rounded-none border border-white/20 text-white transition hover:bg-white/10"
                aria-label="Wishlist"
              >
                <HeartGlyph className="h-[19px] w-[19px]" />
                <span className="absolute -right-0.5 -top-0.5 grid min-h-[1.125rem] min-w-[1.125rem] place-items-center rounded-none bg-slate-900 px-1 text-[10px] font-bold leading-none text-white ring-2 ring-brand dark:ring-slate-950">
                  0
                </span>
              </Link>
              <Link
                href="/cart"
                className="relative inline-flex h-11 w-11 items-center justify-center rounded-none border border-white/20 text-white transition hover:bg-white/10"
                aria-label="Shopping cart"
              >
                <CartGlyph className="h-[19px] w-[19px]" />
                <span className="absolute -right-0.5 -top-0.5 grid min-h-[1.125rem] min-w-[1.125rem] place-items-center rounded-none bg-slate-900 px-1 text-[10px] font-bold leading-none text-white ring-2 ring-brand dark:ring-slate-950">
                  0
                </span>
              </Link>
              <Link
                href="/account"
                className={`flex items-center gap-2 rounded-none border border-white/15 px-2 py-1.5 transition hover:bg-white/10 ${accountActive ? "bg-white/10" : ""}`}
              >
                <UserGlyph className="h-7 w-7 shrink-0 text-white/95" />
                <span className="flex flex-col text-left leading-tight">
                  <span className="text-[12px] font-semibold leading-none">Log in</span>
                  <span className="mt-0.5 text-[10px] text-white/75">Register</span>
                </span>
              </Link>
            </div>
          </div>

          <form
            onSubmit={onSearch}
            className="order-last flex w-full min-w-0 flex-1 lg:order-none"
            role="search"
          >
            <label className="sr-only" htmlFor="store-search">
              Search ZimMarket
            </label>
            <div className="flex w-full min-w-0 overflow-hidden rounded-none border border-white/20 bg-white shadow-sm ring-1 ring-black/5 dark:border-slate-600 dark:bg-slate-100">
              <div className="relative shrink-0 border-r border-slate-200 dark:border-slate-300">
                <label htmlFor="nav-department" className="sr-only">
                  Department
                </label>
                <select
                  id="nav-department"
                  name="department"
                  defaultValue="all"
                  className="h-full min-h-[48px] cursor-pointer appearance-none bg-transparent py-3 pl-3 pr-8 text-sm font-semibold text-slate-800 outline-none dark:text-slate-900"
                >
                  {DEPARTMENTS.map((d) => (
                    <option key={d.value} value={d.value}>
                      {d.label}
                    </option>
                  ))}
                </select>
                <ChevronDownGlyph className="pointer-events-none absolute right-2 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-500" />
              </div>
              <input
                id="store-search"
                name="q"
                type="search"
                placeholder="I'm shopping for…"
                className="min-w-0 flex-1 border-0 bg-transparent px-3 py-3 text-sm text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-0 dark:text-slate-900"
                autoComplete="off"
              />
              <button
                type="submit"
                className="shrink-0 bg-slate-900 px-5 text-sm font-semibold text-white transition hover:bg-slate-800 dark:bg-slate-900"
              >
                Search
              </button>
            </div>
          </form>

          <div className="hidden items-center gap-1 lg:flex lg:shrink-0">
            <ThemeToggle variant="onBrand" />
            <Link
              href="/account"
              className="relative inline-flex h-11 w-11 items-center justify-center rounded-none border border-white/20 text-white transition hover:bg-white/10"
              aria-label="Wishlist"
            >
              <HeartGlyph className="h-[19px] w-[19px]" />
              <span className="absolute -right-0.5 -top-0.5 grid min-h-[1.125rem] min-w-[1.125rem] place-items-center rounded-none bg-slate-900 px-1 text-[10px] font-bold leading-none text-white ring-2 ring-brand dark:ring-slate-950">
                0
              </span>
            </Link>
            <Link
              href="/cart"
              className="relative inline-flex h-11 w-11 items-center justify-center rounded-none border border-white/20 text-white transition hover:bg-white/10"
              aria-label="Shopping cart"
            >
              <CartGlyph className="h-[19px] w-[19px]" />
              <span className="absolute -right-0.5 -top-0.5 grid min-h-[1.125rem] min-w-[1.125rem] place-items-center rounded-none bg-slate-900 px-1 text-[10px] font-bold leading-none text-white ring-2 ring-brand dark:ring-slate-950">
                0
              </span>
            </Link>
            <Link
              href="/account"
              className={`ml-1 flex items-center gap-2.5 rounded-none border border-white/15 px-2 py-1.5 transition hover:bg-white/10 ${accountActive ? "bg-white/10" : ""}`}
            >
              <UserGlyph className="h-8 w-8 shrink-0 text-white/95" />
              <span className="flex flex-col text-left leading-tight">
                <span className="text-[13px] font-semibold">Log in</span>
                <span className="text-[11px] text-white/75">Register</span>
              </span>
            </Link>
          </div>
        </div>

        <div className="flex flex-col gap-3 border-t border-white/15 py-3 sm:flex-row sm:items-center sm:justify-between sm:gap-4 dark:border-slate-700/80">
          <nav
            className="-mx-1 flex min-w-0 items-center gap-1 overflow-x-auto pb-0.5 text-sm [scrollbar-width:none] sm:gap-2 [&::-webkit-scrollbar]:hidden"
            aria-label="Primary"
          >
            <Link href="/" className={`shrink-0 whitespace-nowrap rounded-none px-2 py-2 sm:px-3 ${navMuted(homeActive)}`}>
              Home
            </Link>
            <Link
              href="/categories"
              className={`inline-flex shrink-0 items-center gap-1 whitespace-nowrap rounded-none px-2 py-2 sm:px-3 ${navMuted(shopActive)}`}
            >
              Shop
              <ChevronDownGlyph className="h-3.5 w-3.5 opacity-70" />
            </Link>
            <Link
              href="/deals"
              className={`inline-flex shrink-0 items-center gap-1 whitespace-nowrap rounded-none px-2 py-2 sm:px-3 ${navMuted(dealsActive)}`}
            >
              Deals
              <ChevronDownGlyph className="h-3.5 w-3.5 opacity-70" />
            </Link>
            <details className="group relative shrink-0">
              <summary className="flex cursor-pointer list-none items-center gap-1 whitespace-nowrap rounded-none px-2 py-2 marker:content-none sm:px-3 [&::-webkit-details-marker]:hidden">
                <span className={navMuted(moreActive)}>More</span>
                <ChevronDownGlyph className="h-3.5 w-3.5 text-white/70 transition group-open:rotate-180" />
              </summary>
              <ul className="absolute left-0 top-full z-50 mt-1 min-w-[12rem] rounded-none border border-slate-200 bg-white py-1.5 text-sm text-slate-800 shadow-lg ring-1 ring-black/5 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100 dark:ring-white/10">
                <li>
                  <Link href="/help" className="block px-4 py-2.5 hover:bg-slate-50 dark:hover:bg-slate-800">
                    Help centre
                  </Link>
                </li>
                <li>
                  <Link href="/returns" className="block px-4 py-2.5 hover:bg-slate-50 dark:hover:bg-slate-800">
                    Returns
                  </Link>
                </li>
                <li>
                  <Link href="/privacy" className="block px-4 py-2.5 hover:bg-slate-50 dark:hover:bg-slate-800">
                    Privacy
                  </Link>
                </li>
              </ul>
            </details>
          </nav>

          <div className="flex flex-wrap items-center gap-x-1 gap-y-2 text-[13px] text-white/90 sm:justify-end">
            <a
              href={`${env.sellerSiteUrl}/register`}
              className="whitespace-nowrap rounded-none px-2 py-1.5 font-medium transition hover:bg-white/10 hover:text-white"
            >
              Sell on ZimMarket
            </a>
            <span className="hidden text-white/35 sm:inline" aria-hidden>
              |
            </span>
            <Link
              href="/orders"
              className={`whitespace-nowrap rounded-none px-2 py-1.5 font-medium transition hover:bg-white/10 hover:text-white ${ordersActive ? "font-semibold text-white" : ""}`}
            >
              Track your order
            </Link>
            <span className="hidden text-white/35 sm:inline" aria-hidden>
              |
            </span>
            <details className="relative">
              <summary className="flex cursor-pointer list-none items-center gap-1 whitespace-nowrap rounded-none px-2 py-1.5 marker:content-none hover:bg-white/10 [&::-webkit-details-marker]:hidden">
                <span className="font-medium">US Dollar</span>
                <ChevronDownGlyph className="h-3.5 w-3.5 text-white/70" />
              </summary>
              <ul className="absolute right-0 top-full z-50 mt-1 min-w-[9rem] rounded-none border border-slate-200 bg-white py-1 text-sm font-medium text-slate-800 shadow-lg dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100">
                <li className="px-4 py-2 text-slate-500 dark:text-slate-400">USD (default)</li>
                <li className="px-4 py-2 text-slate-500 dark:text-slate-400">ZiG (soon)</li>
              </ul>
            </details>
            <span className="hidden text-white/35 sm:inline" aria-hidden>
              |
            </span>
            <details className="relative">
              <summary className="flex cursor-pointer list-none items-center gap-1 whitespace-nowrap rounded-none px-2 py-1.5 marker:content-none hover:bg-white/10 [&::-webkit-details-marker]:hidden">
                <span className="font-medium">English</span>
                <ChevronDownGlyph className="h-3.5 w-3.5 text-white/70" />
              </summary>
              <ul className="absolute right-0 top-full z-50 mt-1 min-w-[9rem] rounded-none border border-slate-200 bg-white py-1 text-sm font-medium text-slate-800 shadow-lg dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100">
                <li className="px-4 py-2 text-slate-500 dark:text-slate-400">English</li>
              </ul>
            </details>
          </div>
        </div>
      </div>
    </header>
  );
}

function ChevronDownGlyph({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden>
      <path d="M6 9l6 6 6-6" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function HeartGlyph({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" aria-hidden>
      <path
        d="M12 21s-6.2-4.35-8.2-8.2C1.5 9.25 3.25 6 6.75 6c1.85 0 3.55.9 4.55 2.35A5.77 5.77 0 0 1 17.25 6C20.75 6 22.5 9.25 20.2 12.8 18.2 16.65 12 21 12 21z"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function CartGlyph({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" aria-hidden>
      <path d="M6 6h15l-2 9H8L6 6z" strokeLinejoin="round" />
      <circle cx="9" cy="20" r="1" />
      <circle cx="18" cy="20" r="1" />
      <path d="M6 6L5 3H2" strokeLinecap="round" />
    </svg>
  );
}

function UserGlyph({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" aria-hidden>
      <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" strokeLinecap="round" />
      <circle cx="12" cy="7" r="4" />
    </svg>
  );
}
