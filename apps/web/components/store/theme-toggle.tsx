"use client";

import { useEffect, useState } from "react";

import { useTheme } from "@/components/store/theme-provider";

type ThemeToggleProps = {
  /** Use on dark / brand backgrounds (navy header bar). */
  variant?: "default" | "onBrand" | "onHeader";
};

const HEADER_ICON_BUTTON =
  "relative inline-flex h-10 w-10 shrink-0 items-center justify-center text-black transition hover:opacity-75 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-black/40";

export function ThemeToggle({ variant = "default" }: ThemeToggleProps) {
  const { theme, toggleTheme } = useTheme();
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  const isDark = theme === "dark";
  const label = isDark ? "Switch to light mode" : "Switch to dark mode";

  if (!mounted) {
    if (variant === "onHeader") {
      return (
        <button type="button" className={HEADER_ICON_BUTTON} aria-label="Toggle theme" suppressHydrationWarning>
          <MoonIcon className="h-[22px] w-[22px] opacity-0" />
        </button>
      );
    }

    const surface =
      variant === "onBrand"
        ? "border-white/25 bg-white/10 text-white shadow-none dark:border-white/20 dark:bg-white/10"
        : "border-border/90 bg-page/90 text-foreground shadow-sm dark:border-slate-700/90 dark:bg-slate-900/50";

    return (
      <button
        type="button"
        className={`inline-flex h-10 w-10 items-center justify-center rounded-none border transition ${surface}`}
        aria-label="Toggle theme"
        suppressHydrationWarning
      >
        <MoonIcon className="h-[18px] w-[18px] opacity-0" />
      </button>
    );
  }

  if (variant === "onHeader") {
    return (
      <button
        type="button"
        onClick={toggleTheme}
        className={HEADER_ICON_BUTTON}
        aria-label={label}
      >
        {isDark ? (
          <SunIcon className="h-[22px] w-[22px]" />
        ) : (
          <MoonIcon className="h-[22px] w-[22px]" />
        )}
      </button>
    );
  }

  const surface =
    variant === "onBrand"
      ? "border-white/25 bg-white/10 text-white shadow-none hover:border-white/40 hover:bg-white/15 focus-visible:outline-white/60 dark:border-white/20 dark:bg-white/10 dark:hover:bg-white/15"
      : "border-border/90 bg-page/90 text-foreground shadow-sm hover:border-brand/30 hover:shadow-md focus-visible:outline-brand dark:border-slate-700/90 dark:bg-slate-900/50";

  const iconClass =
    variant === "onBrand"
      ? "text-white"
      : isDark
        ? "text-amber-200"
        : "text-brand";

  return (
    <button
      type="button"
      onClick={toggleTheme}
      className={`inline-flex h-10 w-10 items-center justify-center rounded-none border transition focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 ${surface}`}
      aria-label={label}
    >
      {isDark ? (
        <SunIcon className={`h-[18px] w-[18px] ${variant === "onBrand" ? "text-white" : "text-amber-200"}`} />
      ) : (
        <MoonIcon className={`h-[18px] w-[18px] ${iconClass}`} />
      )}
    </button>
  );
}

function SunIcon({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" aria-hidden>
      <circle cx="12" cy="12" r="4" />
      <path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41" strokeLinecap="round" />
    </svg>
  );
}

function MoonIcon({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" aria-hidden>
      <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z" strokeLinejoin="round" />
    </svg>
  );
}
