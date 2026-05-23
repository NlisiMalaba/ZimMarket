import { clearSession, getAccessToken } from "@/lib/auth-session";
import { env } from "@/lib/env";

const defaultContentType = "application/json";

export class ApiError extends Error {
  public readonly status: number;
  public readonly code?: string;

  public constructor(message: string, status: number, code?: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
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
  errorCode?: string;
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

function buildUrl(path: string, query?: RequestOptions["query"]): string {
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  const url = new URL(`${env.apiUrl}${normalizedPath}`);

  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== null && value !== undefined) {
        url.searchParams.set(key, String(value));
      }
    }
  }

  return url.toString();
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

async function request<TResponse, TBody = unknown>(
  method: HttpMethod,
  path: string,
  options?: RequestWithBodyOptions<TBody>,
): Promise<TResponse> {
  const response = await fetch(buildUrl(path, options?.query), {
    method,
    headers: buildHeaders(options?.headers),
    body: options?.body === undefined ? undefined : JSON.stringify(options.body),
    signal: options?.signal,
    credentials: "include",
    cache: "no-store",
  });

  if (response.status === 204) {
    return undefined as TResponse;
  }

  const text = await response.text();
  const payload = safeParseJson(text);

  if (!response.ok) {
    if (response.status === 401 && typeof window !== "undefined") {
      clearSession();
      window.location.assign("/login");
    }

    const errorPayload = typeof payload === "object" && payload !== null ? (payload as ErrorPayload) : undefined;
    const fallbackMessage = typeof payload === "string" ? payload : "Unexpected API error.";

    throw new ApiError(
      errorPayload?.message ?? fallbackMessage,
      response.status,
      errorPayload?.errorCode,
    );
  }

  return payload as TResponse;
}

export const api = {
  get<TResponse>(path: string, options?: RequestOptions): Promise<TResponse> {
    return request<TResponse>("GET", path, options);
  },
};
