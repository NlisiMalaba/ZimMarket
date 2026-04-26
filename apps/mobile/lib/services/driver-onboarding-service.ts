import { api } from '@/lib/api/client';
import { fileUploadService, type UploadableImage } from '@/lib/services/file-upload-service';
import type { KycStatus } from '@/types/auth';

type ApiEnvelope<T> = {
  data?: T;
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

export const driverOnboardingService = {
  async getPresignedUploadUrl(
    fileType: 4 | 5,
    file: UploadableImage
  ): Promise<{
    uploadUrl: string;
    fileKey: string;
  }> {
    return fileUploadService.getPresignedUploadUrl({
      fileType,
      contentType: file.contentType,
      fileSizeBytes: file.fileSizeBytes,
    });
  },

  async uploadDocument(uploadUrl: string, file: UploadableImage): Promise<void> {
    await fileUploadService.uploadToPresignedUrl({
      uploadUrl,
      file,
    });
  },

  async submitKyc(payload: {
    licenseDocKey: string;
    vehicleDocKey: string;
    licenseNumber: string;
    vehicleRegistration: string;
  }): Promise<void> {
    await api.post('/auth/kyc/driver', payload);
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
