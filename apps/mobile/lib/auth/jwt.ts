type JwtPayload = {
  sub?: string;
  email?: string;
  role?: string;
  kycStatus?: string | number;
};

const decodeBase64Url = (segment: string): string | null => {
  try {
    const normalized = segment.replace(/-/g, '+').replace(/_/g, '/');
    const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=');

    if (typeof globalThis.atob !== 'function') {
      return null;
    }

    return globalThis.atob(padded);
  } catch {
    return null;
  }
};

export const decodeJwtPayload = (token: string): JwtPayload | null => {
  const parts = token.split('.');
  if (parts.length < 2) {
    return null;
  }

  const json = decodeBase64Url(parts[1]);
  if (!json) {
    return null;
  }

  try {
    return JSON.parse(json) as JwtPayload;
  } catch {
    return null;
  }
};

export const getClaimsFromAccessToken = (
  accessToken: string
): { userId: string | null; email: string | null; role: string | null; kycStatus: string | null } => {
  const payload = decodeJwtPayload(accessToken);
  if (!payload) {
    return { userId: null, email: null, role: null, kycStatus: null };
  }

  const kycRaw = payload.kycStatus;
  const kycStatus =
    typeof kycRaw === 'string'
      ? kycRaw
      : typeof kycRaw === 'number'
        ? String(kycRaw)
        : null;

  return {
    userId: typeof payload.sub === 'string' ? payload.sub : null,
    email: typeof payload.email === 'string' ? payload.email : null,
    role: typeof payload.role === 'string' ? payload.role : null,
    kycStatus,
  };
};
