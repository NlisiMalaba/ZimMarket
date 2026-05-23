"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useSyncExternalStore } from "react";

import { env } from "@/lib/env";
import { getAccessToken, getCurrentUserRole, subscribeToSession } from "@/lib/auth-session";
import { ThemeToggle } from "@/components/theme-toggle";

export function SellerHeader() {
  const pathname = usePathname();
  const token = useSyncExternalStore(subscribeToSession, getAccessToken, getAccessToken);
  const role = useSyncExternalStore(subscribeToSession, getCurrentUserRole, getCurrentUserRole);
  const isAuthenticated = Boolean(token && role === "Seller");
  const isAuthPage = pathname === "/login" || pathname === "/register";

  return (
    <header className="border-b border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
      <div className="mx-auto flex max-w-5xl flex-wrap items-center justify-between gap-3 px-4 py-4 sm:px-6">
        <Link
          href={isAuthenticated ? "/dashboard" : "/login"}
          className="text-lg font-semibold tracking-tight text-slate-900 dark:text-slate-100"
        >
          ZimMarket <span className="text-slate-500 dark:text-slate-400">Sellers</span>
        </Link>
        <nav className="flex flex-wrap items-center gap-4 text-sm font-medium">
          <ThemeToggle />
          <a
            href={env.customerSiteUrl}
            className="text-slate-600 hover:text-slate-900 dark:text-slate-400 dark:hover:text-slate-100"
          >
            Shop as customer
          </a>
          {isAuthenticated ? (
            <Link
              href="/dashboard"
              className="text-slate-600 hover:text-slate-900 dark:text-slate-400 dark:hover:text-slate-100"
            >
              Dashboard
            </Link>
          ) : isAuthPage ? null : (
            <>
              <Link
                href="/login"
                className="text-slate-600 hover:text-slate-900 dark:text-slate-400 dark:hover:text-slate-100"
              >
                Sign in
              </Link>
              <Link
                href="/register"
                className="rounded-none bg-slate-900 px-3 py-1.5 text-white hover:bg-slate-800 dark:bg-slate-100 dark:text-slate-900 dark:hover:bg-slate-200"
              >
                Start selling
              </Link>
            </>
          )}
        </nav>
      </div>
    </header>
  );
}
