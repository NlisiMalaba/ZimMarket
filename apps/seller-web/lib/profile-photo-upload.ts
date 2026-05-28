import { getAccessToken } from "@/lib/auth-session";
import { resolveProductImageContentType } from "@/lib/file-upload";

type ApiSuccessResponse<T> = {
  data: T;
};

type ApiErrorPayload = {
  message?: string;
  Message?: string;
};

async function readApiErrorMessage(response: Response): Promise<string> {
  try {
    const payload = (await response.json()) as ApiErrorPayload;
    return payload.message ?? payload.Message ?? `Upload failed (${response.status}).`;
  } catch {
    return `Upload failed (${response.status}).`;
  }
}

export async function uploadProfilePhoto(file: File): Promise<string> {
  const contentType = resolveProductImageContentType(file);
  if (!contentType) {
    throw new Error("Only JPG, PNG, and WEBP images are supported.");
  }

  if (file.size <= 0) {
    throw new Error("The selected image file is empty.");
  }

  const token = getAccessToken();
  if (!token) {
    throw new Error("You must be signed in to upload a profile photo.");
  }

  const formData = new FormData();
  formData.append("file", file, file.name);

  const response = await fetch("/api/v1/files/profile-photo", {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
    body: formData,
    credentials: "include",
    cache: "no-store",
  });

  if (!response.ok) {
    throw new Error(await readApiErrorMessage(response));
  }

  const payload = (await response.json()) as ApiSuccessResponse<string>;
  const fileKey = payload.data?.trim();

  if (!fileKey) {
    throw new Error("Upload succeeded but the server did not return a file key.");
  }

  return fileKey;
}
