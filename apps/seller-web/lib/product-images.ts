import { api } from "@/lib/api";
import { getAccessToken } from "@/lib/auth-session";

type ApiSuccessResponse<T> = {
  data: T;
};

type FileReadUrlDto = {
  key: string;
  url: string;
  expiresAt: string;
};

function readData<T>(response: ApiSuccessResponse<T>): T {
  return response.data;
}

export function isAbortError(error: unknown): boolean {
  return error instanceof DOMException
    ? error.name === "AbortError"
    : error instanceof Error && error.name === "AbortError";
}

/**
 * Routes image requests through the seller-web origin so Next.js can proxy to the API.
 */
export function normalizeProductImageUrl(url: string | null | undefined): string | null {
  if (!url?.trim()) {
    return null;
  }

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

export const productImagesService = {
  /**
   * Loads a product image with the seller JWT (no expiring SAS in the img src).
   */
  async loadImageObjectUrl(imageKey: string, signal?: AbortSignal): Promise<string | null> {
    const token = getAccessToken();
    const trimmedKey = imageKey.trim();

    if (!token || !trimmedKey) {
      return null;
    }

    try {
      const response = await fetch(
        `/api/v1/files/seller-product-image?key=${encodeURIComponent(trimmedKey)}`,
        {
          headers: { Authorization: `Bearer ${token}` },
          signal,
          cache: "no-store",
          credentials: "include",
        },
      );

      if (!response.ok) {
        return null;
      }

      const blob = await response.blob();
      if (blob.size === 0) {
        return null;
      }

      return URL.createObjectURL(blob);
    } catch (error) {
      if (isAbortError(error)) {
        return null;
      }

      throw error;
    }
  },

  async resolveReadUrls(keys: string[]): Promise<Map<string, string>> {
    const uniqueKeys = [...new Set(keys.map((key) => key.trim()).filter(Boolean))];
    if (uniqueKeys.length === 0) {
      return new Map();
    }

    const response = await api.post<ApiSuccessResponse<FileReadUrlDto[]>>(
      "/api/v1/files/resolve-read-urls",
      { keys: uniqueKeys },
    );

    const map = new Map<string, string>();
    for (const item of readData(response)) {
      const normalized = normalizeProductImageUrl(item.url);
      if (normalized) {
        map.set(item.key, normalized);
      }
    }

    return map;
  },

  async resolveReadUrl(key: string): Promise<string | null> {
    const map = await this.resolveReadUrls([key]);
    return map.get(key.trim()) ?? null;
  },
};
