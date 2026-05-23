"use client";

import { startTransition, useEffect, useSyncExternalStore } from "react";
import { useRouter } from "next/navigation";

import { SellerDashboardHeader } from "@/components/seller-dashboard-header";
import { SellerSidebar } from "@/components/seller-sidebar";
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
  const token = useSyncExternalStore(subscribeToSession, getAccessToken, getAccessToken);
  const role = useSyncExternalStore(subscribeToSession, getCurrentUserRole, getCurrentUserRole);

  useEffect(() => {
    if (!token || role !== "Seller") {
      if (token && role !== "Seller") {
        clearSession();
      }
      startTransition(() => {
        router.replace("/login");
      });
    }
  }, [router, role, token]);

  if (!token || role !== "Seller") {
    return (
      <div className="flex min-h-screen items-center justify-center text-sm text-muted-foreground">
        Redirecting to sign in...
      </div>
    );
  }

  return (
    <div className="flex min-h-screen bg-background">
      <SellerSidebar />
      <div className="flex min-w-0 flex-1 flex-col">
        <SellerDashboardHeader />
        <main className="flex-1 overflow-auto bg-muted/35 px-4 py-6 lg:px-8 lg:py-8">{children}</main>
      </div>
    </div>
  );
}
