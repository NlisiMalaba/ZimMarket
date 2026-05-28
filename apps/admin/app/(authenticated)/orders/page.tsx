"use client";

import { useCallback, useEffect, useMemo, useState } from "react";

import { ApiError, api } from "@/lib/api";
import {
  getCurrencyLabel,
  getOrderStatusLabel,
  getOrderStatusValue,
  getPaymentStatusLabel,
  isOrderStatusName,
  type OrderStatusName,
} from "@/lib/domain-enums";

type PagedList<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};

type AdminOrderListItemDto = {
  orderId: string;
  customerId: string;
  status: number | OrderStatusName;
  paymentStatus: number | string;
  totalAmount: number;
  totalCurrency: string;
  lineItemCount: number;
  createdAt: string;
};

type OrderDetailItemDto = {
  productId: string;
  productTitle: string;
  quantity: number;
  unitPriceUsd: number;
  lineTotalUsd: number;
};

type OrderDetailDto = {
  orderId: string;
  status: number | OrderStatusName;
  paymentStatus: number | string;
  deliveryBatchId?: string | null;
  items: OrderDetailItemDto[];
  totalUsd: number;
};

type SortField = "createdAt" | "totalAmount" | "status" | "paymentStatus";
type SortDirection = "asc" | "desc";

const pageSize = 20;
const orderStatuses = [
  "Pending",
  "Paid",
  "AtWarehouse",
  "QcPassed",
  "Batched",
  "OutForDelivery",
  "Delivered",
  "Cancelled",
  "Refunded",
] as const satisfies readonly OrderStatusName[];

function formatDateTime(value: string): string {
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? value : parsed.toLocaleString();
}

export default function OrdersPage() {
  const [orders, setOrders] = useState<AdminOrderListItemDto[]>([]);
  const [selectedOrderId, setSelectedOrderId] = useState<string | null>(null);
  const [selectedOrderDetail, setSelectedOrderDetail] = useState<OrderDetailDto | null>(null);
  const [isLoadingList, setIsLoadingList] = useState(true);
  const [isLoadingDetail, setIsLoadingDetail] = useState(false);
  const [isMutating, setIsMutating] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  const [statusFilter, setStatusFilter] = useState("");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [customerFilter, setCustomerFilter] = useState("");
  const [sortField, setSortField] = useState<SortField>("createdAt");
  const [sortDirection, setSortDirection] = useState<SortDirection>("desc");

  const [isOverrideModalOpen, setIsOverrideModalOpen] = useState(false);
  const [overrideStatus, setOverrideStatus] = useState<OrderStatusName>("Paid");
  const [overrideReason, setOverrideReason] = useState("");

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  const loadOrders = useCallback(async () => {
    setIsLoadingList(true);

    try {
      const response = await api.get<PagedList<AdminOrderListItemDto>>("/api/v1/admin/orders", {
        query: {
          page: currentPage,
          pageSize,
          status: statusFilter || undefined,
          dateFrom: dateFrom ? new Date(dateFrom).toISOString() : undefined,
          dateTo: dateTo ? new Date(`${dateTo}T23:59:59`).toISOString() : undefined,
        },
      });

      setOrders(response.items);
      setTotalCount(response.totalCount);
      setErrorMessage(null);
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Unable to load orders.");
    } finally {
      setIsLoadingList(false);
    }
  }, [currentPage, dateFrom, dateTo, statusFilter]);

  const loadOrderDetail = useCallback(async (orderId: string) => {
    setIsLoadingDetail(true);
    setSelectedOrderId(orderId);

    try {
      const response = await api.get<OrderDetailDto>(`/api/v1/orders/${orderId}`);
      setSelectedOrderDetail(response);
      setErrorMessage(null);
    } catch (error) {
      setSelectedOrderDetail(null);
      setErrorMessage(error instanceof ApiError ? error.message : "Unable to load order details.");
    } finally {
      setIsLoadingDetail(false);
    }
  }, []);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      void loadOrders();
    }, 0);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [loadOrders]);

  const filteredAndSortedOrders = useMemo(() => {
    const customerQuery = customerFilter.trim().toLowerCase();

    const filtered = orders.filter((order) => {
      if (!customerQuery) {
        return true;
      }

      return (
        order.customerId.toLowerCase().includes(customerQuery) ||
        order.orderId.toLowerCase().includes(customerQuery)
      );
    });

    return [...filtered].sort((a, b) => {
      const direction = sortDirection === "asc" ? 1 : -1;

      if (sortField === "createdAt") {
        return direction * (new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
      }

      if (sortField === "totalAmount") {
        return direction * (a.totalAmount - b.totalAmount);
      }

      if (sortField === "status") {
        return direction * getOrderStatusLabel(a.status).localeCompare(getOrderStatusLabel(b.status));
      }

      return (
        direction *
        getPaymentStatusLabel(a.paymentStatus).localeCompare(getPaymentStatusLabel(b.paymentStatus))
      );
    });
  }, [customerFilter, orders, sortDirection, sortField]);

  const openOverrideModal = () => {
    if (!selectedOrderDetail) {
      return;
    }

    const normalizedStatus = getOrderStatusLabel(selectedOrderDetail.status);
    setOverrideStatus(isOrderStatusName(normalizedStatus) ? normalizedStatus : "Pending");
    setOverrideReason("");
    setIsOverrideModalOpen(true);
  };

  const overrideOrderStatus = async () => {
    if (!selectedOrderDetail) {
      return;
    }

    if (!overrideReason.trim()) {
      setErrorMessage("Please provide a reason for the status override.");
      return;
    }

    setIsMutating(true);

    try {
      await api.patch<null, { newStatus: number; reason: string }>(
        `/api/v1/admin/orders/${selectedOrderDetail.orderId}/status`,
        {
          newStatus: getOrderStatusValue(overrideStatus),
          reason: overrideReason.trim(),
        },
      );

      setIsOverrideModalOpen(false);
      setErrorMessage(null);
      await loadOrders();
      await loadOrderDetail(selectedOrderDetail.orderId);
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to override order status.");
    } finally {
      setIsMutating(false);
    }
  };

  return (
    <section className="space-y-6">
      <header className="rounded-xl border bg-card p-6 shadow-sm">
        <h1 className="text-2xl font-semibold">Order Management</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Filter, review, and manage orders with administrative status overrides.
        </p>
      </header>

      {errorMessage ? (
        <div className="rounded-md border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {errorMessage}
        </div>
      ) : null}

      <section className="rounded-xl border bg-card p-4 shadow-sm">
        <div className="grid gap-3 md:grid-cols-5">
          <select
            value={statusFilter}
            onChange={(event) => {
              setStatusFilter(event.target.value);
              setCurrentPage(1);
            }}
            className="rounded-md border bg-background px-3 py-2 text-sm"
          >
            <option value="">All Statuses</option>
            {orderStatuses.map((status) => (
              <option key={status} value={status}>
                {status}
              </option>
            ))}
          </select>

          <input
            type="date"
            value={dateFrom}
            onChange={(event) => {
              setDateFrom(event.target.value);
              setCurrentPage(1);
            }}
            className="rounded-md border bg-background px-3 py-2 text-sm"
          />
          <input
            type="date"
            value={dateTo}
            onChange={(event) => {
              setDateTo(event.target.value);
              setCurrentPage(1);
            }}
            className="rounded-md border bg-background px-3 py-2 text-sm"
          />
          <input
            value={customerFilter}
            onChange={(event) => setCustomerFilter(event.target.value)}
            placeholder="Filter by customer/order id"
            className="rounded-md border bg-background px-3 py-2 text-sm"
          />
          <div className="grid grid-cols-2 gap-2">
            <select
              value={sortField}
              onChange={(event) => setSortField(event.target.value as SortField)}
              className="rounded-md border bg-background px-3 py-2 text-sm"
            >
              <option value="createdAt">Created</option>
              <option value="totalAmount">Amount</option>
              <option value="status">Status</option>
              <option value="paymentStatus">Payment</option>
            </select>
            <select
              value={sortDirection}
              onChange={(event) => setSortDirection(event.target.value as SortDirection)}
              className="rounded-md border bg-background px-3 py-2 text-sm"
            >
              <option value="desc">Desc</option>
              <option value="asc">Asc</option>
            </select>
          </div>
        </div>
      </section>

      <div className="grid gap-6 xl:grid-cols-[1.25fr_1fr]">
        <section className="rounded-xl border bg-card shadow-sm">
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y">
              <thead>
                <tr className="text-left text-xs text-muted-foreground">
                  <th className="px-4 py-3 font-medium">Order ID</th>
                  <th className="px-4 py-3 font-medium">Customer</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                  <th className="px-4 py-3 font-medium">Payment</th>
                  <th className="px-4 py-3 font-medium">Amount</th>
                  <th className="px-4 py-3 font-medium">Created</th>
                </tr>
              </thead>
              <tbody className="divide-y text-sm">
                {filteredAndSortedOrders.map((order) => (
                  <tr
                    key={order.orderId}
                    className={`cursor-pointer transition-colors hover:bg-muted/50 ${
                      selectedOrderId === order.orderId ? "bg-muted/50" : ""
                    }`}
                    onClick={() => {
                      void loadOrderDetail(order.orderId);
                    }}
                  >
                    <td className="px-4 py-3 font-mono text-xs">{order.orderId.slice(0, 8)}</td>
                    <td className="px-4 py-3 font-mono text-xs">{order.customerId.slice(0, 8)}</td>
                    <td className="px-4 py-3">{getOrderStatusLabel(order.status)}</td>
                    <td className="px-4 py-3">{getPaymentStatusLabel(order.paymentStatus)}</td>
                    <td className="px-4 py-3">
                      {getCurrencyLabel(order.totalCurrency)} {order.totalAmount.toFixed(2)}
                    </td>
                    <td className="px-4 py-3">{formatDateTime(order.createdAt)}</td>
                  </tr>
                ))}
                {!isLoadingList && filteredAndSortedOrders.length === 0 ? (
                  <tr>
                    <td className="px-4 py-6 text-center text-muted-foreground" colSpan={6}>
                      No orders found for the selected filters.
                    </td>
                  </tr>
                ) : null}
                {isLoadingList ? (
                  <tr>
                    <td className="px-4 py-6 text-center text-muted-foreground" colSpan={6}>
                      Loading orders...
                    </td>
                  </tr>
                ) : null}
              </tbody>
            </table>
          </div>
          <div className="flex items-center justify-between border-t px-4 py-3 text-sm">
            <p className="text-muted-foreground">
              Page {currentPage} of {totalPages}
            </p>
            <div className="flex gap-2">
              <button
                type="button"
                className="rounded-md border px-3 py-1.5 disabled:opacity-50"
                disabled={currentPage <= 1 || isLoadingList}
                onClick={() => setCurrentPage((value) => Math.max(1, value - 1))}
              >
                Previous
              </button>
              <button
                type="button"
                className="rounded-md border px-3 py-1.5 disabled:opacity-50"
                disabled={currentPage >= totalPages || isLoadingList}
                onClick={() => setCurrentPage((value) => Math.min(totalPages, value + 1))}
              >
                Next
              </button>
            </div>
          </div>
        </section>

        <aside className="rounded-xl border bg-card p-4 shadow-sm">
          <h2 className="text-sm font-semibold">Order Detail</h2>

          {isLoadingDetail ? (
            <p className="mt-4 text-sm text-muted-foreground">Loading order details...</p>
          ) : null}

          {!isLoadingDetail && !selectedOrderDetail ? (
            <p className="mt-4 text-sm text-muted-foreground">
              Select an order to view items, payment information, delivery batch, and timeline.
            </p>
          ) : null}

          {selectedOrderDetail ? (
            <div className="mt-4 space-y-4 text-sm">
              <div className="space-y-1">
                <p>
                  <span className="font-medium">Order ID:</span>{" "}
                  <span className="font-mono text-xs">{selectedOrderDetail.orderId}</span>
                </p>
                <p>
                  <span className="font-medium">Status:</span> {getOrderStatusLabel(selectedOrderDetail.status)}
                </p>
                <p>
                  <span className="font-medium">Payment:</span>{" "}
                  {getPaymentStatusLabel(selectedOrderDetail.paymentStatus)}
                </p>
                <p>
                  <span className="font-medium">Delivery Batch:</span>{" "}
                  {selectedOrderDetail.deliveryBatchId ? (
                    <span className="font-mono text-xs">{selectedOrderDetail.deliveryBatchId}</span>
                  ) : (
                    "Unassigned"
                  )}
                </p>
                <p>
                  <span className="font-medium">Total (USD):</span> {selectedOrderDetail.totalUsd.toFixed(2)}
                </p>
              </div>

              <div>
                <h3 className="text-xs font-semibold text-muted-foreground">Items</h3>
                <ul className="mt-2 space-y-2">
                  {selectedOrderDetail.items.map((item) => (
                    <li key={item.productId} className="rounded-md border p-2">
                      <p className="font-medium">{item.productTitle}</p>
                      <p className="text-xs text-muted-foreground">
                        Qty {item.quantity} x ${item.unitPriceUsd.toFixed(2)} = ${item.lineTotalUsd.toFixed(2)}
                      </p>
                    </li>
                  ))}
                </ul>
              </div>

              <div>
                <h3 className="text-xs font-semibold text-muted-foreground">Timeline</h3>
                <ol className="mt-2 space-y-2">
                  <li className="rounded-md border p-2">
                    <p className="font-medium">Current Status</p>
                    <p className="text-xs text-muted-foreground">
                      {getOrderStatusLabel(selectedOrderDetail.status)}
                    </p>
                  </li>
                  <li className="rounded-md border p-2">
                    <p className="font-medium">Payment State</p>
                    <p className="text-xs text-muted-foreground">
                      {getPaymentStatusLabel(selectedOrderDetail.paymentStatus)}
                    </p>
                  </li>
                </ol>
              </div>

              <button
                type="button"
                className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground"
                onClick={openOverrideModal}
              >
                Override Status
              </button>
            </div>
          ) : null}
        </aside>
      </div>

      {isOverrideModalOpen ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="w-full max-w-md rounded-xl border bg-card p-4 shadow-lg">
            <h3 className="text-base font-semibold">Confirm Status Override</h3>
            <p className="mt-1 text-sm text-muted-foreground">
              Update order status with an audit reason.
            </p>

            <select
              value={overrideStatus}
              onChange={(event) => setOverrideStatus(event.target.value as OrderStatusName)}
              className="mt-3 w-full rounded-md border bg-background px-3 py-2 text-sm"
            >
              {orderStatuses.map((status) => (
                <option key={status} value={status}>
                  {status}
                </option>
              ))}
            </select>

            <textarea
              value={overrideReason}
              onChange={(event) => setOverrideReason(event.target.value)}
              placeholder="Reason for override..."
              className="mt-3 h-28 w-full rounded-md border bg-background px-3 py-2 text-sm outline-none ring-ring focus:ring-2"
            />

            <div className="mt-4 flex justify-end gap-2">
              <button
                type="button"
                className="rounded-md border px-3 py-2 text-sm"
                onClick={() => setIsOverrideModalOpen(false)}
                disabled={isMutating}
              >
                Cancel
              </button>
              <button
                type="button"
                className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground disabled:opacity-50"
                onClick={() => {
                  void overrideOrderStatus();
                }}
                disabled={isMutating}
              >
                {isMutating ? "Saving..." : "Confirm Override"}
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </section>
  );
}
