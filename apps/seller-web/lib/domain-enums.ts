const ORDER_STATUS_LABELS: Record<number, string> = {
  0: "Pending",
  1: "Paid",
  2: "At warehouse",
  3: "QC passed",
  4: "Batched",
  5: "Out for delivery",
  6: "Delivered",
  7: "Cancelled",
  8: "Refunded",
};

const PAYMENT_STATUS_LABELS: Record<number, string> = {
  0: "Pending",
  1: "Paid",
  2: "Failed",
  3: "Refunded",
};

const PRODUCT_STATUS_LABELS: Record<number, string> = {
  0: "Active",
  1: "Suspended",
  2: "Deleted",
};

export function getOrderStatusLabel(status: number | string): string {
  const key = typeof status === "string" ? Number.parseInt(status, 10) : status;
  return ORDER_STATUS_LABELS[key] ?? String(status);
}

export function getPaymentStatusLabel(status: number | string): string {
  const key = typeof status === "string" ? Number.parseInt(status, 10) : status;
  return PAYMENT_STATUS_LABELS[key] ?? String(status);
}

export function getProductStatusLabel(status: number | string): string {
  const key = typeof status === "string" ? Number.parseInt(status, 10) : status;
  return PRODUCT_STATUS_LABELS[key] ?? String(status);
}

export function formatCurrencyUsd(amount: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    maximumFractionDigits: 2,
  }).format(amount);
}
