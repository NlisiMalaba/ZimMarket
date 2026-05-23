export function isSellerKycApproved(kycStatus: string | null): boolean {
  return kycStatus === "Approved" || kycStatus === "2";
}
