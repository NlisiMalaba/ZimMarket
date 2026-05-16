"use client";

import { useRef } from "react";

export function HorizontalRail({
  title,
  subtitle,
  action,
  children,
  id,
}: {
  title: string;
  subtitle?: string;
  action?: React.ReactNode;
  children: React.ReactNode;
  id?: string;
}) {
  const scrollerRef = useRef<HTMLDivElement>(null);

  const scrollByDir = (dir: -1 | 1) => {
    const el = scrollerRef.current;
    if (!el) return;
    const delta = Math.min(480, el.clientWidth * 0.85) * dir;
    el.scrollBy({ left: delta, behavior: "smooth" });
  };

  return (
    <section id={id} className="py-10 sm:py-12">
      <div className="container-store mb-6 flex flex-wrap items-end justify-between gap-4">
        <div>
          <h2 className="font-display text-xl font-semibold tracking-tight text-foreground sm:text-2xl">{title}</h2>
          {subtitle ? <p className="mt-1 max-w-2xl text-sm text-muted">{subtitle}</p> : null}
        </div>
        <div className="flex items-center gap-2">
          {action}
          <div className="hidden items-center gap-1 sm:flex">
            <button
              type="button"
              onClick={() => scrollByDir(-1)}
              className="inline-flex h-10 w-10 items-center justify-center rounded-none border border-border bg-page-elevated text-foreground shadow-sm transition hover:border-brand/30 hover:text-brand"
              aria-label="Scroll left"
            >
              ‹
            </button>
            <button
              type="button"
              onClick={() => scrollByDir(1)}
              className="inline-flex h-10 w-10 items-center justify-center rounded-none border border-border bg-page-elevated text-foreground shadow-sm transition hover:border-brand/30 hover:text-brand"
              aria-label="Scroll right"
            >
              ›
            </button>
          </div>
        </div>
      </div>

      <div className="relative">
        <div
          ref={scrollerRef}
          className="container-store flex snap-x snap-mandatory gap-4 overflow-x-auto pb-2 [-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
        >
          {children}
        </div>
        <div className="pointer-events-none absolute inset-y-0 right-0 hidden w-16 bg-gradient-to-l from-page to-transparent sm:block" />
      </div>
    </section>
  );
}

export function RailItem({ children }: { children: React.ReactNode }) {
  return <div className="w-[min(300px,85vw)] shrink-0 snap-start">{children}</div>;
}
