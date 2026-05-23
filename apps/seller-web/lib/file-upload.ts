import { getAccessToken } from "@/lib/auth-session";

type ApiSuccessResponse<T> = {
  data: T;
};

type ApiErrorPayload = {
  message?: string;
  Message?: string;
};

const allowedContentTypes = new Set(["image/jpeg", "image/png", "image/webp"]);

export type ProductImageUpload = {
  file: File;
  contentType: "image/jpeg" | "image/png" | "image/webp";
};

export function resolveProductImageContentType(file: File): ProductImageUpload["contentType"] | null {
  const type = file.type.toLowerCase();
  if (allowedContentTypes.has(type)) {
    return type as ProductImageUpload["contentType"];
  }

  return null;
}

/**
 * Routes presigned upload URLs through the seller-web proxy (avoids cross-origin PUT to the API).
 */
export function normalizeFileServiceUrl(url: string): string {
  const trimmed = url.trim();

  if (trimmed.startsWith("/api/v1/files/")) {
    return trimmed;
  }

  if (typeof window === "undefined") {
    return trimmed;
  }

  try {
    const parsed = new URL(trimmed, window.location.origin);
    if (parsed.pathname.startsWith("/api/v1/files/")) {
      return `${parsed.pathname}${parsed.search}`;
    }
  } catch {
    return trimmed;
  }

  return trimmed;
}

async function readApiErrorMessage(response: Response): Promise<string> {
  try {
    const payload = (await response.json()) as ApiErrorPayload;
    return payload.message ?? payload.Message ?? `Upload failed (${response.status}).`;
  } catch {
    return `Upload failed (${response.status}).`;
  }
}

/**
 * Uploads via authenticated multipart POST (preferred — works through Next.js proxy).
 */
async function uploadProductImageDirect(file: File, contentType: ProductImageUpload["contentType"]): Promise<string> {
  const token = getAccessToken();
  if (!token) {
    throw new Error("You must be signed in to upload images.");
  }

  const formData = new FormData();
  formData.append("file", file, file.name);

  const response = await fetch("/api/v1/files/product-image", {
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

/**
 * Fallback: presigned URL + PUT (legacy path for mobile clients).
 */
async function uploadProductImagePresigned(
  file: File,
  contentType: ProductImageUpload["contentType"],
): Promise<string> {
  const token = getAccessToken();
  if (!token) {
    throw new Error("You must be signed in to upload images.");
  }

  const presignedResponse = await fetch("/api/v1/files/presigned-url", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      fileType: 1,
      contentType,
      fileSizeBytes: file.size,
    }),
    credentials: "include",
    cache: "no-store",
  });

  if (!presignedResponse.ok) {
    throw new Error(await readApiErrorMessage(presignedResponse));
  }

  const presignedPayload = (await presignedResponse.json()) as ApiSuccessResponse<{
    uploadUrl: string;
    fileKey: string;
  }>;

  const presigned = presignedPayload.data;
  const uploadUrl = normalizeFileServiceUrl(presigned.uploadUrl);

  const uploadResponse = await fetch(uploadUrl, {
    method: "PUT",
    headers: { "Content-Type": contentType },
    body: file,
    credentials: "include",
    cache: "no-store",
  });

  if (!uploadResponse.ok) {
    throw new Error(
      `Image upload to storage failed (${uploadResponse.status}). Check that Storage__Provider=Local is set on the API.`,
    );
  }

  return presigned.fileKey;
}

export async function uploadProductImage(file: File): Promise<string> {
  const contentType = resolveProductImageContentType(file);
  if (!contentType) {
    throw new Error("Only JPG, PNG, and WEBP images are supported.");
  }

  if (file.size <= 0) {
    throw new Error("The selected image file is empty.");
  }

  try {
    return await uploadProductImageDirect(file, contentType);
  } catch (directError) {
    const message = directError instanceof Error ? directError.message : "Direct upload failed.";
    try {
      return await uploadProductImagePresigned(file, contentType);
    } catch {
      throw new Error(message);
    }
  }
}
