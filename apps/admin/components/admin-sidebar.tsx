"use client";

import type { ComponentType } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useSyncExternalStore } from "react";
import {
  LayoutDashboard,
  LogOut,
  MapPin,
  Package,
  Settings,
  Truck,
  Users,
  Warehouse,
} from "lucide-react";

import {
  clearSession,
  getCurrentUserRole,
  subscribeToSession,
  type UserRole,
} from "@/lib/auth-session";
import { cn } from "@/lib/utils";

type NavItem = {
  href: string;
  label: string;
  icon: ComponentType<{ className?: string }>;
  badge?: string;
  allowedRoles: UserRole[];
};

type NavSection = {
  title: string;
  items: NavItem[];
};

const navSections: NavSection[] = [
  {
    title: "Overview",
    items: [
      {
        href: "/dashboard",
        label: "Dashboard",
        icon: LayoutDashboard,
        allowedRoles: ["Admin", "SuperAdmin"],
      },
    ],
  },
  {
    title: "Commerce",
    items: [
      {
        href: "/orders",
        label: "Orders",
        icon: Package,
        allowedRoles: ["Admin", "SuperAdmin"],
      },
      {
        href: "/warehouse",
        label: "Warehouse",
        icon: Warehouse,
        allowedRoles: ["Admin", "SuperAdmin"],
      },
    ],
  },
  {
    title: "People",
    items: [
      {
        href: "/sellers",
        label: "Sellers (KYC)",
        icon: Users,
        allowedRoles: ["Admin", "SuperAdmin"],
      },
      {
        href: "/drivers",
        label: "Drivers (KYC)",
        icon: Truck,
        allowedRoles: ["Admin", "SuperAdmin"],
      },
      {
        href: "/drivers/map",
        label: "Drivers map",
        icon: MapPin,
        allowedRoles: ["Admin", "SuperAdmin"],
      },
    ],
  },
  {
    title: "System",
    items: [
      {
        href: "/settings",
        label: "Settings",
        icon: Settings,
        allowedRoles: ["SuperAdmin"],
      },
    ],
  },
];

function ZenithLogo() {
  return (
    <div className="flex items-center gap-3">
      <div className="flex size-11 items-center justify-center rounded-xl bg-zinc-900 text-[10px] font-bold leading-tight text-white shadow-md dark:bg-zinc-100 dark:text-zinc-900">
        ZM
      </div>
      <div>
        <p className="text-sm font-semibold tracking-tight text-foreground">ZimMarket</p>
        <p className="text-[11px] font-medium uppercase tracking-[0.18em] text-muted-foreground">
          Admin
        </p>
      </div>
    </div>
  );
}

export function AdminSidebar() {
  const pathname = usePathname();
  const router = useRouter();
  const role = useSyncExternalStore(subscribeToSession, getCurrentUserRole, getCurrentUserRole);

  const visibleSections = navSections
    .map((section) => ({
      ...section,
      items: section.items.filter((item) => item.allowedRoles.includes(role)),
    }))
    .filter((section) => section.items.length > 0);

  const flatHrefs = visibleSections.flatMap((section) => section.items.map((item) => item.href));

  function linkActive(href: string): boolean {
    if (pathname === href) {
      return true;
    }
    const obscuredByMoreSpecific = flatHrefs.some(
      (h) =>
        h !== href &&
        h.startsWith(`${href.replace(/\/$/, "")}/`) &&
        (pathname === h || pathname.startsWith(`${h}/`)),
    );
    if (obscuredByMoreSpecific) {
      return false;
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

  const displayName =
    role === "SuperAdmin" ? "Super Admin" : role === "Admin" ? "Administrator" : role;

  return (
    <aside className="sticky top-0 flex h-screen w-[260px] shrink-0 flex-col border-r border-border/60 bg-sidebar py-6">
      <div className="px-5 pb-8">
        <ZenithLogo />
      </div>

      <nav className="flex flex-1 flex-col gap-7 overflow-y-auto px-3 pb-6">
        {visibleSections.map((section) => (
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
                        <span className="rounded-md bg-muted px-1.5 py-0.5 text-[10px] font-semibold text-muted-foreground">
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
          <div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-teal-400 to-cyan-600 text-xs font-semibold text-white">
            {role === "SuperAdmin" ? "SA" : "AD"}
          </div>
          <div className="min-w-0 flex-1">
            <p className="truncate text-sm font-medium text-foreground">{displayName}</p>
            <p className="truncate text-xs text-muted-foreground">{role}</p>
          </div>
          <button
            type="button"
            onClick={() => {
              clearSession();
              router.replace("/login");
            }}
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
