"use client";

import Link from "next/link";
import { useRef, useState } from "react";

import type { MegaNavColumn } from "@/lib/storefront-data";

export function MegaMenu({ columns }: { columns: MegaNavColumn[] }) {
  const [open, setOpen] = useState(false);
  const closeTimer = useRef<number | null>(null);

  const clearTimer = () => {
    if (closeTimer.current) {
      window.clearTimeout(closeTimer.current);
      closeTimer.current = null;
    }
  };

  const scheduleClose = () => {
    clearTimer();
    closeTimer.current = window.setTimeout(() => setOpen(false), 140);
  };

  return (
    <div
      className="relative"
      onPointerEnter={() => {
        clearTimer();
        setOpen(true);
      }}
      onPointerLeave={scheduleClose}
    >
      <button
        type="button"
        className={`inline-flex items-center gap-1 rounded-none px-3.5 py-2 text-sm font-semibold transition ${
          open
            ? "bg-page-elevated text-brand shadow-sm ring-1 ring-slate-200/90 dark:bg-slate-800 dark:ring-slate-600/80"
            : "text-foreground hover:bg-slate-100/80 dark:hover:bg-slate-800/70"
        }`}
        aria-expanded={open}
        aria-haspopup="true"
        onFocus={() => setOpen(true)}
        onBlur={(e) => {
          if (!e.currentTarget.contains(e.relatedTarget as Node | null)) scheduleClose();
        }}
      >
        Categories
        <Chevron className={`h-4 w-4 text-muted transition ${open ? "rotate-180" : ""}`} />
      </button>

      {open ? (
        <div
          className="absolute left-0 top-full z-50 mt-2 w-[min(100vw-2rem,920px)] rounded-none border border-border/80 bg-page-elevated/95 p-4 shadow-[var(--shadow-card-hover)] ring-1 ring-black/[0.04] backdrop-blur-xl animate-[fade-in_0.2s_ease-out_both] dark:border-slate-800/90 dark:bg-slate-950/90 dark:ring-white/[0.06]"
          role="menu"
        >
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {columns.map((col) => (
              <div
                key={col.title}
                className="rounded-none border border-border/70 bg-gradient-to-b from-page-elevated to-page p-3.5 shadow-sm dark:border-slate-800/80 dark:from-slate-900/80 dark:to-slate-950/40"
              >
                <Link
                  href={col.href}
                  className="text-sm font-semibold text-brand hover:underline"
                  role="menuitem"
                >
                  {col.title}
                </Link>
                <ul className="mt-3 space-y-2">
                  {col.items.map((item) => (
                    <li key={item.href + item.label}>
                      <Link
                        href={item.href}
                        className="text-sm text-muted transition hover:text-foreground"
                        role="menuitem"
                      >
                        {item.label}
                      </Link>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
          <div className="mt-4 flex flex-wrap items-center justify-between gap-3 border-t border-border pt-4 text-xs text-muted">
            <p>Structured discovery across electronics, fashion, home, and more.</p>
            <Link href="/categories" className="font-semibold text-brand hover:underline">
              View all categories
            </Link>
          </div>
        </div>
      ) : null}
    </div>
  );
}

function Chevron({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden>
      <path d="M6 9l6 6 6-6" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}
