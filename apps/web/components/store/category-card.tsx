import Link from "next/link";

import { CategoryIcon } from "@/components/store/category-icon";
import type { StorefrontCategory } from "@/lib/storefront-data";

export function CategoryCard({ category, featured }: { category: StorefrontCategory; featured?: boolean }) {
  const href = `/categories/${encodeURIComponent(category.slug)}`;

  return (
    <Link
      href={href}
      className={`group relative flex flex-col overflow-hidden rounded-[var(--radius-lg)] border border-border bg-page-elevated shadow-[var(--shadow-card)] transition duration-300 hover:-translate-y-0.5 hover:border-brand/25 hover:shadow-[var(--shadow-card-hover)] ${
        featured ? "min-h-[240px] justify-end p-6 sm:min-h-[280px] sm:p-8" : "p-5"
      }`}
    >
      <div
        className={`pointer-events-none absolute inset-0 bg-gradient-to-br ${category.accent} opacity-80 transition group-hover:opacity-100`}
      />
      <div className={`relative flex items-start justify-between gap-3 ${featured ? "" : ""}`}>
        <div
          className={`inline-flex items-center justify-center rounded-[14px] border border-border bg-page-elevated text-brand shadow-sm transition group-hover:border-brand/30 group-hover:text-brand ${
            featured ? "h-14 w-14" : "h-12 w-12"
          }`}
        >
          <CategoryIcon icon={category.icon} className={featured ? "h-8 w-8" : undefined} />
        </div>
        <span className="rounded-full border border-border bg-page-elevated px-2 py-1 text-[10px] font-semibold uppercase tracking-wide text-muted transition group-hover:border-brand/20 group-hover:text-brand">
          {featured ? "Start here" : "Browse"}
        </span>
      </div>
      <div className="relative mt-5">
        <h3 className={`font-semibold tracking-tight text-foreground ${featured ? "text-xl sm:text-2xl" : "text-base"}`}>
          {category.name}
        </h3>
        <p className={`mt-1 text-muted ${featured ? "mt-2 max-w-md text-sm leading-relaxed sm:text-base" : "text-sm"}`}>
          {category.description}
        </p>
      </div>
    </Link>
  );
}
