"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

import { ProductCard } from "@/components/store/product-card";
import type { StorefrontProduct } from "@/lib/storefront-data";

function pad2(n: number) {
  return String(n).padStart(2, "0");
}

function msUntilEndOfDay() {
  const now = new Date();
  const end = new Date(now);
  end.setHours(23, 59, 59, 999);
  return Math.max(0, end.getTime() - now.getTime());
}

type CountdownParts = { hours: number; minutes: number; seconds: number };

function getCountdownParts(): CountdownParts {
  const totalSec = Math.floor(msUntilEndOfDay() / 1000);
  return {
    hours: Math.floor(totalSec / 3600),
    minutes: Math.floor((totalSec % 3600) / 60),
    seconds: totalSec % 60,
  };
}

/** Client-only after mount — avoids SSR/client clock skew hydration mismatch. */
function useDealCountdown() {
  const [parts, setParts] = useState<CountdownParts | null>(null);

  useEffect(() => {
    const tick = () => setParts(getCountdownParts());
    tick();
    const id = window.setInterval(tick, 1000);
    return () => window.clearInterval(id);
  }, []);

  return parts;
}

export function DealsOfTheDay({ products }: { products: StorefrontProduct[] }) {
  const countdown = useDealCountdown();

  if (products.length === 0) return null;

  return (
    <section className="relative left-1/2 w-screen max-w-[100dvw] -translate-x-1/2 overflow-x-clip border-b border-border bg-page-elevated py-6 sm:py-10 md:py-12 lg:py-14">
      <div className="border-b border-border px-4 pb-3 sm:px-6 sm:pb-4 md:px-8 lg:px-12">
        <div className="mx-auto flex max-w-[1400px] flex-col gap-3 sm:flex-row sm:items-center sm:justify-between sm:gap-4">
          <div className="flex min-w-0 flex-wrap items-center gap-2 sm:gap-4 md:gap-5">
            <h2 className="text-lg font-semibold tracking-tight text-foreground sm:text-xl md:text-2xl">
              Deals of the day
            </h2>
            <span className="inline-flex w-fit items-center gap-1 bg-[#f97316] px-2.5 py-1 text-[11px] font-medium text-white sm:gap-1.5 sm:px-3 sm:py-1.5 sm:text-xs md:px-3.5 md:py-2 md:text-sm">
              <span>Ends in:</span>
              <span className="font-semibold tabular-nums tracking-wide" aria-live="polite">
                {countdown
                  ? `${pad2(countdown.hours)} : ${pad2(countdown.minutes)} : ${pad2(countdown.seconds)}`
                  : "00 : 00 : 00"}
              </span>
            </span>
          </div>
          <Link
            href="/deals"
            className="shrink-0 text-sm text-foreground underline underline-offset-2 hover:text-brand sm:text-base"
          >
            View All
          </Link>
        </div>
      </div>

      <div className="relative mt-5 w-full sm:mt-7 md:mt-8">
        <div className="flex snap-x snap-mandatory gap-4 overflow-x-auto scroll-smooth px-4 py-2 pb-4 [-ms-overflow-style:none] [scrollbar-width:none] sm:gap-5 sm:px-6 md:gap-6 md:px-8 lg:px-12 [&::-webkit-scrollbar]:hidden">
          {products.map((p, i) => (
            <div key={p.id} className="w-[min(280px,82vw)] shrink-0 snap-start sm:w-[300px]">
              <ProductCard product={p} priority={i < 3} />
            </div>
          ))}
        </div>

        <div
          className="pointer-events-none absolute inset-y-0 right-0 hidden w-12 bg-gradient-to-l from-page-elevated to-transparent sm:block md:w-16"
          aria-hidden
        />
        <p className="mt-1 px-4 text-center text-[11px] text-muted sm:hidden">Swipe for more deals</p>
      </div>
    </section>
  );
}
