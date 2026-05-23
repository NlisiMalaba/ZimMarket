type SessionListener = () => void;

let accessToken: string | null = null;
const listeners = new Set<SessionListener>();

function notifyListeners(): void {
  listeners.forEach((listener) => listener());
}

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

    const payloadJson =
      typeof window !== "undefined"
        ? window.atob(encodedPayload)
        : Buffer.from(encodedPayload, "base64").toString("utf-8");

    return JSON.parse(payloadJson) as Record<string, unknown>;
  } catch {
    return null;
  }
}

export function setAccessToken(token: string | null): void {
  accessToken = token;
  notifyListeners();
}

export function getAccessToken(): string | null {
  return accessToken;
}

export function getCurrentUserRole(): "Seller" | "Unknown" {
  if (!accessToken) {
    return "Unknown";
  }

  const payload = decodeJwtPayload(accessToken);
  return payload?.role === "Seller" ? "Seller" : "Unknown";
}

export function getKycStatus(): string | null {
  if (!accessToken) {
    return null;
  }

  const payload = decodeJwtPayload(accessToken);
  const kycStatus = payload?.kycStatus;
  return typeof kycStatus === "string" ? kycStatus : kycStatus != null ? String(kycStatus) : null;
}

export function clearSession(): void {
  accessToken = null;
  notifyListeners();
}

export function subscribeToSession(listener: SessionListener): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}
