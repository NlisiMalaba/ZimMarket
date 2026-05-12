import Link from "next/link";

const categories = ["Groceries", "Household", "Electronics", "Fashion", "Health", "Deals"] as const;

export const metadata = {
  title: "Categories",
};

export default function CategoriesPage() {
  return (
    <div className="mx-auto max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
      <h1 className="text-2xl font-semibold text-neutral-900">All categories</h1>
      <ul className="mt-6 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {categories.map((name) => (
          <li key={name}>
            <Link
              href={`/categories/${encodeURIComponent(name.toLowerCase())}`}
              className="block rounded-lg border border-neutral-200 bg-white p-4 font-medium text-neutral-900 shadow-sm hover:border-store-accent/40"
            >
              {name}
            </Link>
          </li>
        ))}
      </ul>
    </div>
  );
}
