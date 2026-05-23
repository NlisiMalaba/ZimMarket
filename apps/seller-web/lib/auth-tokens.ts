export type AuthTokens = {
  accessToken: string;
  refreshToken: string;
  kycStatus: string;
};

type ApiSuccessEnvelope<T> = {
  data?: T;
};

type ApiErrorBody = {
  message?: string;
  title?: string;
  validationErrors?: Array<{ field?: string; message?: string }>;
};

function decodeJwtPayload(token: string): Record<string, unknown> | null {
  const tokenParts = token.split(".");
  if (tokenParts.length < 2) {
    return null;
  }

  try {
    const encodedPayload = tokenParts[1]
      .replace(/-/g, "+")
      .replace(/_/g, "/")
      .padEnd(Math.ceil(tokenParts[1].length / 4) * 4, "=");

    const payloadJson = Buffer.from(encodedPayload, "base64").toString("utf-8");
    return JSON.parse(payloadJson) as Record<string, unknown>;
  } catch {
    return null;
  }
}

export function getRoleFromAccessToken(token: string): string | null {
  const payload = decodeJwtPayload(token);
  const role = payload?.role;
  return typeof role === "string" ? role : null;
}

export function parseUpstreamAuthPayload(payload: unknown): AuthTokens | null {
  if (!payload || typeof payload !== "object") {
    return null;
  }

  const envelope = payload as ApiSuccessEnvelope<Record<string, unknown>>;
  const raw = envelope.data;
  if (!raw || typeof raw !== "object") {
    return null;
  }

  const accessToken = raw.accessToken ?? raw.AccessToken;
  const refreshToken = raw.refreshToken ?? raw.RefreshToken;
  const kycValue = raw.kycStatus ?? raw.KycStatus;

  if (typeof accessToken !== "string" || typeof refreshToken !== "string") {
    return null;
  }

  const kycStatus =
    typeof kycValue === "string"
      ? kycValue
      : kycValue !== undefined && kycValue !== null
        ? String(kycValue)
        : "";

  return { accessToken, refreshToken, kycStatus };
}

export function parseUpstreamErrorMessage(payload: unknown, fallback: string): string {
  if (!payload || typeof payload !== "object") {
    return fallback;
  }

  const errorBody = payload as ApiErrorBody;
  if (errorBody.message) {
    return errorBody.message;
  }

  if (errorBody.title) {
    return errorBody.title;
  }

  const validationErrors = errorBody.validationErrors;
  if (Array.isArray(validationErrors) && validationErrors.length > 0) {
    const first = validationErrors[0];
    if (first?.message) {
      return first.message;
    }
  }

  return fallback;
}
