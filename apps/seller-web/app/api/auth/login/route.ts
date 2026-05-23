import { NextResponse } from "next/server";

import {
  getRoleFromAccessToken,
  parseUpstreamAuthPayload,
  parseUpstreamErrorMessage,
} from "@/lib/auth-tokens";
import { REFRESH_COOKIE_MAX_AGE_SECONDS, SELLER_REFRESH_COOKIE_NAME } from "@/lib/auth-cookies";
import { env } from "@/lib/env";

type LoginRequest = {
  email?: string;
  password?: string;
};

export async function POST(request: Request): Promise<NextResponse> {
  const body = (await request.json()) as LoginRequest;

  if (!body.email || !body.password) {
    return NextResponse.json({ message: "Email and password are required." }, { status: 400 });
  }

  const upstreamResponse = await fetch(`${env.apiUrl}/api/v1/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    credentials: "include",
    cache: "no-store",
    body: JSON.stringify({
      email: body.email,
      password: body.password,
      deviceInfo: "seller-web",
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
    return NextResponse.json(
      {
        message: parseUpstreamErrorMessage(
          responsePayload,
          "Unable to sign in. Please check your credentials.",
        ),
      },
      { status: upstreamResponse.status },
    );
  }

  const tokens = parseUpstreamAuthPayload(responsePayload);
  if (!tokens) {
    return NextResponse.json({ message: "Invalid sign-in response from server." }, { status: 502 });
  }

  const role = getRoleFromAccessToken(tokens.accessToken);
  if (role !== "Seller") {
    return NextResponse.json(
      { message: "This portal is for seller accounts only. Use the correct app for your role." },
      { status: 403 },
    );
  }

  const response = NextResponse.json({
    accessToken: tokens.accessToken,
    kycStatus: tokens.kycStatus,
  });

  response.cookies.set({
    name: SELLER_REFRESH_COOKIE_NAME,
    value: tokens.refreshToken,
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    maxAge: REFRESH_COOKIE_MAX_AGE_SECONDS,
  });

  return response;
}
