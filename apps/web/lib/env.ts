/**
 * Public site URLs. Set these in production so footer links point at real subdomains.
 * Defaults match local dev ports: web 3000, seller 3001, driver 3002.
 */
const defaultSellerSiteUrl = "http://localhost:3001";
const defaultDriverSiteUrl = "http://localhost:3002";

function normalizeBaseUrl(value: string | undefined, fallback: string): string {
  const raw = (value ?? fallback).trim();
  return raw.replace(/\/$/, "");
}

export const env = {
  /** Public storefront origin (this app). Used for canonical links and sharing. */
  customerSiteUrl: (() => {
    const raw = (process.env.NEXT_PUBLIC_CUSTOMER_SITE_URL ?? "").trim();
    return raw ? raw.replace(/\/$/, "") : undefined;
  })(),
  sellerSiteUrl: normalizeBaseUrl(process.env.NEXT_PUBLIC_SELLER_SITE_URL, defaultSellerSiteUrl),
  driverSiteUrl: normalizeBaseUrl(process.env.NEXT_PUBLIC_DRIVER_SITE_URL, defaultDriverSiteUrl),
} as const;
