"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useSyncExternalStore } from "react";

import {
  clearSession,
  getAccessToken,
  getCurrentUserRole,
  subscribeToSession,
  type UserRole,
} from "@/lib/auth-session";
import { cn } from "@/lib/utils";

type NavItem = {
  href: string;
  label: string;
  allowedRoles: UserRole[];
};

const navItems: NavItem[] = [
  { href: "/dashboard", label: "Dashboard", allowedRoles: ["Admin", "SuperAdmin"] },
  { href: "/orders", label: "Orders", allowedRoles: ["Admin", "SuperAdmin"] },
  { href: "/sellers", label: "Sellers (KYC)", allowedRoles: ["Admin", "SuperAdmin"] },
  { href: "/drivers", label: "Drivers (KYC)", allowedRoles: ["Admin", "SuperAdmin"] },
  { href: "/drivers/map", label: "Drivers Map", allowedRoles: ["Admin", "SuperAdmin"] },
  { href: "/warehouse", label: "Warehouse", allowedRoles: ["Admin", "SuperAdmin"] },
  { href: "/settings", label: "Settings", allowedRoles: ["SuperAdmin"] },
];

function useSessionSnapshot() {
  return useSyncExternalStore(
    subscribeToSession,
    () => ({
      token: getAccessToken(),
      role: getCurrentUserRole(),
    }),
    () => ({
      token: getAccessToken(),
      role: getCurrentUserRole(),
    }),
  );
}

export function AdminSidebar() {
  const pathname = usePathname();
  const router = useRouter();
  const { role } = useSessionSnapshot();

  const visibleItems = navItems.filter((item) => item.allowedRoles.includes(role));

  useEffect(() => {
    if (role === "Unknown") {
      router.replace("/login");
    }
  }, [role, router]);

  return (
    <aside className="w-64 border-r bg-card p-4">
      <h1 className="mb-1 text-lg font-semibold">ZimMarket Admin</h1>
      <p className="mb-4 text-xs text-muted-foreground">Role: {role}</p>
      <nav className="space-y-1">
        {visibleItems.map((item) => (
          <Link
            key={item.href}
            href={item.href}
            className={cn(
              "block rounded-md px-3 py-2 text-sm transition-colors hover:bg-muted",
              pathname === item.href ? "bg-muted font-medium text-foreground" : "text-muted-foreground",
            )}
          >
            {item.label}
          </Link>
        ))}
      </nav>
      <button
        type="button"
        className="mt-6 w-full rounded-md border px-3 py-2 text-sm hover:bg-muted"
        onClick={() => {
          clearSession();
          router.replace("/login");
        }}
      >
        Logout
      </button>
    </aside>
  );
}
