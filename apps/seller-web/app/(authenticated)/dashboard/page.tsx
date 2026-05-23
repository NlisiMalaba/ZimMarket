"use client";

import { useSyncExternalStore } from "react";
import { useRouter } from "next/navigation";

import {
  clearSession,
  getAccessToken,
  getCurrentUserRole,
  getKycStatus,
  subscribeToSession,
} from "@/lib/auth-session";

const KYC_STATUS_LABELS: Record<string, string> = {
  NotSubmitted: "KYC not submitted",
  PendingReview: "KYC pending review",
  Approved: "KYC approved",
  Rejected: "KYC rejected",
  "0": "KYC not submitted",
  "1": "KYC pending review",
  "2": "KYC approved",
  "3": "KYC rejected",
};

export default function SellerDashboardPage() {
  const router = useRouter();
  const token = useSyncExternalStore(subscribeToSession, getAccessToken, getAccessToken);
  const role = useSyncExternalStore(subscribeToSession, getCurrentUserRole, getCurrentUserRole);
  const kycStatus = useSyncExternalStore(subscribeToSession, getKycStatus, getKycStatus);

  const kycLabel = kycStatus ? (KYC_STATUS_LABELS[kycStatus] ?? `KYC status: ${kycStatus}`) : "Unknown";

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
    <div className="mx-auto max-w-5xl px-4 py-10 sm:px-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900 dark:text-slate-100">Seller dashboard</h1>
          <p className="mt-2 text-slate-600 dark:text-slate-400">
            You are signed in as a seller. Product and order tools will appear here next.
          </p>
        </div>
        <button
          type="button"
          onClick={onSignOut}
          className="border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-800 hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
        >
          Sign out
        </button>
      </div>

      <div className="mt-8 grid gap-4 sm:grid-cols-2">
        <div className="border border-slate-200 bg-white p-5 dark:border-slate-700 dark:bg-slate-900">
          <p className="text-sm font-medium text-slate-500 dark:text-slate-400">Account</p>
          <p className="mt-1 text-lg font-semibold text-slate-900 dark:text-slate-100">
            {role === "Seller" ? "Seller" : "Unknown"}
          </p>
        </div>
        <div className="border border-slate-200 bg-white p-5 dark:border-slate-700 dark:bg-slate-900">
          <p className="text-sm font-medium text-slate-500 dark:text-slate-400">Verification</p>
          <p className="mt-1 text-lg font-semibold text-slate-900 dark:text-slate-100">{kycLabel}</p>
        </div>
      </div>
    </div>
  );
}
