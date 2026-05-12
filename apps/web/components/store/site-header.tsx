import Link from "next/link";

export function SiteHeader() {
  return (
    <header className="sticky top-0 z-40 border-b border-neutral-200 bg-neutral-900 text-neutral-100">
      <div className="mx-auto flex max-w-6xl flex-col gap-3 px-4 py-3 sm:flex-row sm:items-center sm:justify-between sm:px-6 lg:px-8">
        <div className="flex items-center gap-6">
          <Link href="/" className="text-lg font-semibold tracking-tight text-white">
            ZimMarket
          </Link>
          <span className="hidden text-xs text-neutral-400 sm:inline">Deliver to</span>
          <span className="hidden rounded border border-neutral-600 px-2 py-0.5 text-xs text-neutral-300 sm:inline">
            Set location
          </span>
        </div>
        <div className="flex flex-1 items-center gap-2 sm:mx-6 sm:max-w-xl lg:max-w-2xl">
          <label className="sr-only" htmlFor="store-search">
            Search ZimMarket
          </label>
          <div className="flex w-full rounded-md bg-white shadow-sm ring-1 ring-neutral-300">
            <input
              id="store-search"
              type="search"
              name="q"
              placeholder="Search ZimMarket"
              className="min-w-0 flex-1 rounded-l-md border-0 bg-transparent px-3 py-2 text-sm text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-inset focus:ring-store-accent"
              autoComplete="off"
            />
            <Link
              href="/search"
              className="rounded-r-md bg-store-accent px-4 py-2 text-sm font-medium text-white hover:bg-store-accent-hover"
            >
              Go
            </Link>
          </div>
        </div>
        <nav className="flex items-center gap-4 text-sm">
          <Link href="/account" className="hover:text-white">
            Account
          </Link>
          <Link href="/orders" className="hover:text-white">
            Orders
          </Link>
          <Link href="/cart" className="font-medium text-store-accent hover:text-amber-300">
            Cart
          </Link>
        </nav>
      </div>
    </header>
  );
}
