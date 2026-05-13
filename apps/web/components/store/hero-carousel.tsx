"use client";

import Image from "next/image";
import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

import type { HeroSlide } from "@/lib/storefront-data";

const SIDE_PROMOS = [
  {
    id: "bags",
    badge: "20% OFF",
    title: "Everyday carry",
    subtitle: "Bags, wallets, and the small things you touch daily.",
    href: "/categories/fashion",
    image:
      "https://images.unsplash.com/photo-1590874103328-eac38a683ce7?auto=format&fit=crop&w=900&q=80",
  },
  {
    id: "phones",
    badge: "DEALS",
    title: "Phones & accessories",
    subtitle: "Clear listings, verified sellers, honest delivery windows.",
    href: "/categories/phones",
    image:
      "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=900&q=80",
  },
] as const;

const SERVICE_ITEMS = [
  {
    title: "Free delivery",
    subtitle: "On qualifying orders—see checkout for your area.",
    icon: <RocketIcon />,
  },
  {
    title: "Easy returns",
    subtitle: "If something arrives wrong, you're not on your own.",
    icon: <ReturnIcon />,
  },
  {
    title: "Secure payment",
    subtitle: "Encrypted checkout with clear receipts.",
    icon: <CardIcon />,
  },
  {
    title: "Support that answers",
    subtitle: "Real help for real orders—not template loops.",
    icon: <HeadsetIcon />,
  },
  {
    title: "Gift-friendly",
    subtitle: "Send something nice without the guesswork.",
    icon: <GiftIcon />,
  },
] as const;

export function HeroCarousel({ slides }: { slides: HeroSlide[] }) {
  const [index, setIndex] = useState(0);
  const len = slides.length;

  const go = useCallback(
    (delta: number) => {
      setIndex((i) => (i + delta + len) % len);
    },
    [len],
  );

  useEffect(() => {
    const t = window.setInterval(() => {
      setIndex((i) => (i + 1) % len);
    }, 7000);
    return () => window.clearInterval(t);
  }, [len]);

  return (
    <section className="relative w-full min-w-0 overflow-hidden">
      <div className="mx-auto w-full max-w-none px-4 py-6 sm:px-6 sm:py-8 lg:px-8 lg:py-10 xl:px-10 2xl:px-12">
        <div className="grid gap-4 lg:grid-cols-12 lg:items-stretch lg:gap-5">
          <div className="relative min-h-0 lg:col-span-8">
            <div className="relative h-full min-h-[380px] overflow-hidden rounded-none border border-border/80 bg-slate-950 shadow-[0_24px_80px_rgb(15_23_42/0.14)] ring-1 ring-slate-900/10 dark:border-slate-800/80 dark:shadow-[0_28px_90px_rgb(0_0_0/0.45)] dark:ring-white/10 sm:min-h-[420px] lg:min-h-[460px]">
              <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_at_top,rgb(255_255_255/0.12),transparent_55%)]" />

              {slides.map((s, i) => (
                <div
                  key={s.id}
                  className={`absolute inset-0 transition-[opacity,transform] duration-[900ms] ease-out ${i === index ? "opacity-100 scale-100" : "pointer-events-none opacity-0 scale-[1.01]"}`}
                  aria-hidden={i !== index}
                >
                  <Image
                    src={s.image}
                    alt=""
                    fill
                    priority={i === 0}
                    className="object-cover"
                    sizes="(min-width: 1024px) 72vw, 100vw"
                  />
                  <div className={`absolute inset-0 bg-gradient-to-r ${s.gradient}`} />
                  <div className="pointer-events-none absolute inset-0 bg-gradient-to-t from-slate-950/92 via-slate-950/45 to-slate-950/20 lg:from-slate-950/85 lg:via-slate-950/40 lg:to-slate-950/15" />
                  <div className="pointer-events-none absolute inset-0 bg-gradient-to-r from-slate-950/80 via-slate-950/40 to-transparent" />
                  <div className="absolute inset-0 z-[1] flex flex-col justify-end p-6 pb-[5.75rem] sm:p-8 sm:pb-28 lg:justify-center lg:py-10 lg:pl-20 lg:pr-20 lg:pb-36">
                    <div className="relative max-w-xl animate-[fade-in_0.55s_ease-out_both] lg:max-w-lg">
                      <span className="inline-flex w-fit rounded-none border border-white/25 bg-white/10 px-3 py-1 text-[11px] font-semibold uppercase tracking-wider text-white">
                        {s.eyebrow}
                      </span>
                      <h1 className="mt-4 font-display text-balance-safe text-[1.75rem] font-semibold leading-[1.12] tracking-[-0.02em] text-white [text-shadow:0_2px_24px_rgb(0_0_0/0.55)] sm:text-4xl lg:text-[2.5rem]">
                        {s.title}
                      </h1>
                      <p className="mt-3 max-w-xl text-[14px] leading-relaxed text-white/95 sm:text-[15px] lg:mt-4">
                        {s.subtitle}
                      </p>
                      <div className="mt-7 flex flex-wrap gap-3 lg:mt-9">
                        <Link
                          href={s.ctaHref}
                          className="inline-flex items-center justify-center rounded-none bg-gradient-to-b from-cta to-cta-hover px-6 py-3 text-sm font-semibold text-white shadow-[0_10px_28px_rgb(245_158_11/0.35)] ring-1 ring-white/20 transition hover:brightness-110 active:scale-[0.99]"
                        >
                          {s.ctaLabel}
                        </Link>
                        {s.secondaryCtaHref && s.secondaryCtaLabel ? (
                          <Link
                            href={s.secondaryCtaHref}
                            className="inline-flex items-center justify-center rounded-none border border-white/35 bg-white/10 px-6 py-3 text-sm font-semibold text-white shadow-[inset_0_1px_0_rgb(255_255_255/0.12)] backdrop-blur-md transition hover:bg-white/14"
                          >
                            {s.secondaryCtaLabel}
                          </Link>
                        ) : null}
                      </div>
                    </div>
                  </div>
                </div>
              ))}

              <div className="absolute bottom-[5.25rem] left-3 z-20 flex sm:left-4 lg:bottom-auto lg:left-4 lg:top-1/2 lg:-translate-y-1/2">
                <button
                  type="button"
                  onClick={() => go(-1)}
                  className="inline-flex h-11 w-11 items-center justify-center rounded-none border border-white/30 bg-slate-950/70 text-white shadow-lg backdrop-blur-md transition hover:border-white/45 hover:bg-slate-950/85 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white/60"
                  aria-label="Previous promotion"
                >
                  <ChevronLeft className="h-5 w-5" />
                </button>
              </div>
              <div className="absolute bottom-[5.25rem] right-3 z-20 flex sm:right-4 lg:bottom-auto lg:right-4 lg:top-1/2 lg:-translate-y-1/2">
                <button
                  type="button"
                  onClick={() => go(1)}
                  className="inline-flex h-11 w-11 items-center justify-center rounded-none border border-white/30 bg-slate-950/70 text-white shadow-lg backdrop-blur-md transition hover:border-white/45 hover:bg-slate-950/85 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white/60"
                  aria-label="Next promotion"
                >
                  <ChevronRight className="h-5 w-5" />
                </button>
              </div>

              <div className="absolute bottom-0 left-0 right-0 z-20 flex items-center justify-between gap-4 border-t border-white/10 bg-slate-950/55 px-4 py-3 backdrop-blur-xl supports-[backdrop-filter]:bg-slate-950/40 sm:px-5">
                <div className="flex items-center gap-2">
                  {slides.map((s, i) => (
                    <button
                      key={s.id}
                      type="button"
                      onClick={() => setIndex(i)}
                      className={`group relative h-2.5 overflow-hidden rounded-none transition-all duration-500 ${i === index ? "w-12 bg-white/15" : "w-2.5 bg-white/25 hover:bg-white/40"}`}
                      aria-label={`Show promotion ${i + 1}`}
                      aria-current={i === index}
                    >
                      <span
                        className={`absolute inset-y-0 left-0 rounded-none bg-gradient-to-r from-brand via-brand-muted to-brand transition-all duration-500 ${i === index ? "w-full opacity-100" : "w-0 opacity-0"}`}
                      />
                    </button>
                  ))}
                </div>
                <div className="hidden items-center gap-2 text-[11px] font-medium text-white/75 sm:flex">
                  <LockMini className="h-3.5 w-3.5 text-emerald-300/90" />
                  <span>Checkout is encrypted</span>
                </div>
              </div>
            </div>
          </div>

          <div className="flex min-h-0 flex-col gap-4 lg:col-span-4 lg:min-h-[460px]">
            {SIDE_PROMOS.map((p) => (
              <HeroSidePromo key={p.id} {...p} />
            ))}
          </div>
        </div>

        <HeroServiceStrip />
      </div>
    </section>
  );
}

function HeroSidePromo({
  badge,
  title,
  subtitle,
  href,
  image,
}: {
  badge: string;
  title: string;
  subtitle: string;
  href: string;
  image: string;
}) {
  return (
    <Link
      href={href}
      className="group relative flex min-h-[200px] flex-1 flex-row overflow-hidden rounded-none border border-border/80 bg-page-elevated shadow-[var(--shadow-card)] ring-1 ring-slate-900/[0.04] transition duration-300 hover:-translate-y-0.5 hover:shadow-[var(--shadow-card-hover)] dark:border-slate-800/80 dark:bg-slate-900/45 dark:ring-white/[0.06] lg:min-h-0 lg:flex-1"
    >
      <div className="pointer-events-none absolute -right-16 top-1/2 z-[1] h-56 w-56 -translate-y-1/2 rounded-none bg-gradient-to-br from-brand/15 to-transparent blur-2xl dark:from-brand/25" />
      <div className="relative z-[2] flex min-w-0 flex-1 flex-col justify-center p-5 sm:p-6">
        <div className="absolute right-4 top-4 grid h-[4.25rem] w-[4.25rem] place-items-center rounded-none bg-gradient-to-br from-cta to-cta-hover text-center text-[11px] font-bold leading-tight text-white shadow-[0_8px_24px_rgb(245_158_11/0.35)] ring-2 ring-white/90 dark:ring-slate-900">
          {badge}
        </div>
        <p className="max-w-[58%] font-display text-lg font-semibold tracking-tight text-foreground sm:text-xl">
          {title}
        </p>
        <p className="mt-2 max-w-[62%] text-sm leading-relaxed text-muted">{subtitle}</p>
        <span className="mt-4 inline-flex items-center gap-1 text-sm font-semibold text-brand transition group-hover:gap-2">
          Shop now
          <span aria-hidden>→</span>
        </span>
      </div>
      <div className="relative h-full min-h-[200px] w-[42%] shrink-0 sm:w-[45%]">
        <Image
          src={image}
          alt=""
          fill
          className="object-cover object-center transition duration-500 group-hover:scale-105"
          sizes="(min-width: 1024px) 200px, 40vw"
        />
        <div className="absolute inset-0 bg-gradient-to-r from-page-elevated via-page-elevated/40 to-transparent dark:from-slate-900 dark:via-slate-900/50" />
      </div>
    </Link>
  );
}

function HeroServiceStrip() {
  return (
    <div className="mt-6 rounded-none border border-border/80 bg-page-elevated/90 px-4 py-6 shadow-[var(--shadow-card)] ring-1 ring-slate-900/[0.03] dark:border-slate-800/80 dark:bg-slate-900/40 dark:ring-white/[0.05] sm:px-6 lg:px-8">
      <ul className="grid grid-cols-2 gap-x-4 gap-y-6 sm:grid-cols-3 lg:grid-cols-5 lg:gap-6">
        {SERVICE_ITEMS.map((item) => (
          <li key={item.title} className="flex gap-3">
            <span className="grid h-11 w-11 shrink-0 place-items-center rounded-none border border-brand/15 bg-brand/[0.06] text-brand dark:border-brand/25 dark:bg-brand/15 dark:text-white">
              {item.icon}
            </span>
            <div className="min-w-0">
              <p className="text-sm font-semibold text-foreground">{item.title}</p>
              <p className="mt-1 text-xs leading-relaxed text-muted">{item.subtitle}</p>
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}

function ChevronLeft({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden>
      <path d="M15 6l-6 6 6 6" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function ChevronRight({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden>
      <path d="M9 6l6 6-6 6" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function LockMini({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden>
      <rect x="5" y="11" width="14" height="10" rx="2" />
      <path d="M8 11V8a4 4 0 0 1 8 0v3" strokeLinecap="round" />
    </svg>
  );
}

function RocketIcon() {
  return (
    <svg className="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" aria-hidden>
      <path d="M12 2.5s4.5 4.2 4.5 9.2c0 2.1-.6 4-1.6 5.5L12 22l-2.9-4.8c-1-1.5-1.6-3.4-1.6-5.5 0-5 4.5-9.2 4.5-9.2z" strokeLinejoin="round" />
      <path d="M12 9v3" strokeLinecap="round" />
    </svg>
  );
}

function ReturnIcon() {
  return (
    <svg className="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" aria-hidden>
      <path d="M4 8h11a4 4 0 0 1 4 4 4 4 0 0 1-4 4H8" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M8 16L4 20l4 4" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function CardIcon() {
  return (
    <svg className="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" aria-hidden>
      <rect x="3" y="5" width="18" height="14" rx="2" />
      <path d="M3 10h18" />
      <path d="M7 15h4" strokeLinecap="round" />
    </svg>
  );
}

function HeadsetIcon() {
  return (
    <svg className="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" aria-hidden>
      <path d="M5 16v-3a7 7 0 0 1 14 0v3" strokeLinecap="round" />
      <rect x="3" y="14" width="5" height="6" rx="1.5" />
      <rect x="16" y="14" width="5" height="6" rx="1.5" />
    </svg>
  );
}

function GiftIcon() {
  return (
    <svg className="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" aria-hidden>
      <rect x="3" y="10" width="18" height="11" rx="2" strokeLinejoin="round" />
      <path d="M12 10V21M3 14h18" strokeLinecap="round" />
      <path d="M12 10H8.5a2.5 2.5 0 0 1 0-5C11 5 12 10 12 10zm0 0h3.5a2.5 2.5 0 0 0 0-5C13 5 12 10 12 10z" strokeLinejoin="round" />
    </svg>
  );
}
