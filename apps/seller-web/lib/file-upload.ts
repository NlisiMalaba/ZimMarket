import { api } from "@/lib/api";

type ApiSuccessResponse<T> = {
  data: T;
};

type PresignedUploadResponse = {
  uploadUrl: string;
  fileKey: string;
};

const PRODUCT_IMAGE_FILE_TYPE = 1;

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

export async function uploadProductImage(file: File): Promise<string> {
  const contentType = resolveProductImageContentType(file);
  if (!contentType) {
    throw new Error("Only JPG, PNG, and WEBP images are supported.");
  }

  if (file.size <= 0) {
    throw new Error("The selected image file is empty.");
  }

  const presignedResponse = await api.post<ApiSuccessResponse<PresignedUploadResponse>>(
    "/api/v1/files/presigned-url",
    {
      fileType: PRODUCT_IMAGE_FILE_TYPE,
      contentType,
      fileSizeBytes: file.size,
    },
  );

  const presigned = presignedResponse.data;
  const uploadResponse = await fetch(presigned.uploadUrl, {
    method: "PUT",
    headers: { "Content-Type": contentType },
    body: file,
  });

  if (!uploadResponse.ok) {
    throw new Error("Image upload failed. Please try again.");
  }

  return presigned.fileKey;
}
