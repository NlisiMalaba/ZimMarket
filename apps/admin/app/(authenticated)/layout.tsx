"use client";

import { useEffect, useSyncExternalStore } from "react";
import { usePathname, useRouter } from "next/navigation";

import { AdminSidebar } from "@/components/admin-sidebar";
import {
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
    if (!token) {
      router.replace("/login");
    }
  }, [router, token]);

  useEffect(() => {
    if (pathname.startsWith("/settings") && role !== "SuperAdmin") {
      router.replace("/dashboard");
    }
  }, [pathname, role, router]);

  if (!token) {
    return (
      <main className="flex min-h-screen items-center justify-center text-sm text-muted-foreground">
        Redirecting to login...
      </main>
    );
  }

  return (
    <div className="flex min-h-screen bg-background">
      <AdminSidebar />
      <main className="flex-1 p-6">{children}</main>
    </div>
  );
}
