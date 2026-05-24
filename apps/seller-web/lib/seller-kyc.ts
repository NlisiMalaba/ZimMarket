/** Matches <see cref="ZimMarket.Application.Files.FileType"/> for seller KYC uploads. */
export const SELLER_KYC_FILE_TYPE = {
  nationalId: 2,
  proofOfResidence: 3,
} as const;

export type NormalizedKycStatus = "notSubmitted" | "pendingReview" | "approved" | "rejected" | "unknown";

export function normalizeKycStatus(value: string | number | null | undefined): NormalizedKycStatus {
  if (value === null || value === undefined) {
    return "unknown";
  }

  const raw = String(value).trim().toLowerCase();
  if (raw === "0" || raw === "notsubmitted") {
    return "notSubmitted";
  }

  if (raw === "1" || raw === "pendingreview" || raw === "pending") {
    return "pendingReview";
  }

  if (raw === "2" || raw === "approved") {
    return "approved";
  }

  if (raw === "3" || raw === "rejected") {
    return "rejected";
  }

  return "unknown";
}

export function isSellerKycApproved(kycStatus: string | null): boolean {
  return normalizeKycStatus(kycStatus) === "approved";
}

export function formatKycStatusLabel(kycStatus: string | null): string {
  switch (normalizeKycStatus(kycStatus)) {
    case "notSubmitted":
      return "Not submitted";
    case "pendingReview":
      return "Pending review";
    case "approved":
      return "Approved";
    case "rejected":
      return "Rejected";
    default:
      return "Unknown";
  }
}

export function canSubmitKycDocuments(kycStatus: string | null): boolean {
  const status = normalizeKycStatus(kycStatus);
  return status === "notSubmitted" || status === "rejected";
}

export function resolveSellerPostAuthPath(kycStatus: string | null): "/dashboard" | "/verification" {
  return isSellerKycApproved(kycStatus) ? "/dashboard" : "/verification";
}
