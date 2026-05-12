import Link from "next/link";

const categoryPlaceholders = [
  "Groceries",
  "Household",
  "Electronics",
  "Fashion",
  "Health",
  "Deals",
] as const;

export default function HomePage() {
  return (
    <div className="pb-16">
      <section className="border-b border-neutral-200 bg-gradient-to-b from-amber-50/80 to-neutral-50">
        <div className="mx-auto max-w-6xl px-4 py-12 sm:px-6 lg:px-8">
          <p className="text-sm font-medium uppercase tracking-wide text-store-accent">Same-day delivery in your area</p>
          <h1 className="mt-2 max-w-2xl text-3xl font-semibold tracking-tight text-neutral-900 sm:text-4xl">
            Everything you need, from shops you trust
          </h1>
          <p className="mt-4 max-w-xl text-neutral-600">
            Browse products, compare sellers, and checkout securely. This storefront is for shoppers—selling and
            deliveries use dedicated portals.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <Link
              href="/deals"
              className="inline-flex items-center justify-center rounded-md bg-store-accent px-5 py-2.5 text-sm font-medium text-white shadow-sm hover:bg-store-accent-hover"
            >
              Today&apos;s deals
            </Link>
            <Link
              href="/categories"
              className="inline-flex items-center justify-center rounded-md border border-neutral-300 bg-white px-5 py-2.5 text-sm font-medium text-neutral-800 shadow-sm hover:bg-neutral-50"
            >
              Shop by category
            </Link>
          </div>
        </div>
      </section>

      <section className="mx-auto max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
        <h2 className="text-lg font-semibold text-neutral-900">Shop by category</h2>
        <ul className="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
          {categoryPlaceholders.map((name) => (
            <li key={name}>
              <Link
                href={`/categories/${encodeURIComponent(name.toLowerCase())}`}
                className="flex h-24 items-end rounded-lg border border-neutral-200 bg-white p-3 text-sm font-medium text-neutral-800 shadow-sm transition hover:border-store-accent/40 hover:shadow"
              >
                {name}
              </Link>
            </li>
          ))}
        </ul>
      </section>

      <section className="border-t border-neutral-200 bg-white">
        <div className="mx-auto max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
          <div className="flex items-end justify-between gap-4">
            <h2 className="text-lg font-semibold text-neutral-900">Recommended for you</h2>
            <Link href="/search" className="text-sm font-medium text-store-accent hover:underline">
              See more
            </Link>
          </div>
          <p className="mt-2 text-sm text-neutral-600">
            Product listings will appear here once the catalogue API is connected.
          </p>
          <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {Array.from({ length: 4 }).map((_, index) => (
              <div
                key={index}
                className="aspect-[4/5] rounded-lg border border-dashed border-neutral-200 bg-neutral-50"
                aria-hidden
              />
            ))}
          </div>
        </div>
      </section>
    </div>
  );
}
