import { api } from '@/lib/api/client';

type ApiEnvelope<T> = {
  data?: T;
};

type PresignedUploadResponse = {
  uploadUrl: string;
  fileKey: string;
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

export type UploadableImage = {
  uri: string;
  contentType: 'image/jpeg' | 'image/png' | 'image/webp';
  fileSizeBytes: number;
};

export const fileUploadService = {
  async getPresignedUploadUrl(params: {
    fileType: number;
    contentType: UploadableImage['contentType'];
    fileSizeBytes: number;
  }): Promise<PresignedUploadResponse> {
    const response = await api.post<ApiEnvelope<PresignedUploadResponse>>('/files/presigned-url', {
      fileType: params.fileType,
      contentType: params.contentType,
      fileSizeBytes: params.fileSizeBytes,
    });

    return readEnvelopeData(response.data);
  },

  async uploadToPresignedUrl(params: {
    uploadUrl: string;
    file: UploadableImage;
  }): Promise<void> {
    const fileResponse = await fetch(params.file.uri);
    const fileBlob = await fileResponse.blob();

    const response = await fetch(params.uploadUrl, {
      method: 'PUT',
      headers: {
        'Content-Type': params.file.contentType,
      },
      body: fileBlob,
    });

    if (!response.ok) {
      throw new Error('Image upload failed. Please retry.');
    }
  },
};

