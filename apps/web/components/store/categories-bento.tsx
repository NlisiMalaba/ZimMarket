import Image from "next/image";
import Link from "next/link";

import type { HomeBentoCategory } from "@/lib/storefront-data";

function formatCount(n: number) {
  return new Intl.NumberFormat("en-US").format(n);
}

function BentoTile({ category, priority }: { category: HomeBentoCategory; priority?: boolean }) {
  const href = `/categories/${encodeURIComponent(category.slug)}`;
  const subline = `${formatCount(category.itemCount)} ${category.itemLabel}`;

  return (
    <Link
      href={href}
      className={`group relative block min-h-[200px] overflow-hidden rounded-[16px] sm:min-h-[220px] md:min-h-[240px] ${category.colSpan}`}
    >
      <Image
        src={category.image}
        alt={category.name}
        fill
        sizes="(max-width: 640px) 100vw, (max-width: 1024px) 66vw, 50vw"
        className="object-cover transition duration-500 group-hover:scale-105"
        priority={priority}
      />
      <div
        className="pointer-events-none absolute inset-0 bg-gradient-to-t from-black/75 via-black/25 to-black/5 transition duration-300 group-hover:from-black/80"
        aria-hidden
      />
      <div className="absolute inset-x-0 bottom-0 p-5 sm:p-6">
        <h3 className="text-xl font-semibold tracking-tight text-white sm:text-2xl">{category.name}</h3>
        <p className="mt-1 text-sm text-white/85 sm:text-[15px]">{subline}</p>
      </div>
    </Link>
  );
}

export function CategoriesBento({ categories }: { categories: HomeBentoCategory[] }) {
  if (categories.length === 0) return null;

  return (
    <section className="relative left-1/2 w-screen max-w-[100dvw] -translate-x-1/2 overflow-x-clip px-4 py-8 sm:px-6 sm:py-10 md:px-8 lg:px-10 lg:py-12">
      <div className="mx-auto max-w-[1400px]">
        <div className="mb-5 flex flex-wrap items-center justify-between gap-3 sm:mb-6 md:mb-8">
          <h2 className="font-display text-xl font-semibold tracking-tight text-foreground sm:text-2xl md:text-3xl">
            Shop by Category
          </h2>
          <Link
            href="/categories"
            className="text-sm text-foreground underline underline-offset-2 hover:text-brand sm:text-base"
          >
            View All
          </Link>
        </div>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-6 sm:gap-4">
          {categories.map((category, i) => (
            <BentoTile key={category.slug} category={category} priority={i < 2} />
          ))}
        </div>
      </div>
    </section>
  );
}
