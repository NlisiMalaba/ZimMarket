import { cookies } from "next/headers";
import { NextResponse } from "next/server";

import { parseUpstreamErrorMessage } from "@/lib/auth-tokens";
import { SELLER_REFRESH_COOKIE_NAME } from "@/lib/auth-cookies";
import { env } from "@/lib/env";

export async function POST(request: Request): Promise<NextResponse> {
  const cookieStore = await cookies();
  const refreshToken = cookieStore.get(SELLER_REFRESH_COOKIE_NAME)?.value;

  let accessToken: string | undefined;
  try {
    const body = (await request.json()) as { accessToken?: string };
    accessToken = body.accessToken;
  } catch {
    accessToken = undefined;
  }

  if (refreshToken) {
    const upstreamResponse = await fetch(`${env.apiUrl}/api/v1/auth/logout`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      },
      cache: "no-store",
      body: JSON.stringify({ refreshToken }),
    });

    if (!upstreamResponse.ok) {
      const responseText = await upstreamResponse.text();
      let responsePayload: unknown = null;
      try {
        responsePayload = responseText ? JSON.parse(responseText) : null;
      } catch {
        responsePayload = null;
      }

      return NextResponse.json(
        {
          message: parseUpstreamErrorMessage(responsePayload, "Unable to sign out right now."),
        },
        { status: upstreamResponse.status },
      );
    }
  }

  const response = NextResponse.json({ ok: true });
  response.cookies.set({
    name: SELLER_REFRESH_COOKIE_NAME,
    value: "",
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    maxAge: 0,
  });

  return response;
}
