import { api } from '@/lib/api/client';

type ApiEnvelope<T> = {
  data?: T;
};

export type PickupAddress = {
  street: string;
  suburb: string;
  city: string;
  country: string;
};

export type SellerProfile = {
  fullName: string;
  email: string;
  phone: string;
  businessName: string;
  profilePhotoKey: string | null;
  profilePhotoUrl: string | null;
  defaultPickupAddress: PickupAddress | null;
};

export type UpdateSellerProfileInput = {
  fullName: string;
  email: string;
  phone: string;
  businessName: string;
  profilePhotoKey: string | null;
  defaultPickupAddress: PickupAddress | null;
  clearDefaultPickupAddress: boolean;
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

export const sellerSettingsService = {
  async getProfile(): Promise<SellerProfile> {
    const response = await api.get<SellerProfile | ApiEnvelope<SellerProfile>>('/api/v1/seller/profile');
    return readEnvelopeData(response.data);
  },

  async updateProfile(input: UpdateSellerProfileInput): Promise<void> {
    await api.put('/api/v1/seller/profile', input);
  },

  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    await api.post('/api/v1/seller/change-password', { currentPassword, newPassword });
  },

  async uploadProfilePhoto(fileUri: string, fileName: string, mimeType: string): Promise<string> {
    const formData = new FormData();
    formData.append('file', {
      uri: fileUri,
      name: fileName,
      type: mimeType,
    } as unknown as Blob);

    const response = await api.post<string | ApiEnvelope<string>>('/api/v1/files/profile-photo', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });

    return readEnvelopeData(response.data);
  },
};
