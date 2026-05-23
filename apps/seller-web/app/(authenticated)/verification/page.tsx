"use client";

import { useSyncExternalStore } from "react";

import { getKycStatus, subscribeToSession } from "@/lib/auth-session";

const KYC_STATUS_LABELS: Record<string, string> = {
  NotSubmitted: "Not submitted",
  PendingReview: "Pending review",
  Approved: "Approved",
  Rejected: "Rejected",
  "0": "Not submitted",
  "1": "Pending review",
  "2": "Approved",
  "3": "Rejected",
};

export default function SellerVerificationPage() {
  const kycStatus = useSyncExternalStore(subscribeToSession, getKycStatus, getKycStatus);
  const label = kycStatus ? (KYC_STATUS_LABELS[kycStatus] ?? kycStatus) : "Unknown";

  return (
    <div className="mx-auto max-w-[1400px] space-y-4">
      <h1 className="text-3xl font-semibold tracking-tight text-foreground">Verification</h1>
      <p className="text-sm text-muted-foreground">Your seller KYC status on ZimMarket.</p>
      <div className="rounded-2xl border border-border/70 bg-card p-6 shadow-sm">
        <p className="text-sm font-medium text-muted-foreground">Current status</p>
        <p className="mt-2 text-2xl font-semibold text-foreground">{label}</p>
      </div>
    </div>
  );
}
