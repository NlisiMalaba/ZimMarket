"use client";

import type { ComponentType } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useSyncExternalStore } from "react";
import {
  LayoutDashboard,
  LogOut,
  Package,
  ShieldCheck,
  Tag,
} from "lucide-react";

import {
  clearSession,
  getAccessToken,
  getUserDisplayName,
  getUserEmail,
  getUserInitials,
  subscribeToSession,
} from "@/lib/auth-session";
import { cn } from "@/lib/utils";

type NavItem = {
  href: string;
  label: string;
  icon: ComponentType<{ className?: string }>;
  badge?: string;
};

type NavSection = {
  title: string;
  items: NavItem[];
};

const navSections: NavSection[] = [
  {
    title: "Overview",
    items: [{ href: "/dashboard", label: "Dashboard", icon: LayoutDashboard }],
  },
  {
    title: "Commerce",
    items: [
      { href: "/orders", label: "Orders", icon: Package },
      { href: "/products", label: "Products", icon: Tag },
    ],
  },
  {
    title: "Account",
    items: [{ href: "/verification", label: "Verification", icon: ShieldCheck }],
  },
];

function SellerLogo() {
  return (
    <div className="flex items-center gap-3">
      <div className="flex size-11 items-center justify-center rounded-xl bg-zinc-900 text-[10px] font-bold leading-tight text-white shadow-md dark:bg-zinc-100 dark:text-zinc-900">
        ZM
      </div>
      <div>
        <p className="text-sm font-semibold tracking-tight text-foreground">ZimMarket</p>
        <p className="text-[11px] font-medium uppercase tracking-[0.18em] text-muted-foreground">Sellers</p>
      </div>
    </div>
  );
}

export function SellerSidebar({ orderCount }: { orderCount?: number }) {
  const pathname = usePathname();
  const router = useRouter();
  const token = useSyncExternalStore(subscribeToSession, getAccessToken, getAccessToken);
  const displayName = useSyncExternalStore(subscribeToSession, getUserDisplayName, getUserDisplayName);
  const email = useSyncExternalStore(subscribeToSession, getUserEmail, getUserEmail);
  const initials = useSyncExternalStore(subscribeToSession, getUserInitials, getUserInitials);

  const sections = navSections.map((section) => ({
    ...section,
    items: section.items.map((item) =>
      item.href === "/orders" && orderCount && orderCount > 0
        ? { ...item, badge: String(orderCount) }
        : item,
    ),
  }));

  function linkActive(href: string): boolean {
    if (pathname === href) {
      return true;
    }

    return pathname.startsWith(`${href.replace(/\/$/, "")}/`);
  }

  useEffect(() => {
    for (const section of navSections) {
      for (const item of section.items) {
        router.prefetch(item.href);
      }
    }
  }, [router]);

  const onSignOut = async () => {
    try {
      await fetch("/api/auth/logout", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ accessToken: token }),
      });
    } finally {
      clearSession();
      router.replace("/login");
    }
  };

  return (
    <aside className="sticky top-0 flex h-screen w-[260px] shrink-0 flex-col border-r border-border/60 bg-sidebar py-6">
      <div className="px-5 pb-8">
        <SellerLogo />
      </div>

      <nav className="flex flex-1 flex-col gap-7 overflow-y-auto px-3 pb-6">
        {sections.map((section) => (
          <div key={section.title}>
            <p className="mb-2 px-2 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              {section.title}
            </p>
            <ul className="space-y-0.5">
              {section.items.map((item) => {
                const active = linkActive(item.href);
                const Icon = item.icon;
                return (
                  <li key={item.href}>
                    <Link
                      href={item.href}
                      className={cn(
                        "flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors",
                        active
                          ? "bg-muted text-foreground shadow-sm"
                          : "text-muted-foreground hover:bg-muted/70 hover:text-foreground",
                      )}
                    >
                      <Icon className="size-[18px] shrink-0 opacity-80" aria-hidden />
                      <span className="flex-1">{item.label}</span>
                      {item.badge ? (
                        <span className="rounded-md bg-orange-500/15 px-1.5 py-0.5 text-[10px] font-semibold text-orange-600 dark:text-orange-400">
                          {item.badge}
                        </span>
                      ) : null}
                    </Link>
                  </li>
                );
              })}
            </ul>
          </div>
        ))}
      </nav>

      <div className="mt-auto border-t border-border/60 px-4 pt-4">
        <div className="flex items-center gap-3 rounded-xl bg-muted/50 p-3">
          <div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-orange-400 to-amber-600 text-xs font-semibold text-white">
            {initials}
          </div>
          <div className="min-w-0 flex-1">
            <p className="truncate text-sm font-medium text-foreground">{displayName}</p>
            <p className="truncate text-xs text-muted-foreground">{email ?? "Seller"}</p>
          </div>
          <button
            type="button"
            onClick={onSignOut}
            className="flex size-9 shrink-0 items-center justify-center rounded-lg border border-border/80 bg-card text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
            aria-label="Log out"
          >
            <LogOut className="size-[18px]" aria-hidden />
          </button>
        </div>
      </div>
    </aside>
  );
}
