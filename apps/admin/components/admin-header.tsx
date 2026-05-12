"use client";

import Link from "next/link";
import { useEffect, useState, useSyncExternalStore } from "react";
import {
  Bell,
  Globe2,
  Moon,
  Plus,
  Search,
  Sun,
} from "lucide-react";

import {
  getCurrentUserRole,
  subscribeToSession,
} from "@/lib/auth-session";
import { cn } from "@/lib/utils";

function useDarkMode(): [boolean, () => void] {
  const [dark, setDark] = useState(false);

  useEffect(() => {
    queueMicrotask(() => {
      setDark(document.documentElement.classList.contains("dark"));
    });
  }, []);

  const toggle = () => {
    document.documentElement.classList.toggle("dark");
    setDark(document.documentElement.classList.contains("dark"));
  };

  return [dark, toggle];
}

export function AdminHeader() {
  const [dark, toggleDark] = useDarkMode();
  const role = useSyncExternalStore(subscribeToSession, getCurrentUserRole, getCurrentUserRole);

  const initials =
    role === "SuperAdmin"
      ? "SA"
      : role === "Admin"
        ? "AD"
        : role.slice(0, 2).toUpperCase();

  return (
    <header className="sticky top-0 z-30 flex h-16 shrink-0 items-center gap-4 border-b border-border/60 bg-background/80 px-4 backdrop-blur-md lg:px-8">
      <div className="relative mx-auto flex w-full max-w-6xl flex-1 items-center gap-3 lg:max-w-none">
        <div className="relative hidden min-w-0 flex-1 md:block md:max-w-md lg:max-w-lg">
          <Search
            className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
            aria-hidden
          />
          <input
            type="search"
            placeholder="Search orders, sellers, drivers..."
            className="h-10 w-full rounded-xl border border-border/80 bg-muted/40 py-2 pl-10 pr-4 text-sm outline-none ring-ring placeholder:text-muted-foreground focus:ring-2"
            aria-label="Search"
          />
        </div>

        <div className="ml-auto flex items-center gap-2 sm:gap-3">
          <Link
            href="/orders"
            className="inline-flex h-10 shrink-0 items-center gap-2 rounded-xl bg-foreground px-4 text-sm font-medium text-background transition-opacity hover:opacity-90"
          >
            <Plus className="size-4" aria-hidden />
            <span className="hidden sm:inline">New order</span>
          </Link>

          <button
            type="button"
            onClick={toggleDark}
            className={cn(
              "flex size-10 items-center justify-center rounded-xl border border-border/80 bg-card text-muted-foreground transition-colors hover:bg-muted hover:text-foreground",
            )}
            aria-label={dark ? "Switch to light mode" : "Switch to dark mode"}
          >
            {dark ? <Sun className="size-[18px]" /> : <Moon className="size-[18px]" />}
          </button>

          <button
            type="button"
            className="hidden size-10 items-center justify-center rounded-xl border border-border/80 bg-card text-muted-foreground transition-colors hover:bg-muted hover:text-foreground sm:flex"
            aria-label="Language"
          >
            <Globe2 className="size-[18px]" />
          </button>

          <button
            type="button"
            className="relative flex size-10 items-center justify-center rounded-xl border border-border/80 bg-card text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
            aria-label="Notifications"
          >
            <Bell className="size-[18px]" />
            <span className="absolute right-2 top-2 size-2 rounded-full bg-orange-500 ring-2 ring-card" />
          </button>

          <div
            className="flex size-10 items-center justify-center rounded-full bg-gradient-to-br from-orange-400 to-amber-600 text-xs font-semibold text-white shadow-inner"
            title={role}
          >
            {initials}
          </div>
        </div>
      </div>
    </header>
  );
}
