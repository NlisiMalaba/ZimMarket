import { api } from '@/lib/api/client';
import type { KycStatus } from '@/types/auth';

type ApiEnvelope<T> = {
  data?: T;
};

type PresignedUploadPayload = {
  uploadUrl: string;
  fileKey: string;
  expiresAt: string;
};

type PresignedUploadResult = {
  uploadUrl: string;
  fileKey: string;
};

export type SellerKycUploadInput = {
  nationalIdKey: string;
  proofOfResidenceKey: string;
};

const normalizeKycStatus = (value: unknown): KycStatus => {
  if (typeof value !== 'string' || value.trim().length === 0) {
    return 'pending';
  }

  return value.trim();
};

const readEnvelopeData = <T>(responseData: T | ApiEnvelope<T>): T => {
  if (responseData && typeof responseData === 'object' && 'data' in responseData) {
    const value = (responseData as ApiEnvelope<T>).data;
    if (value == null) {
      throw new Error('Server returned an empty response.');
    }

    return value;
  }

  return responseData as T;
};

export const sellerOnboardingService = {
  async getPresignedUploadUrl(
    fileType: 2 | 3,
    contentType: 'image/jpeg' | 'image/png' | 'image/webp',
    fileSizeBytes: number
  ): Promise<PresignedUploadResult> {
    const response = await api.post<ApiEnvelope<PresignedUploadPayload>>('/files/presigned-url', {
      fileType,
      contentType,
      fileSizeBytes,
    });

    const data = readEnvelopeData(response.data);
    return {
      uploadUrl: data.uploadUrl,
      fileKey: data.fileKey,
    };
  },

  async uploadDocument(uploadUrl: string, fileUri: string, contentType: string): Promise<void> {
    const uploadResponse = await fetch(fileUri);
    const fileBlob = await uploadResponse.blob();

    const response = await fetch(uploadUrl, {
      method: 'PUT',
      headers: {
        'Content-Type': contentType,
      },
      body: fileBlob,
    });

    if (!response.ok) {
      throw new Error('Document upload failed. Please retry.');
    }
  },

  async submitKyc(payload: SellerKycUploadInput): Promise<void> {
    await api.post('/auth/kyc/seller', payload);
  },

  async pollKycStatus(accessToken: string, refreshToken: string): Promise<{
    accessToken: string;
    refreshToken: string;
    kycStatus: KycStatus;
  }> {
    const response = await api.post<ApiEnvelope<{ accessToken: string; refreshToken: string; kycStatus?: string }>>(
      '/auth/refresh',
      {
        accessToken,
        refreshToken,
      }
    );

    const data = readEnvelopeData(response.data);
    return {
      accessToken: data.accessToken,
      refreshToken: data.refreshToken,
      kycStatus: normalizeKycStatus(data.kycStatus),
    };
  },
};
