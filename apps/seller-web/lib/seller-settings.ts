import { api } from "@/lib/api";

type ApiSuccessResponse<T> = {
  data: T;
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

export type UpdateSellerProfilePayload = {
  fullName: string;
  email: string;
  phone: string;
  businessName: string;
  profilePhotoKey: string | null;
  defaultPickupAddress: PickupAddress | null;
  clearDefaultPickupAddress: boolean;
};

export type ChangePasswordPayload = {
  currentPassword: string;
  newPassword: string;
};

export async function getSellerProfile(): Promise<SellerProfile> {
  const response = await api.get<ApiSuccessResponse<SellerProfile>>("/api/v1/seller/profile");
  return response.data;
}

export async function updateSellerProfile(payload: UpdateSellerProfilePayload): Promise<void> {
  await api.put<ApiSuccessResponse<unknown>>("/api/v1/seller/profile", payload);
}

export async function changeSellerPassword(payload: ChangePasswordPayload): Promise<void> {
  await api.post<ApiSuccessResponse<unknown>>("/api/v1/seller/change-password", payload);
}
