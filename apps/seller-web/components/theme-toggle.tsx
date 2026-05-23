"use client";

import { useEffect, useState } from "react";

import { useTheme } from "@/components/theme-provider";

const BUTTON_CLASS =
  "inline-flex h-9 w-9 shrink-0 items-center justify-center border border-slate-300 bg-white text-slate-700 transition hover:bg-slate-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-200 dark:hover:bg-slate-700 dark:focus-visible:outline-slate-300";

export function ThemeToggle() {
  const { theme, toggleTheme } = useTheme();
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  const isDark = theme === "dark";
  const label = isDark ? "Switch to light mode" : "Switch to dark mode";

  if (!mounted) {
    return (
      <button type="button" className={BUTTON_CLASS} aria-label="Toggle theme" suppressHydrationWarning>
        <MoonIcon className="h-[18px] w-[18px] opacity-0" />
      </button>
    );
  }

  return (
    <button type="button" onClick={toggleTheme} className={BUTTON_CLASS} aria-label={label}>
      {isDark ? <SunIcon className="h-[18px] w-[18px] text-amber-300" /> : <MoonIcon className="h-[18px] w-[18px]" />}
    </button>
  );
}

function SunIcon({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" aria-hidden>
      <circle cx="12" cy="12" r="4" />
      <path
        d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41"
        strokeLinecap="round"
      />
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
