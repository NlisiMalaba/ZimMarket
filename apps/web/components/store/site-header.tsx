"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { type FormEvent, useCallback, useEffect, useId, useRef, useState } from "react";

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

/** Collapse when scrolling down past this; expand only when scrolling back above EXPAND. */
const SCROLL_COLLAPSE_AT = 96;
const SCROLL_EXPAND_AT = 32;

function navLink(active: boolean) {
  return active
    ? "font-semibold text-black"
    : "font-medium text-black/90 hover:text-black";
}

export function SiteHeader() {
  const pathname = usePathname();
  const router = useRouter();
  const [scrolled, setScrolled] = useState(false);

  const homeActive = pathname === "/";
  const shopActive = pathname.startsWith("/categories");
  const dealsActive = pathname === "/deals";
  const ordersActive = pathname.startsWith("/orders");
  const accountActive = pathname.startsWith("/account");
  const moreActive = ["/help", "/returns", "/privacy"].some(
    (p) => pathname === p || pathname.startsWith(`${p}/`),
  );

  const scrolledRef = useRef(false);

  useEffect(() => {
    const onScroll = () => {
      const y = window.scrollY;
      const next = scrolledRef.current ? y > SCROLL_EXPAND_AT : y > SCROLL_COLLAPSE_AT;
      if (next === scrolledRef.current) return;
      scrolledRef.current = next;
      setScrolled(next);
    };
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

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
    <header
      className={`sticky top-0 z-50 bg-header text-black shadow-[0_2px_12px_rgb(0_0_0/0.08)] transition-[box-shadow] duration-300 ${scrolled ? "shadow-[0_4px_16px_rgb(0_0_0/0.12)]" : ""}`}
    >
      <div className="mx-auto w-full max-w-[1400px] px-4 sm:px-6 lg:px-8 xl:px-10">
        {/* Expanded top row: logo, search, actions */}
        <div
          className={`grid transition-opacity duration-300 ease-out ${
            scrolled ? "pointer-events-none grid-rows-[0fr] opacity-0" : "grid-rows-[1fr] opacity-100"
          }`}
          aria-hidden={scrolled}
        >
          <div className="overflow-hidden">
            <div className="flex flex-col gap-4 py-4 sm:flex-row sm:items-center sm:gap-6 lg:gap-8">
              <Link
                href="/"
                className="group shrink-0 font-display text-[1.35rem] font-bold tracking-tight outline-none focus-visible:ring-2 focus-visible:ring-black/30 focus-visible:ring-offset-2 focus-visible:ring-offset-header sm:text-[1.5rem]"
              >
                <span className="text-black">Zim</span>
                <span className="text-white">Market</span>
              </Link>

              <HeaderSearchForm
                onSubmit={onSearch}
                className="hidden min-w-0 flex-1 sm:flex"
                idSuffix="desktop"
              />

              <HeaderActions
                accountActive={accountActive}
                className="hidden sm:flex"
                showTheme
              />
            </div>
          </div>
        </div>

        {/* Bottom row (expanded) or sticky row (scrolled) */}
        <div
          className={`flex flex-col gap-3 border-black/10 ${
            scrolled ? "border-0 py-3.5" : "border-t py-3 sm:flex-row sm:items-center sm:justify-between sm:gap-4"
          }`}
        >
          <div
            className={`flex min-w-0 items-center gap-3 ${
              scrolled ? "w-full flex-col gap-3 sm:flex-row sm:gap-5" : "shrink-0"
            }`}
          >
            <ShopByDepartment className={scrolled ? "" : "sm:mr-2"} />

            {scrolled ? (
              <>
                <HeaderSearchForm
                  onSubmit={onSearch}
                  className="w-full min-w-0 flex-1"
                  idSuffix="sticky"
                />
                <HeaderActions accountActive={accountActive} className="flex shrink-0" showTheme />
              </>
            ) : null}
          </div>

          {!scrolled ? (
            <>
              <nav
                className="-mx-1 flex min-w-0 items-center gap-0.5 overflow-x-auto text-[15px] [scrollbar-width:none] sm:gap-1 [&::-webkit-scrollbar]:hidden"
                aria-label="Primary"
              >
                <Link
                  href="/"
                  className={`inline-flex shrink-0 items-center gap-1 whitespace-nowrap px-2.5 py-2 sm:px-3 ${navLink(homeActive)}`}
                >
                  Home
                </Link>
                <Link
                  href="/categories"
                  className={`inline-flex shrink-0 items-center gap-1 whitespace-nowrap px-2.5 py-2 sm:px-3 ${navLink(shopActive)}`}
                >
                  Shop
                  <ChevronDownGlyph className="h-3.5 w-3.5 opacity-80" />
                </Link>
                <Link
                  href="/deals"
                  className={`inline-flex shrink-0 items-center gap-1 whitespace-nowrap px-2.5 py-2 sm:px-3 ${navLink(dealsActive)}`}
                >
                  Deals
                  <ChevronDownGlyph className="h-3.5 w-3.5 opacity-80" />
                </Link>
                <details className="group relative shrink-0">
                  <summary className="flex cursor-pointer list-none items-center gap-1 whitespace-nowrap px-2.5 py-2 marker:content-none sm:px-3 [&::-webkit-details-marker]:hidden">
                    <span className={navLink(moreActive)}>More</span>
                    <ChevronDownGlyph className="h-3.5 w-3.5 opacity-80 transition group-open:rotate-180" />
                  </summary>
                  <ul className="absolute left-0 top-full z-50 mt-1 min-w-[12rem] rounded-md border border-slate-200 bg-white py-1.5 text-sm text-slate-800 shadow-lg ring-1 ring-black/5">
                    <li>
                      <Link href="/help" className="block px-4 py-2.5 hover:bg-slate-50">
                        Help centre
                      </Link>
                    </li>
                    <li>
                      <Link href="/returns" className="block px-4 py-2.5 hover:bg-slate-50">
                        Returns
                      </Link>
                    </li>
                    <li>
                      <Link href="/privacy" className="block px-4 py-2.5 hover:bg-slate-50">
                        Privacy
                      </Link>
                    </li>
                  </ul>
                </details>
              </nav>

              <UtilityNav ordersActive={ordersActive} className="hidden lg:flex" />
            </>
          ) : null}
        </div>

        {/* Mobile: search + actions when not scrolled */}
        {!scrolled ? (
          <div className="flex flex-col gap-3 border-t border-black/10 pb-4 pt-3 sm:hidden">
            <HeaderSearchForm onSubmit={onSearch} className="w-full" idSuffix="mobile" />
            <div className="flex items-center justify-between gap-2">
              <HeaderActions accountActive={accountActive} className="flex" showTheme />
              <UtilityNav ordersActive={ordersActive} className="flex flex-wrap justify-end text-[12px]" />
            </div>
          </div>
        ) : null}
      </div>
    </header>
  );
}

function ShopByDepartment({ className }: { className?: string }) {
  return (
    <Link
      href="/categories"
      className={`inline-flex shrink-0 items-center gap-2.5 text-[15px] font-bold text-black transition hover:text-black/80 ${className ?? ""}`}
    >
      <MenuGlyph className="h-[18px] w-[18px]" />
      <span className="whitespace-nowrap">Shop By Department</span>
    </Link>
  );
}

function HeaderSearchForm({
  onSubmit,
  className,
  idSuffix,
}: {
  onSubmit: (e: FormEvent<HTMLFormElement>) => void;
  className?: string;
  idSuffix: string;
}) {
  const uid = useId();
  const searchId = `store-search-${idSuffix}-${uid}`;
  const deptId = `nav-department-${idSuffix}-${uid}`;

  return (
    <form onSubmit={onSubmit} className={className} role="search">
      <label className="sr-only" htmlFor={searchId}>
        Search ZimMarket
      </label>
      <div className="flex w-full min-w-0 overflow-hidden rounded-[5px] bg-white shadow-sm ring-1 ring-black/5">
        <div className="relative shrink-0 border-r border-slate-200">
          <label htmlFor={deptId} className="sr-only">
            Department
          </label>
          <select
            id={deptId}
            name="department"
            defaultValue="all"
            className="h-full min-h-[46px] cursor-pointer appearance-none bg-transparent py-2.5 pl-3.5 pr-9 text-sm font-semibold text-slate-800 outline-none"
          >
            {DEPARTMENTS.map((d) => (
              <option key={d.value} value={d.value}>
                {d.label}
              </option>
            ))}
          </select>
          <ChevronDownGlyph className="pointer-events-none absolute right-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-500" />
        </div>
        <input
          id={searchId}
          name="q"
          type="search"
          placeholder="I'm shopping for…"
          className="min-w-0 flex-1 border-0 bg-transparent px-3 py-2.5 text-sm text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-0"
          autoComplete="off"
        />
        <button
          type="submit"
          className="shrink-0 bg-black px-5 text-sm font-bold text-white transition hover:bg-neutral-800"
        >
          Search
        </button>
      </div>
    </form>
  );
}

const HEADER_ICON_HIT =
  "relative inline-flex h-10 w-10 shrink-0 items-center justify-center text-black transition hover:opacity-75";

function HeaderActions({
  accountActive,
  className,
  showTheme,
}: {
  accountActive: boolean;
  className?: string;
  showTheme?: boolean;
}) {
  return (
    <div className={`flex items-center gap-4 ${className ?? ""}`}>
      {showTheme ? <ThemeToggle variant="onHeader" /> : null}
      <Link href="/account" className={HEADER_ICON_HIT} aria-label="Wishlist">
        <HeartGlyph className="h-[22px] w-[22px]" />
        <span className="absolute -bottom-0.5 -right-0.5 grid min-h-[1.125rem] min-w-[1.125rem] place-items-center rounded-full bg-black px-1 text-[10px] font-bold leading-none text-white">
          0
        </span>
      </Link>
      <Link href="/cart" className={HEADER_ICON_HIT} aria-label="Shopping cart">
        <CartGlyph className="h-[22px] w-[22px]" />
        <span className="absolute -bottom-0.5 -right-0.5 grid min-h-[1.125rem] min-w-[1.125rem] place-items-center rounded-full bg-black px-1 text-[10px] font-bold leading-none text-white">
          0
        </span>
      </Link>
      <Link
        href="/account"
        className={`flex h-10 shrink-0 items-center gap-2 transition hover:opacity-80 ${accountActive ? "opacity-90" : ""}`}
      >
        <span className="inline-flex h-10 w-10 shrink-0 items-center justify-center">
          <UserGlyph className="h-[22px] w-[22px] text-black" />
        </span>
        <span className="hidden flex-col text-left leading-tight sm:flex">
          <span className="text-[13px] font-bold leading-none">Log in</span>
          <span className="mt-0.5 text-[12px] font-bold leading-none">Register</span>
        </span>
      </Link>
    </div>
  );
}

function UtilityNav({
  ordersActive,
  className,
}: {
  ordersActive: boolean;
  className?: string;
}) {
  return (
    <div className={`items-center gap-x-0.5 text-[13px] text-black/90 ${className ?? ""}`}>
      <a
        href={`${env.sellerSiteUrl}/register`}
        className="whitespace-nowrap px-2 py-1.5 font-medium transition hover:text-black"
      >
        Sell on ZimMarket
      </a>
      <span className="px-1 text-black/30" aria-hidden>
        |
      </span>
      <Link
        href="/orders"
        className={`whitespace-nowrap px-2 py-1.5 font-medium transition hover:text-black ${ordersActive ? "font-semibold text-black" : ""}`}
      >
        Track your order
      </Link>
      <span className="hidden px-1 text-black/30 sm:inline" aria-hidden>
        |
      </span>
      <details className="relative hidden sm:block">
        <summary className="flex cursor-pointer list-none items-center gap-1 whitespace-nowrap px-2 py-1.5 marker:content-none hover:text-black [&::-webkit-details-marker]:hidden">
          <span className="font-medium">US Dollar</span>
          <ChevronDownGlyph className="h-3.5 w-3.5 opacity-70" />
        </summary>
        <ul className="absolute right-0 top-full z-50 mt-1 min-w-[9rem] rounded-md border border-slate-200 bg-white py-1 text-sm font-medium text-slate-800 shadow-lg">
          <li className="px-4 py-2 text-slate-500">USD (default)</li>
          <li className="px-4 py-2 text-slate-500">ZiG (soon)</li>
        </ul>
      </details>
      <span className="hidden px-1 text-black/30 sm:inline" aria-hidden>
        |
      </span>
      <details className="relative hidden sm:block">
        <summary className="flex cursor-pointer list-none items-center gap-1 whitespace-nowrap px-2 py-1.5 marker:content-none hover:text-black [&::-webkit-details-marker]:hidden">
          <span className="font-medium">English</span>
          <ChevronDownGlyph className="h-3.5 w-3.5 opacity-70" />
        </summary>
        <ul className="absolute right-0 top-full z-50 mt-1 min-w-[9rem] rounded-md border border-slate-200 bg-white py-1 text-sm font-medium text-slate-800 shadow-lg">
          <li className="px-4 py-2 text-slate-500">English</li>
        </ul>
      </details>
    </div>
  );
}

function MenuGlyph({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.25" aria-hidden>
      <path d="M4 7h16M4 12h16M4 17h16" strokeLinecap="round" />
    </svg>
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


