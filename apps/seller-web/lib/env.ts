/**
 * Cross-portal links. Set in production to the customer and driver subdomains.
 * Defaults match local dev: customer 3000, driver 3002.
 */
const defaultCustomerSiteUrl = "http://localhost:3000";
const defaultDriverSiteUrl = "http://localhost:3002";
const defaultApiUrl = "http://localhost:8080";

function normalizeBaseUrl(value: string | undefined, fallback: string): string {
  const raw = (value ?? fallback).trim();
  return raw.replace(/\/$/, "");
}

function getApiUrl(): string {
  const value = process.env.NEXT_PUBLIC_API_URL?.trim();
  return normalizeBaseUrl(value, defaultApiUrl);
}

export const env = {
  apiUrl: getApiUrl(),
  customerSiteUrl: normalizeBaseUrl(process.env.NEXT_PUBLIC_CUSTOMER_SITE_URL, defaultCustomerSiteUrl),
  driverSiteUrl: normalizeBaseUrl(process.env.NEXT_PUBLIC_DRIVER_SITE_URL, defaultDriverSiteUrl),
} as const;
