import { NextResponse } from "next/server";

import { env } from "@/lib/env";

type LoginRequest = {
  email?: string;
  password?: string;
};

type LoginResponse = {
  accessToken: string;
  refreshToken: string;
  kycStatus: string;
};

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
  const responsePayload = responseText ? JSON.parse(responseText) : null;

  if (!upstreamResponse.ok) {
    return NextResponse.json(
      {
        message:
          responsePayload?.message ??
          responsePayload?.title ??
          "Unable to login. Please check your credentials.",
      },
      { status: upstreamResponse.status },
    );
  }

  const tokens = responsePayload as LoginResponse;

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
