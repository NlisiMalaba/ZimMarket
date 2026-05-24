import { clearSession, getAccessToken } from "@/lib/auth-session";
import { env } from "@/lib/env";
import { showToast } from "@/lib/toast";

const defaultContentType = "application/json";

export class ApiError extends Error {
  public readonly status: number;
  public readonly code?: string;
  public readonly details?: unknown;

  public constructor(message: string, status: number, code?: string, details?: unknown) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
    this.details = details;
  }
}

type HttpMethod = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

type RequestOptions = {
  headers?: HeadersInit;
  query?: Record<string, string | number | boolean | null | undefined>;
  signal?: AbortSignal;
};

type RequestWithBodyOptions<TBody> = RequestOptions & {
  body?: TBody;
};

type ErrorPayload = {
  message?: string;
  Message?: string;
  code?: string;
  errorCode?: string;
  ErrorCode?: string;
  title?: string;
  Title?: string;
  details?: unknown;
};

function buildHeaders(headers?: HeadersInit): Headers {
  const result = new Headers(headers);
  const token = getAccessToken();

  if (token) {
    result.set("Authorization", `Bearer ${token}`);
  }

  if (!result.has("Content-Type")) {
    result.set("Content-Type", defaultContentType);
  }

  return result;
}

function getApiBaseUrl(): string {
  // Browser calls use same-origin rewrites (see next.config.mjs) to avoid CORS.
  if (typeof window !== "undefined") {
    return "";
  }

  return env.apiUrl;
}

function buildUrl(path: string, query?: RequestOptions["query"]): string {
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  const base = getApiBaseUrl();

  if (!base) {
    const relative = normalizedPath;
    if (!query) {
      return relative;
    }

    const params = new URLSearchParams();
    for (const [key, value] of Object.entries(query)) {
      if (value !== null && value !== undefined) {
        params.set(key, String(value));
      }
    }

    const qs = params.toString();
    return qs ? `${relative}?${qs}` : relative;
  }

  const url = new URL(`${base}${normalizedPath}`);

  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== null && value !== undefined) {
        url.searchParams.set(key, String(value));
      }
    }
  }

  return url.toString();
}

function readApiErrorMessage(payload: unknown, status: number): string {
  if (typeof payload === "string" && payload.trim()) {
    return payload.trim().slice(0, 300);
  }

  if (!payload || typeof payload !== "object") {
    return status === 404
      ? "The requested API endpoint was not found. Restart the API with the latest build."
      : "Unexpected API error.";
  }

  const record = payload as ErrorPayload;
  const message =
    record.message ??
    record.Message ??
    record.title ??
    record.Title;

  if (typeof message === "string" && message.trim()) {
    return message.trim();
  }

  const code = record.errorCode ?? record.ErrorCode ?? record.code;
  if (typeof code === "string" && code.trim()) {
    return code.trim();
  }

  return status === 404
    ? "The requested API endpoint was not found. Restart the API with the latest build."
    : "Unexpected API error.";
}

/** Unwraps `{ data }` / `{ Data }` success envelopes from the .NET API. */
export function unwrapApiData<T>(payload: unknown): T {
  if (!payload || typeof payload !== "object") {
    throw new ApiError("Invalid API response.", 502);
  }

  const record = payload as Record<string, unknown>;
  const data = record.data ?? record.Data;

  if (data === undefined) {
    throw new ApiError("Invalid API response envelope.", 502);
  }

  return data as T;
}

function safeParseJson(text: string): unknown {
  if (!text) {
    return null;
  }

  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

function handleHttpSideEffects(status: number): void {
  if (status === 401 && typeof window !== "undefined") {
    clearSession();
    window.location.assign("/login");
  }

  if (status === 429) {
    showToast({
      message: "Too many requests",
      variant: "warning",
    });
  }
}

async function request<TResponse, TBody = unknown>(
  method: HttpMethod,
  path: string,
  options?: RequestWithBodyOptions<TBody>,
): Promise<TResponse> {
  let response: Response;

  try {
    response = await fetch(buildUrl(path, options?.query), {
      method,
      headers: buildHeaders(options?.headers),
      body: options?.body === undefined ? undefined : JSON.stringify(options.body),
      signal: options?.signal,
      credentials: "include",
      cache: "no-store",
    });
  } catch {
    throw new ApiError(
      "Cannot reach the API. Ensure the backend is running and NEXT_PUBLIC_API_URL is correct.",
      0,
    );
  }

  if (response.status === 204) {
    return undefined as TResponse;
  }

  const text = await response.text();
  const payload = safeParseJson(text);

  if (!response.ok) {
    handleHttpSideEffects(response.status);

    const errorPayload = typeof payload === "object" && payload !== null ? (payload as ErrorPayload) : undefined;
    const errorCode =
      errorPayload?.errorCode ?? errorPayload?.ErrorCode ?? errorPayload?.code;

    throw new ApiError(
      readApiErrorMessage(payload, response.status),
      response.status,
      typeof errorCode === "string" ? errorCode : undefined,
      errorPayload?.details,
    );
  }

  return unwrapApiData<TResponse>(payload);
}

export const api = {
  get<TResponse>(path: string, options?: RequestOptions): Promise<TResponse> {
    return request<TResponse>("GET", path, options);
  },
  post<TResponse, TBody = unknown>(
    path: string,
    body?: TBody,
    options?: RequestOptions,
  ): Promise<TResponse> {
    return request<TResponse, TBody>("POST", path, { ...options, body });
  },
  put<TResponse, TBody = unknown>(
    path: string,
    body?: TBody,
    options?: RequestOptions,
  ): Promise<TResponse> {
    return request<TResponse, TBody>("PUT", path, { ...options, body });
  },
  patch<TResponse, TBody = unknown>(
    path: string,
    body?: TBody,
    options?: RequestOptions,
  ): Promise<TResponse> {
    return request<TResponse, TBody>("PATCH", path, { ...options, body });
  },
  delete<TResponse>(path: string, options?: RequestOptions): Promise<TResponse> {
    return request<TResponse>("DELETE", path, options);
  },
};
