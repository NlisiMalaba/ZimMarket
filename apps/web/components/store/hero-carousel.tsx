"use client";

import Image from "next/image";
import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

import type { HeroSlide } from "@/lib/storefront-data";

const AUTO_PLAY_MS = 5000;

/** Single source of truth for hero layout — avoids SSR/client class drift during HMR. */
const HERO_ROOT_HEIGHT =
  "relative min-h-[clamp(36rem,72vh,56rem)] sm:min-h-[clamp(38rem,74vh,58rem)] lg:min-h-[clamp(42rem,78vh,62rem)]";
const HERO_DOTS_BAR =
  "absolute bottom-8 left-1/2 z-30 flex -translate-x-1/2 gap-2 sm:bottom-10";

export function HeroCarousel({ slides }: { slides: HeroSlide[] }) {
  const [index, setIndex] = useState(0);
  const [ready, setReady] = useState(false);
  const len = slides.length;

  const go = useCallback(
    (delta: number) => {
      setIndex((i) => (i + delta + len) % len);
    },
    [len],
  );

  useEffect(() => {
    setReady(true);
  }, []);

  useEffect(() => {
    if (!ready) return;
    const t = window.setInterval(() => {
      setIndex((i) => (i + 1) % len);
    }, AUTO_PLAY_MS);
    return () => window.clearInterval(t);
  }, [len, ready]);

  return (
    <section className="relative w-full min-w-0 overflow-hidden">
      <div className={HERO_ROOT_HEIGHT}>
        {slides.map((slide, i) => (
          <HeroSlidePanel key={slide.id} slide={slide} active={i === index} />
        ))}

        <HeroDecorations />

        <button
          type="button"
          onClick={() => go(-1)}
          className="absolute left-3 top-[42%] z-30 inline-flex h-11 w-11 -translate-y-1/2 items-center justify-center rounded-full border border-white/60 bg-white/75 text-neutral-800 shadow-lg backdrop-blur-sm transition hover:bg-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-400 sm:left-5 lg:left-8"
          aria-label="Previous slide"
        >
          <ChevronLeft className="h-5 w-5" />
        </button>
        <button
          type="button"
          onClick={() => go(1)}
          className="absolute right-3 top-[42%] z-30 inline-flex h-11 w-11 -translate-y-1/2 items-center justify-center rounded-full border border-white/60 bg-white/75 text-neutral-800 shadow-lg backdrop-blur-sm transition hover:bg-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-400 sm:right-5 lg:right-8"
          aria-label="Next slide"
        >
          <ChevronRight className="h-5 w-5" />
        </button>

        <div className={HERO_DOTS_BAR} suppressHydrationWarning>
          {slides.map((slide, i) => (
            <button
              key={slide.id}
              type="button"
              onClick={() => setIndex(i)}
              className={`h-2 rounded-full transition-all duration-300 ${i === index ? "w-7 bg-orange-500" : "w-2 bg-black/25 hover:bg-black/40"}`}
              aria-label={`Go to slide ${i + 1}`}
              {...(i === index ? { "aria-current": true as const } : {})}
            />
          ))}
        </div>

      </div>
    </section>
  );
}

function HeroSlidePanel({ slide, active }: { slide: HeroSlide; active: boolean }) {
  return (
    <div
      className={`absolute inset-0 transition-opacity duration-[900ms] ease-in-out ${active ? "opacity-100" : "pointer-events-none opacity-0"}`}
      aria-hidden={!active}
    >
      <div className={`absolute inset-0 bg-gradient-to-b ${slide.background}`} />

      <div className="absolute bottom-0 left-0 right-0 top-0 mx-auto flex max-w-[1400px] items-end justify-center px-4 pb-16 pt-10 sm:px-6 sm:pb-20 sm:pt-12 lg:px-10 lg:pb-24 lg:pt-14">
        <div className="pointer-events-none absolute bottom-0 left-0 hidden h-[82%] w-[34%] max-w-[360px] sm:block lg:w-[32%] lg:max-w-[420px] relative">
          <Image
            src={slide.leftImage}
            alt=""
            fill
            priority={slide.id === "drop"}
            className="object-contain object-bottom"
            sizes="(min-width: 1024px) 30vw, 34vw"
          />
        </div>

        <div className="relative pointer-events-none absolute bottom-0 right-0 hidden h-[82%] w-[34%] max-w-[360px] sm:block lg:w-[32%] lg:max-w-[420px]">
          <Image
            src={slide.rightImage}
            alt=""
            fill
            className="object-contain object-bottom"
            sizes="(min-width: 1024px) 30vw, 34vw"
          />
        </div>

        <div
          className={`relative z-10 flex max-w-lg flex-col items-center text-center transition-all duration-700 ${active ? "translate-y-0 opacity-100" : "translate-y-3 opacity-0"}`}
        >
          <p className="font-display text-xl font-bold tracking-tight text-black sm:text-2xl">
            <span className="text-black">Zim</span>
            <span className="text-white [text-shadow:0_1px_0_rgb(0_0_0/0.15)]">Market</span>
          </p>

          <p className="mt-4 text-sm font-bold uppercase tracking-[0.2em] text-black/85 sm:text-base">
            {slide.kicker}
          </p>

          <div className="relative mt-1">
            <Sunburst className="absolute left-1/2 top-1/2 h-28 w-28 -translate-x-1/2 -translate-y-1/2 text-orange-400/90 sm:h-36 sm:w-36" />
            <p className="relative font-display text-5xl font-extrabold uppercase leading-none tracking-tight text-[#f97316] sm:text-6xl lg:text-7xl">
              {slide.highlight}
            </p>
          </div>

          <p className="mt-1 font-display text-2xl font-extrabold uppercase tracking-tight text-[#ea580c] sm:text-3xl lg:text-4xl">
            {slide.headlineTail}
          </p>

          <p className="mt-3 text-sm font-medium text-neutral-700 sm:text-base">{slide.subtitle}</p>

          <Link
            href={slide.ctaHref}
            className="pointer-events-auto mt-6 inline-flex items-center justify-center rounded-lg bg-[#f97316] px-8 py-3 text-sm font-bold text-white shadow-[0_8px_24px_rgb(249_115_22/0.45)] transition hover:bg-[#ea580c] active:scale-[0.98]"
          >
            {slide.ctaLabel}
          </Link>
        </div>
      </div>
    </div>
  );
}

function HeroDecorations() {
  return (
    <>
      <div className="pointer-events-none absolute inset-0 overflow-hidden">
        <WavyBlob className="absolute -left-24 top-8 h-64 w-64 text-white/25" />
        <WavyBlob className="absolute -right-20 top-12 h-56 w-56 rotate-180 text-white/20" />
      </div>

      <Balloon className="absolute left-[8%] top-6 h-14 w-10 opacity-90 sm:left-[12%] sm:top-8 sm:h-16" />
      <Balloon className="absolute right-[8%] top-10 h-12 w-9 opacity-85 sm:right-[12%] sm:h-14" variant="yellow" />

      <Sparkle className="absolute left-[18%] top-[22%] h-3 w-3" />
      <Sparkle className="absolute left-[42%] top-[14%] h-2 w-2" />
      <Sparkle className="absolute right-[28%] top-[18%] h-3 w-3" />
      <Sparkle className="absolute right-[15%] top-[30%] h-2.5 w-2.5" />
      <Sparkle className="absolute left-[30%] top-[38%] h-2 w-2" />
    </>
  );
}

function Sunburst({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 100 100" fill="currentColor" aria-hidden>
      {Array.from({ length: 12 }).map((_, i) => (
        <path
          key={i}
          d="M50 8 L54 38 L50 42 L46 38 Z"
          transform={`rotate(${i * 30} 50 50)`}
          opacity={0.35 + (i % 3) * 0.15}
        />
      ))}
    </svg>
  );
}

function WavyBlob({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 200 200" fill="currentColor" aria-hidden>
      <path d="M40,100 C40,40 100,20 140,60 C180,100 160,160 100,170 C40,180 40,140 40,100 Z" />
    </svg>
  );
}

function Balloon({ className, variant = "orange" }: { className?: string; variant?: "orange" | "yellow" }) {
  const stripe = variant === "yellow" ? "#fbbf24" : "#fb923c";
  return (
    <svg className={className} viewBox="0 0 40 56" aria-hidden>
      <ellipse cx="20" cy="18" rx="14" ry="16" fill={stripe} />
      <path d="M14 30 Q20 38 26 30" fill={stripe} opacity="0.85" />
      <line x1="20" y1="34" x2="20" y2="52" stroke="#94a3b8" strokeWidth="1.5" />
      <path d="M8 14 L32 14" stroke="white" strokeWidth="3" opacity="0.5" />
      <path d="M10 20 L30 20" stroke="white" strokeWidth="2" opacity="0.4" />
    </svg>
  );
}

function Sparkle({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="white" aria-hidden>
      <path d="M12 2l2 7h7l-5.5 4 2 7L12 16l-5.5 4 2-7L3 9h7z" opacity="0.85" />
    </svg>
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
