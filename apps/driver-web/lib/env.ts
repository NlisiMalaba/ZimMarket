/**
 * Cross-portal links. Set in production to the customer and seller subdomains.
 * Defaults match local dev: customer 3000, seller 3001.
 */
const defaultCustomerSiteUrl = "http://localhost:3000";
const defaultSellerSiteUrl = "http://localhost:3001";

function normalizeBaseUrl(value: string | undefined, fallback: string): string {
  const raw = (value ?? fallback).trim();
  return raw.replace(/\/$/, "");
}

export const env = {
  customerSiteUrl: normalizeBaseUrl(process.env.NEXT_PUBLIC_CUSTOMER_SITE_URL, defaultCustomerSiteUrl),
  sellerSiteUrl: normalizeBaseUrl(process.env.NEXT_PUBLIC_SELLER_SITE_URL, defaultSellerSiteUrl),
} as const;
