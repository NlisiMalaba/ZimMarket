import type { KycStatus } from '@/types/auth';

export type NormalizedKycStatus = 'notSubmitted' | 'pendingReview' | 'approved' | 'rejected' | 'unknown';

export const normalizeKycStatus = (value: string | number | null | undefined): NormalizedKycStatus => {
  if (value === null || value === undefined) {
    return 'unknown';
  }

  const raw = String(value).trim().toLowerCase();
  if (raw === '0' || raw === 'notsubmitted') {
    return 'notSubmitted';
  }

  if (raw === '1' || raw === 'pendingreview' || raw === 'pending') {
    return 'pendingReview';
  }

  if (raw === '2' || raw === 'approved') {
    return 'approved';
  }

  if (raw === '3' || raw === 'rejected') {
    return 'rejected';
  }

  return 'unknown';
};

export const isSellerKycApproved = (kycStatus: KycStatus | null | undefined): boolean =>
  normalizeKycStatus(kycStatus) === 'approved';

export const formatKycStatusLabel = (kycStatus: KycStatus | null | undefined): string => {
  switch (normalizeKycStatus(kycStatus)) {
    case 'notSubmitted':
      return 'Not submitted';
    case 'pendingReview':
      return 'Pending review';
    case 'approved':
      return 'Approved';
    case 'rejected':
      return 'Rejected';
    default:
      return 'Unknown';
  }
};

export type SellerOnboardingRoute =
  | '/(seller)'
  | '/(seller)/kyc-upload'
  | '/(seller)/application-submitted';

export const resolveSellerOnboardingRoute = (
  kycStatus: KycStatus | null | undefined
): SellerOnboardingRoute => {
  switch (normalizeKycStatus(kycStatus)) {
    case 'approved':
      return '/(seller)';
    case 'pendingReview':
      return '/(seller)/application-submitted';
    case 'notSubmitted':
    case 'rejected':
    default:
      return '/(seller)/kyc-upload';
  }
};
