import { NextResponse } from "next/server";

import { parseUpstreamAuthPayload, parseUpstreamErrorMessage } from "@/lib/auth-tokens";
import { REFRESH_COOKIE_MAX_AGE_SECONDS, SELLER_REFRESH_COOKIE_NAME } from "@/lib/auth-cookies";
import { env } from "@/lib/env";

type RegisterRequest = {
  email?: string;
  phone?: string;
  password?: string;
  fullName?: string;
  businessName?: string;
};

export async function POST(request: Request): Promise<NextResponse> {
  const body = (await request.json()) as RegisterRequest;

  if (!body.email || !body.phone || !body.password || !body.fullName || !body.businessName) {
    return NextResponse.json(
      { message: "Email, phone, password, full name, and business name are required." },
      { status: 400 },
    );
  }

  const upstreamResponse = await fetch(`${env.apiUrl}/api/v1/auth/register/seller`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    credentials: "include",
    cache: "no-store",
    body: JSON.stringify({
      email: body.email,
      phone: body.phone,
      password: body.password,
      fullName: body.fullName,
      businessName: body.businessName,
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
          "Unable to create your seller account. Please try again.",
        ),
      },
      { status: upstreamResponse.status },
    );
  }

  const tokens = parseUpstreamAuthPayload(responsePayload);
  if (!tokens) {
    return NextResponse.json({ message: "Invalid registration response from server." }, { status: 502 });
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
