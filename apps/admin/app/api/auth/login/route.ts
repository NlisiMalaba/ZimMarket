import { NextResponse } from "next/server";

import { env } from "@/lib/env";

type LoginRequest = {
  email?: string;
  password?: string;
};

type LoginTokens = {
  accessToken: string;
  refreshToken: string;
  kycStatus: string;
};

type ApiSuccessEnvelope<T> = {
  data?: T;
};

function parseUpstreamLoginPayload(payload: unknown): LoginTokens | null {
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

const REFRESH_COOKIE_NAME = "zm_admin_refresh_token";
const THIRTY_DAYS_IN_SECONDS = 60 * 60 * 24 * 30;

export async function POST(request: Request): Promise<NextResponse> {
  const body = (await request.json()) as LoginRequest;

  if (!body.email || !body.password) {
    return NextResponse.json(
      { message: "Email and password are required." },
      { status: 400 },
    );
  }

  const upstreamResponse = await fetch(`${env.apiUrl}/api/v1/auth/login`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    credentials: "include",
    cache: "no-store",
    body: JSON.stringify({
      email: body.email,
      password: body.password,
      deviceInfo: "admin-web",
    }),
  });

  const responseText = await upstreamResponse.text();
  let responsePayload: unknown = null;
  try {
    responsePayload = responseText ? JSON.parse(responseText) : null;
  } catch {
    responsePayload = null;
  }

  if (!upstreamResponse.ok) {
    const errorBody =
      responsePayload && typeof responsePayload === "object"
        ? (responsePayload as { message?: string; title?: string })
        : undefined;

    return NextResponse.json(
      {
        message:
          errorBody?.message ??
          errorBody?.title ??
          "Unable to login. Please check your credentials.",
      },
      { status: upstreamResponse.status },
    );
  }

  const tokens = parseUpstreamLoginPayload(responsePayload);
  if (!tokens) {
    return NextResponse.json(
      { message: "Invalid login response from server." },
      { status: 502 },
    );
  }

  const response = NextResponse.json({
    accessToken: tokens.accessToken,
    kycStatus: tokens.kycStatus,
  });

  response.cookies.set({
    name: REFRESH_COOKIE_NAME,
    value: tokens.refreshToken,
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    maxAge: THIRTY_DAYS_IN_SECONDS,
  });

  return response;
}
