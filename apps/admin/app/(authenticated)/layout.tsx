"use client";

import { startTransition, useEffect, useSyncExternalStore } from "react";
import { usePathname, useRouter } from "next/navigation";

import { AdminHeader } from "@/components/admin-header";
import { AdminSidebar } from "@/components/admin-sidebar";
import {
  clearSession,
  getAccessToken,
  getCurrentUserRole,
  subscribeToSession,
} from "@/lib/auth-session";

export default function AuthenticatedLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const router = useRouter();
  const pathname = usePathname();
  const token = useSyncExternalStore(subscribeToSession, getAccessToken, getAccessToken);
  const role = useSyncExternalStore(subscribeToSession, getCurrentUserRole, getCurrentUserRole);

  useEffect(() => {
    if (!token || role === "Unknown") {
      if (token && role === "Unknown") {
        clearSession();
      }
      startTransition(() => {
        router.replace("/login");
      });
      return;
    }

    if (pathname.startsWith("/settings") && role !== "SuperAdmin") {
      startTransition(() => {
        router.replace("/dashboard");
      });
    }
  }, [router, pathname, role, token]);

  if (!token || role === "Unknown") {
    return (
      <main className="flex min-h-screen items-center justify-center text-sm text-muted-foreground">
        Redirecting to login...
      </main>
    );
  }

  return (
    <div className="flex min-h-screen bg-background">
      <AdminSidebar />
      <div className="flex min-w-0 flex-1 flex-col">
        <AdminHeader />
        <main className="flex-1 overflow-auto bg-muted/35 px-4 py-6 lg:px-8 lg:py-8">{children}</main>
      </div>
    </div>
  );
}
