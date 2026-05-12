export type UserRoleName = "Customer" | "Seller" | "Driver" | "Admin" | "SuperAdmin";
export type OrderStatusName =
  | "Pending"
  | "Paid"
  | "AtWarehouse"
  | "QcPassed"
  | "Batched"
  | "OutForDelivery"
  | "Delivered"
  | "Cancelled"
  | "Refunded";
export type PaymentStatusName = "Pending" | "Paid" | "Failed" | "Refunded" | "Initiated";
export type WarehouseQcStatusName = "Pending" | "Passed" | "Failed";
export type CurrencyName = "USD" | "ZWG" | "ZAR";

const orderStatusNames: readonly OrderStatusName[] = [
  "Pending",
  "Paid",
  "AtWarehouse",
  "QcPassed",
  "Batched",
  "OutForDelivery",
  "Delivered",
  "Cancelled",
  "Refunded",
];

const userRoleByValue: Record<number, UserRoleName> = {
  0: "Customer",
  1: "Seller",
  2: "Driver",
  3: "Admin",
  4: "SuperAdmin",
};

const orderStatusByValue: Record<number, OrderStatusName> = {
  0: "Pending",
  1: "Paid",
  2: "AtWarehouse",
  3: "QcPassed",
  4: "Batched",
  5: "OutForDelivery",
  6: "Delivered",
  7: "Cancelled",
  8: "Refunded",
};

const paymentStatusByValue: Record<number, PaymentStatusName> = {
  0: "Pending",
  1: "Paid",
  2: "Failed",
  3: "Refunded",
  4: "Initiated",
};

const warehouseQcStatusByValue: Record<number, WarehouseQcStatusName> = {
  0: "Pending",
  1: "Passed",
  2: "Failed",
};

const currencyByValue: Record<number, CurrencyName> = {
  0: "USD",
  1: "ZWG",
  2: "ZAR",
};

function asEnumValue(value: number | string): number | null {
  if (typeof value === "number" && Number.isInteger(value)) {
    return value;
  }

  if (typeof value === "string" && /^\d+$/.test(value)) {
    return Number(value);
  }

  return null;
}

export function getUserRoleLabel(value: number | string): UserRoleName | string {
  if (typeof value === "string" && value in userRoleByValue === false) {
    return value;
  }

  const parsed = asEnumValue(value);
  return parsed !== null && parsed in userRoleByValue ? userRoleByValue[parsed] : String(value);
}

export function getOrderStatusLabel(value: number | string): OrderStatusName | string {
  if (typeof value === "string" && value in orderStatusByValue === false) {
    return value;
  }

  const parsed = asEnumValue(value);
  return parsed !== null && parsed in orderStatusByValue ? orderStatusByValue[parsed] : String(value);
}

export function getPaymentStatusLabel(value: number | string): PaymentStatusName | string {
  if (typeof value === "string" && value in paymentStatusByValue === false) {
    return value;
  }

  const parsed = asEnumValue(value);
  return parsed !== null && parsed in paymentStatusByValue ? paymentStatusByValue[parsed] : String(value);
}

export function getWarehouseQcStatusLabel(value: number | string): WarehouseQcStatusName | string {
  if (typeof value === "string" && value in warehouseQcStatusByValue === false) {
    return value;
  }

  const parsed = asEnumValue(value);
  return parsed !== null && parsed in warehouseQcStatusByValue ? warehouseQcStatusByValue[parsed] : String(value);
}

export function getCurrencyLabel(value: number | string): CurrencyName | string {
  if (typeof value === "string" && value in currencyByValue === false) {
    return value;
  }

  const parsed = asEnumValue(value);
  return parsed !== null && parsed in currencyByValue ? currencyByValue[parsed] : String(value);
}

export function getUserRoleValue(role: UserRoleName): number {
  return {
    Customer: 0,
    Seller: 1,
    Driver: 2,
    Admin: 3,
    SuperAdmin: 4,
  }[role];
}

export function getOrderStatusValue(status: OrderStatusName): number {
  return {
    Pending: 0,
    Paid: 1,
    AtWarehouse: 2,
    QcPassed: 3,
    Batched: 4,
    OutForDelivery: 5,
    Delivered: 6,
    Cancelled: 7,
    Refunded: 8,
  }[status];
}

export function isOrderStatusName(value: string): value is OrderStatusName {
  return orderStatusNames.includes(value as OrderStatusName);
}

export function getWarehouseQcStatusValue(status: WarehouseQcStatusName): number {
  return {
    Pending: 0,
    Passed: 1,
    Failed: 2,
  }[status];
}
