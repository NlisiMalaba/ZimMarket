"use client";

import { startTransition, useEffect, useSyncExternalStore } from "react";
import { useRouter } from "next/navigation";

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
      <div className="flex min-h-[calc(100vh-8rem)] items-center justify-center text-sm text-slate-500 dark:text-slate-400">
        Redirecting to sign in...
      </div>
    );
  }

  return <>{children}</>;
}
