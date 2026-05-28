"use client";

import { useCallback, useEffect, useState, useSyncExternalStore } from "react";

import { SellerKycForm } from "@/components/verification/seller-kyc-form";
import { getKycStatus, subscribeToSession } from "@/lib/auth-session";
import {
  canSubmitKycDocuments,
  formatKycStatusLabel,
  isSellerKycApproved,
  normalizeKycStatus,
} from "@/lib/seller-kyc";
import { getSellerVerificationDetails } from "@/lib/seller-kyc-upload";

export default function SellerVerificationPage() {
  const kycStatusFromToken = useSyncExternalStore(subscribeToSession, getKycStatus, getKycStatus);
  const [kycStatus, setKycStatus] = useState(kycStatusFromToken);
  const [rejectionReason, setRejectionReason] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const refreshVerification = useCallback(async () => {
    setIsLoading(true);
    setLoadError(null);

    try {
      const details = await getSellerVerificationDetails();
      setKycStatus(details.kycStatus || kycStatusFromToken);
      setRejectionReason(details.rejectionReason);
    } catch (error) {
      setLoadError(error instanceof Error ? error.message : "Unable to load verification status.");
      setKycStatus(kycStatusFromToken);
    } finally {
      setIsLoading(false);
    }
  }, [kycStatusFromToken]);

  useEffect(() => {
    void refreshVerification();
  }, [refreshVerification]);

  const label = formatKycStatusLabel(kycStatus);
  const normalized = normalizeKycStatus(kycStatus);
  const showUploadForm = canSubmitKycDocuments(kycStatus);

  return (
    <div className="mx-auto max-w-[900px] space-y-6">
      <div>
        <h1 className="text-3xl font-semibold tracking-tight text-foreground">Seller verification</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Submit your national ID and proof of residence. Admin approval is required before you can create listings.
        </p>
      </div>

      <div className="rounded-2xl border border-border/70 bg-card p-6 shadow-sm">
        <p className="text-sm font-medium text-muted-foreground">Current status</p>
        <p className="mt-2 text-2xl font-semibold text-foreground">{isLoading ? "Loading…" : label}</p>
        {loadError ? <p className="mt-2 text-sm text-destructive">{loadError}</p> : null}
      </div>

      {normalized === "pendingReview" ? (
        <div className="rounded-2xl border border-amber-500/30 bg-amber-500/10 px-6 py-5 text-sm text-amber-950 dark:text-amber-100">
          Your documents are under review. You will be able to list products once an administrator approves your
          application. Sign out and sign in again after approval so your session reflects the updated status.
        </div>
      ) : null}

      {isSellerKycApproved(kycStatus) ? (
        <div className="rounded-2xl border border-emerald-500/30 bg-emerald-500/10 px-6 py-5 text-sm text-emerald-950 dark:text-emerald-100">
          Your seller account is verified. You can create and manage product listings.
        </div>
      ) : null}

      {showUploadForm ? (
        <div className="rounded-2xl border border-border/70 bg-card p-6 shadow-sm">
          <h2 className="text-lg font-semibold text-foreground">Upload documents</h2>
          <div className="mt-4">
            <SellerKycForm
              rejectionReason={rejectionReason}
              onSubmitted={() => {
                void refreshVerification();
              }}
            />
          </div>
        </div>
      ) : null}

      {!showUploadForm && !isSellerKycApproved(kycStatus) && normalized !== "pendingReview" ? (
        <p className="text-sm text-muted-foreground">
          Document upload is not available for your current status. Contact support if you need assistance.
        </p>
      ) : null}
    </div>
  );
}
