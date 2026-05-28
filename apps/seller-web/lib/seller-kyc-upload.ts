import { ApiError, api } from "@/lib/api";
import { getKycStatus } from "@/lib/auth-session";
import { normalizeFileServiceUrl, resolveProductImageContentType } from "@/lib/file-upload";
import { SELLER_KYC_FILE_TYPE } from "@/lib/seller-kyc";

type ApiSuccessResponse<T> = {
  data: T;
};

type PresignedUploadPayload = {
  uploadUrl: string;
  fileKey: string;
  expiresAt: string;
};

export type SellerKycSubmitPayload = {
  nationalIdKey: string;
  proofOfResidenceKey: string;
};

export type SellerVerificationDetails = {
  kycStatus: string;
  rejectionReason: string | null;
};

/**
 * Uploads a KYC image via presigned URL.
 * @param fileType {@link SELLER_KYC_FILE_TYPE.nationalId} or {@link SELLER_KYC_FILE_TYPE.proofOfResidence}
 */
export async function uploadSellerKycDocument(
  file: File,
  fileType: typeof SELLER_KYC_FILE_TYPE.nationalId | typeof SELLER_KYC_FILE_TYPE.proofOfResidence,
): Promise<string> {
  const contentType = resolveProductImageContentType(file);
  if (!contentType) {
    throw new Error("Only JPG, PNG, and WEBP images are supported for KYC documents.");
  }

  if (file.size <= 0) {
    throw new Error("The selected file is empty.");
  }

  const presignedResponse = await api.post<ApiSuccessResponse<PresignedUploadPayload>>(
    "/api/v1/files/presigned-url",
    {
      fileType,
      contentType,
      fileSizeBytes: file.size,
    },
  );

  const presigned = presignedResponse.data;
  const uploadUrl = normalizeFileServiceUrl(presigned.uploadUrl);

  const uploadResponse = await fetch(uploadUrl, {
    method: "PUT",
    headers: { "Content-Type": contentType },
    body: file,
    credentials: "include",
    cache: "no-store",
  });

  if (!uploadResponse.ok) {
    throw new Error(`KYC document upload failed (${uploadResponse.status}).`);
  }

  return presigned.fileKey;
}

/** Registers uploaded national ID and proof of residence keys with the seller KYC API. */
export async function submitSellerKycDocuments(payload: SellerKycSubmitPayload): Promise<void> {
  await api.post<ApiSuccessResponse<unknown>>("/api/v1/auth/kyc/seller", {
    nationalIdKey: payload.nationalIdKey.trim(),
    proofOfResidenceKey: payload.proofOfResidenceKey.trim(),
  });
}

function readVerificationPayload(data: {
  kycStatus?: string | number;
  rejectionReason?: string | null;
}): SellerVerificationDetails {
  const kycRaw = data.kycStatus;
  const kycStatus =
    typeof kycRaw === "string"
      ? kycRaw
      : typeof kycRaw === "number"
        ? String(kycRaw)
        : "";

  return {
    kycStatus,
    rejectionReason: data.rejectionReason?.trim() || null,
  };
}

export async function getSellerVerificationDetails(): Promise<SellerVerificationDetails> {
  try {
    const response = await api.get<
      ApiSuccessResponse<{
        kycStatus?: string | number;
        rejectionReason?: string | null;
      }>
    >("/api/v1/seller/verification");

    return readVerificationPayload(response.data);
  } catch (error) {
    // Older API builds may not expose GET /seller/verification yet; use JWT claim as fallback.
    if (error instanceof ApiError && error.status === 404) {
      return {
        kycStatus: getKycStatus() ?? "",
        rejectionReason: null,
      };
    }

    throw error;
  }
}
