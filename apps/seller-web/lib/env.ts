/**
 * Cross-portal links. Set in production to the customer and driver subdomains.
 * Defaults match local dev: customer 3000, driver 3002.
 */
const defaultCustomerSiteUrl = "http://localhost:3000";
const defaultDriverSiteUrl = "http://localhost:3002";

function normalizeBaseUrl(value: string | undefined, fallback: string): string {
  const raw = (value ?? fallback).trim();
  return raw.replace(/\/$/, "");
}

export const env = {
  customerSiteUrl: normalizeBaseUrl(process.env.NEXT_PUBLIC_CUSTOMER_SITE_URL, defaultCustomerSiteUrl),
  driverSiteUrl: normalizeBaseUrl(process.env.NEXT_PUBLIC_DRIVER_SITE_URL, defaultDriverSiteUrl),
} as const;
