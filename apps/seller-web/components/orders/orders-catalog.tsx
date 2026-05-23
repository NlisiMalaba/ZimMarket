"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { createPortal } from "react-dom";
import {
  ArrowDown,
  ArrowUp,
  ArrowUpDown,
  Download,
  Eye,
  MoreHorizontal,
  Search,
  SlidersHorizontal,
  X,
} from "lucide-react";

import { Sparkline } from "@/components/dashboard/dashboard-widgets";
import { OrderStatusBadge } from "@/components/orders/order-status-badge";
import { ApiError } from "@/lib/api";
import {
  formatCurrencyUsd,
  getPaymentStatusLabel,
  getSellerOrderDisplayStatus,
  resolveOrderStatusNumber,
} from "@/lib/domain-enums";
import {
  formatOrderReference,
  getCustomerInitials,
  sellerOrdersService,
  type SellerOrderDetail,
  type SellerOrderStatusGroup,
  type SellerOrderSummary,
} from "@/lib/seller-orders";
import { cn } from "@/lib/utils";

export type OrderStatusTab = "all" | "completed" | "processing" | "pending" | "cancelled";

type SortKey = "order" | "customer" | "product" | "status" | "date" | "amount";
type SortDirection = "asc" | "desc";
type ColumnKey = "customer" | "product" | "status" | "date" | "trend" | "amount";

const pageSize = 20;

const statusTabs: { id: OrderStatusTab; label: string }[] = [
  { id: "all", label: "All" },
  { id: "completed", label: "Completed" },
  { id: "processing", label: "Processing" },
  { id: "pending", label: "Pending" },
  { id: "cancelled", label: "Cancelled" },
];

const defaultVisibleColumns: Record<ColumnKey, boolean> = {
  customer: true,
  product: true,
  status: true,
  date: true,
  trend: true,
  amount: true,
};

function tabToStatusGroup(tab: OrderStatusTab): SellerOrderStatusGroup | undefined {
  switch (tab) {
    case "completed":
      return "Completed";
    case "processing":
      return "Processing";
    case "pending":
      return "Pending";
    case "cancelled":
      return "Cancelled";
    default:
      return undefined;
  }
}

function formatOrderDate(value: string): string {
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return value;
  }

  return parsed.toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
  });
}

function sparklineSeed(order: SellerOrderSummary): number {
  const status = resolveOrderStatusNumber(order.status);
  const amount = Math.round(order.sellerTotalUsd * 100);
  return ((order.orderId.charCodeAt(0) * 7919) ^ (status * 7933) ^ amount) >>> 0;
}

function compareOrders(a: SellerOrderSummary, b: SellerOrderSummary, key: SortKey): number {
  switch (key) {
    case "order":
      return formatOrderReference(a.orderId).localeCompare(formatOrderReference(b.orderId));
    case "customer":
      return a.customerName.localeCompare(b.customerName);
    case "product":
      return a.primaryProductTitle.localeCompare(b.primaryProductTitle);
    case "status":
      return getSellerOrderDisplayStatus(a.status).localeCompare(getSellerOrderDisplayStatus(b.status));
    case "date":
      return new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
    case "amount":
      return a.sellerTotalUsd - b.sellerTotalUsd;
    default:
      return 0;
  }
}

function exportOrdersCsv(orders: SellerOrderSummary[]): void {
  const header = [
    "Order",
    "Customer",
    "Email",
    "Product",
    "Status",
    "Payment",
    "Amount",
    "Items",
    "Date",
  ];

  const rows = orders.map((order) => [
    formatOrderReference(order.orderId),
    order.customerName,
    order.customerEmail,
    order.primaryProductTitle,
    getSellerOrderDisplayStatus(order.status),
    getPaymentStatusLabel(order.paymentStatus),
    String(order.sellerTotalUsd),
    String(order.sellerLineItemCount),
    order.createdAt,
  ]);

  const csv = [header, ...rows]
    .map((row) => row.map((cell) => `"${cell.replaceAll('"', '""')}"`).join(","))
    .join("\n");

  const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = "orders.csv";
  link.click();
  URL.revokeObjectURL(url);
}

export function OrdersCatalog() {
  const [statusTab, setStatusTab] = useState<OrderStatusTab>("all");
  const [searchQuery, setSearchQuery] = useState("");
  const [visibleColumns, setVisibleColumns] = useState(defaultVisibleColumns);
  const [showColumnsMenu, setShowColumnsMenu] = useState(false);
  const [openMenuId, setOpenMenuId] = useState<string | null>(null);
  const [menuPosition, setMenuPosition] = useState<{ top: number; left: number } | null>(null);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [orders, setOrders] = useState<SellerOrderSummary[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [sortKey, setSortKey] = useState<SortKey>("date");
  const [sortDirection, setSortDirection] = useState<SortDirection>("desc");
  const [detailOrderId, setDetailOrderId] = useState<string | null>(null);
  const [detail, setDetail] = useState<SellerOrderDetail | null>(null);
  const [isLoadingDetail, setIsLoadingDetail] = useState(false);
  const [detailError, setDetailError] = useState<string | null>(null);

  const columnsMenuRef = useRef<HTMLDivElement>(null);
  const menuAnchorRef = useRef<HTMLElement | null>(null);

  const closeOrderMenu = useCallback(() => {
    setOpenMenuId(null);
    setMenuPosition(null);
    menuAnchorRef.current = null;
  }, []);

  const toggleOrderMenu = useCallback(
    (orderId: string, anchor: HTMLElement) => {
      if (openMenuId === orderId) {
        closeOrderMenu();
        return;
      }

      const rect = anchor.getBoundingClientRect();
      menuAnchorRef.current = anchor;
      setMenuPosition({
        top: rect.bottom + 4,
        left: Math.max(8, rect.right - 160),
      });
      setOpenMenuId(orderId);
    },
    [closeOrderMenu, openMenuId],
  );

  const loadOrders = useCallback(async () => {
    try {
      const response = await sellerOrdersService.listOrders({
        page,
        pageSize,
        statusGroup: tabToStatusGroup(statusTab),
      });
      setOrders(response.items);
      setTotalCount(response.totalCount);
      setErrorMessage(null);
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage("Unable to load orders.");
      }
    } finally {
      setIsLoading(false);
    }
  }, [page, statusTab]);

  const openOrderDetail = useCallback(
    async (orderId: string) => {
      setDetailOrderId(orderId);
      setDetail(null);
      setDetailError(null);
      setIsLoadingDetail(true);
      closeOrderMenu();

      try {
        const response = await sellerOrdersService.getOrderById(orderId);
        setDetail(response);
      } catch (error) {
        setDetailError(error instanceof ApiError ? error.message : "Unable to load order details.");
      } finally {
        setIsLoadingDetail(false);
      }
    },
    [closeOrderMenu],
  );

  useEffect(() => {
    setIsLoading(true);
    setSelectedIds(new Set());
    closeOrderMenu();
    void loadOrders();
  }, [closeOrderMenu, loadOrders]);

  useEffect(() => {
    function onDocumentClick(event: MouseEvent) {
      if (!columnsMenuRef.current?.contains(event.target as Node)) {
        setShowColumnsMenu(false);
      }

      if (!(event.target as HTMLElement).closest("[data-order-menu]")) {
        closeOrderMenu();
      }
    }

    document.addEventListener("click", onDocumentClick);
    return () => document.removeEventListener("click", onDocumentClick);
  }, [closeOrderMenu]);

  const filteredOrders = useMemo(() => {
    const query = searchQuery.trim().toLowerCase();

    return orders
      .filter((order) => {
        if (!query) {
          return true;
        }

        return (
          formatOrderReference(order.orderId).toLowerCase().includes(query) ||
          order.customerName.toLowerCase().includes(query) ||
          order.customerEmail.toLowerCase().includes(query) ||
          order.primaryProductTitle.toLowerCase().includes(query)
        );
      })
      .sort((a, b) => {
        const direction = sortDirection === "asc" ? 1 : -1;
        return compareOrders(a, b, sortKey) * direction;
      });
  }, [orders, searchQuery, sortDirection, sortKey]);

  const openMenuOrder = useMemo(
    () => filteredOrders.find((order) => order.orderId === openMenuId) ?? null,
    [filteredOrders, openMenuId],
  );

  const allVisibleSelected =
    filteredOrders.length > 0 &&
    filteredOrders.every((order) => selectedIds.has(order.orderId));

  const toggleSort = (key: SortKey) => {
    if (sortKey === key) {
      setSortDirection((current) => (current === "asc" ? "desc" : "asc"));
      return;
    }

    setSortKey(key);
    setSortDirection("asc");
  };

  const SortIcon = ({ column }: { column: SortKey }) => {
    if (sortKey !== column) {
      return <ArrowUpDown className="size-3.5 opacity-40" />;
    }

    return sortDirection === "asc" ? (
      <ArrowUp className="size-3.5" />
    ) : (
      <ArrowDown className="size-3.5" />
    );
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
        <div className="flex flex-wrap gap-2">
          {statusTabs.map((tab) => (
            <button
              key={tab.id}
              type="button"
              onClick={() => {
                setStatusTab(tab.id);
                setPage(1);
              }}
              className={cn(
                "rounded-lg px-4 py-2 text-sm font-medium transition-colors",
                statusTab === tab.id
                  ? "bg-muted text-foreground shadow-sm"
                  : "text-muted-foreground hover:bg-muted/60 hover:text-foreground",
              )}
            >
              {tab.label}
            </button>
          ))}
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <div className="relative min-w-[220px] flex-1 sm:min-w-[280px]">
            <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
            <input
              type="search"
              value={searchQuery}
              onChange={(event) => setSearchQuery(event.target.value)}
              placeholder="Search orders..."
              className="h-10 w-full rounded-lg border border-border/80 bg-background pl-10 pr-3 text-sm outline-none focus:border-foreground/30 focus:ring-2 focus:ring-foreground/5"
            />
          </div>

          <div className="relative" ref={columnsMenuRef}>
            <button
              type="button"
              onClick={() => setShowColumnsMenu((current) => !current)}
              className="inline-flex h-10 items-center gap-2 rounded-lg border border-border/80 bg-background px-3 text-sm font-medium hover:bg-muted/50"
            >
              <SlidersHorizontal className="size-4" />
              Columns
            </button>
            {showColumnsMenu ? (
              <div className="absolute right-0 z-20 mt-2 w-44 rounded-xl border border-border/80 bg-card p-2 shadow-lg">
                {(Object.keys(defaultVisibleColumns) as ColumnKey[]).map((column) => (
                  <label
                    key={column}
                    className="flex cursor-pointer items-center gap-2 rounded-lg px-2 py-2 text-sm hover:bg-muted/60"
                  >
                    <input
                      type="checkbox"
                      checked={visibleColumns[column]}
                      onChange={(event) =>
                        setVisibleColumns((current) => ({
                          ...current,
                          [column]: event.target.checked,
                        }))
                      }
                    />
                    <span className="capitalize">{column}</span>
                  </label>
                ))}
              </div>
            ) : null}
          </div>

          <button
            type="button"
            onClick={() => exportOrdersCsv(filteredOrders)}
            className="inline-flex h-10 items-center gap-2 rounded-lg border border-border/80 bg-background px-3 text-sm font-medium hover:bg-muted/50"
          >
            <Download className="size-4" />
            Export
          </button>
        </div>
      </div>

      {errorMessage ? (
        <div className="rounded-xl border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {errorMessage}
        </div>
      ) : null}

      <section className="overflow-hidden rounded-xl border border-border/80 bg-card">
        <div className="overflow-x-auto">
          <table className="min-w-full">
            <thead className="border-b border-border/80 bg-muted/30">
              <tr className="text-left text-sm font-medium text-muted-foreground">
                <th className="w-12 px-4 py-3">
                  <input
                    type="checkbox"
                    checked={allVisibleSelected}
                    onChange={(event) => {
                      if (event.target.checked) {
                        setSelectedIds(new Set(filteredOrders.map((order) => order.orderId)));
                        return;
                      }

                      setSelectedIds(new Set());
                    }}
                    aria-label="Select all orders"
                  />
                </th>
                <th className="min-w-[120px] px-4 py-3">
                  <button
                    type="button"
                    onClick={() => toggleSort("order")}
                    className="inline-flex items-center gap-1.5 hover:text-foreground"
                  >
                    Order
                    <SortIcon column="order" />
                  </button>
                </th>
                {visibleColumns.customer ? (
                  <th className="min-w-[240px] px-4 py-3">
                    <button
                      type="button"
                      onClick={() => toggleSort("customer")}
                      className="inline-flex items-center gap-1.5 hover:text-foreground"
                    >
                      Customer
                      <SortIcon column="customer" />
                    </button>
                  </th>
                ) : null}
                {visibleColumns.product ? (
                  <th className="min-w-[200px] px-4 py-3">
                    <button
                      type="button"
                      onClick={() => toggleSort("product")}
                      className="inline-flex items-center gap-1.5 hover:text-foreground"
                    >
                      Product
                      <SortIcon column="product" />
                    </button>
                  </th>
                ) : null}
                {visibleColumns.status ? (
                  <th className="px-4 py-3">
                    <button
                      type="button"
                      onClick={() => toggleSort("status")}
                      className="inline-flex items-center gap-1.5 hover:text-foreground"
                    >
                      Status
                      <SortIcon column="status" />
                    </button>
                  </th>
                ) : null}
                {visibleColumns.date ? (
                  <th className="px-4 py-3">
                    <button
                      type="button"
                      onClick={() => toggleSort("date")}
                      className="inline-flex items-center gap-1.5 hover:text-foreground"
                    >
                      Date
                      <SortIcon column="date" />
                    </button>
                  </th>
                ) : null}
                {visibleColumns.trend ? <th className="min-w-[100px] px-4 py-3">Trend</th> : null}
                {visibleColumns.amount ? (
                  <th className="px-4 py-3">
                    <button
                      type="button"
                      onClick={() => toggleSort("amount")}
                      className="inline-flex items-center gap-1.5 hover:text-foreground"
                    >
                      Amount
                      <SortIcon column="amount" />
                    </button>
                  </th>
                ) : null}
                <th className="w-12 px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-border/60 text-sm">
              {filteredOrders.map((order) => {
                const displayStatus = getSellerOrderDisplayStatus(order.status);
                const trendColor =
                  displayStatus === "Cancelled"
                    ? "text-red-500"
                    : displayStatus === "Pending"
                      ? "text-orange-500"
                      : "text-emerald-500";

                return (
                  <tr key={order.orderId} className="hover:bg-muted/20">
                    <td className="px-4 py-4">
                      <input
                        type="checkbox"
                        checked={selectedIds.has(order.orderId)}
                        onChange={(event) => {
                          setSelectedIds((current) => {
                            const next = new Set(current);
                            if (event.target.checked) {
                              next.add(order.orderId);
                            } else {
                              next.delete(order.orderId);
                            }

                            return next;
                          });
                        }}
                        aria-label={`Select ${formatOrderReference(order.orderId)}`}
                      />
                    </td>
                    <td className="px-4 py-4 font-medium text-foreground">
                      {formatOrderReference(order.orderId)}
                    </td>
                    {visibleColumns.customer ? (
                      <td className="px-4 py-4">
                        <div className="flex items-center gap-3">
                          <div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-slate-700 to-slate-900 text-xs font-semibold text-white dark:from-slate-200 dark:to-slate-400 dark:text-slate-900">
                            {getCustomerInitials(order.customerName)}
                          </div>
                          <div className="min-w-0">
                            <p className="truncate font-medium text-foreground">{order.customerName}</p>
                            <p className="truncate text-xs text-muted-foreground">
                              {order.customerEmail || "No email"}
                            </p>
                          </div>
                        </div>
                      </td>
                    ) : null}
                    {visibleColumns.product ? (
                      <td className="px-4 py-4 text-foreground">{order.primaryProductTitle}</td>
                    ) : null}
                    {visibleColumns.status ? (
                      <td className="px-4 py-4">
                        <OrderStatusBadge status={order.status} />
                      </td>
                    ) : null}
                    {visibleColumns.date ? (
                      <td className="px-4 py-4 text-muted-foreground">
                        {formatOrderDate(order.createdAt)}
                      </td>
                    ) : null}
                    {visibleColumns.trend ? (
                      <td className="px-4 py-4">
                        <Sparkline
                          seed={sparklineSeed(order)}
                          colorClass={trendColor}
                          className="h-8 w-24"
                        />
                      </td>
                    ) : null}
                    {visibleColumns.amount ? (
                      <td className="px-4 py-4 tabular-nums font-medium">
                        {formatCurrencyUsd(order.sellerTotalUsd)}
                      </td>
                    ) : null}
                    <td className="relative px-4 py-4" data-order-menu>
                      <button
                        type="button"
                        onClick={(event) => toggleOrderMenu(order.orderId, event.currentTarget)}
                        className="inline-flex size-8 items-center justify-center rounded-lg hover:bg-muted/70"
                        aria-label="Order actions"
                        aria-expanded={openMenuId === order.orderId}
                      >
                        <MoreHorizontal className="size-4" />
                      </button>
                    </td>
                  </tr>
                );
              })}
              {!isLoading && filteredOrders.length === 0 ? (
                <tr>
                  <td colSpan={9} className="px-4 py-16 text-center text-sm text-muted-foreground">
                    No orders match your filters.
                  </td>
                </tr>
              ) : null}
              {isLoading ? (
                <tr>
                  <td colSpan={9} className="px-4 py-16 text-center text-sm text-muted-foreground">
                    Loading orders…
                  </td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>

        {totalPages > 1 ? (
          <div className="flex items-center justify-between border-t border-border/80 px-4 py-3 text-sm">
            <p className="text-muted-foreground">
              Page {page} of {totalPages} · {totalCount} orders
            </p>
            <div className="flex gap-2">
              <button
                type="button"
                disabled={page <= 1 || isLoading}
                onClick={() => setPage((current) => Math.max(1, current - 1))}
                className="rounded-lg border border-border/80 px-3 py-1.5 disabled:opacity-50"
              >
                Previous
              </button>
              <button
                type="button"
                disabled={page >= totalPages || isLoading}
                onClick={() => setPage((current) => current + 1)}
                className="rounded-lg border border-border/80 px-3 py-1.5 disabled:opacity-50"
              >
                Next
              </button>
            </div>
          </div>
        ) : null}
      </section>

      {openMenuOrder && menuPosition
        ? createPortal(
            <div
              data-order-menu
              className="fixed z-50 w-40 rounded-xl border border-border/80 bg-card py-1 shadow-lg"
              style={{ top: menuPosition.top, left: menuPosition.left }}
            >
              <button
                type="button"
                onClick={() => void openOrderDetail(openMenuOrder.orderId)}
                className="flex w-full items-center gap-2 px-3 py-2 text-left text-sm hover:bg-muted/60"
              >
                <Eye className="size-4 text-muted-foreground" />
                View details
              </button>
            </div>,
            document.body,
          )
        : null}

      {detailOrderId ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div
            role="dialog"
            aria-modal="true"
            aria-labelledby="order-detail-title"
            className="max-h-[85vh] w-full max-w-lg overflow-y-auto rounded-2xl border border-border/80 bg-card p-6 shadow-xl"
          >
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  Order details
                </p>
                <h2 id="order-detail-title" className="mt-1 text-xl font-semibold text-foreground">
                  {formatOrderReference(detailOrderId)}
                </h2>
              </div>
              <button
                type="button"
                onClick={() => {
                  setDetailOrderId(null);
                  setDetail(null);
                  setDetailError(null);
                }}
                className="inline-flex size-9 items-center justify-center rounded-lg hover:bg-muted/70"
                aria-label="Close order details"
              >
                <X className="size-4" />
              </button>
            </div>

            {isLoadingDetail ? (
              <p className="mt-6 text-sm text-muted-foreground">Loading order details…</p>
            ) : detailError ? (
              <p className="mt-6 text-sm text-destructive">{detailError}</p>
            ) : detail ? (
              <div className="mt-6 space-y-5">
                <div className="flex flex-wrap gap-2">
                  <OrderStatusBadge status={detail.status} />
                  <span className="rounded-full bg-muted px-2.5 py-1 text-xs font-medium text-muted-foreground">
                    {getPaymentStatusLabel(detail.paymentStatus)}
                  </span>
                </div>

                <div className="grid gap-3 text-sm sm:grid-cols-2">
                  <div>
                    <p className="text-muted-foreground">Delivery city</p>
                    <p className="font-medium text-foreground">{detail.customerCity}</p>
                  </div>
                  <div>
                    <p className="text-muted-foreground">Total</p>
                    <p className="font-medium text-foreground">{formatCurrencyUsd(detail.totalUsd)}</p>
                  </div>
                </div>

                <div>
                  <p className="mb-2 text-sm font-medium text-foreground">Your items</p>
                  <ul className="space-y-2">
                    {detail.items.map((item) => (
                      <li
                        key={item.productId}
                        className="rounded-xl border border-border/70 px-3 py-2 text-sm"
                      >
                        <p className="font-medium text-foreground">{item.productTitle}</p>
                        <p className="text-muted-foreground">
                          {item.quantity} × {formatCurrencyUsd(item.unitPriceUsd)} ={" "}
                          {formatCurrencyUsd(item.lineTotalUsd)}
                        </p>
                      </li>
                    ))}
                  </ul>
                </div>
              </div>
            ) : null}
          </div>
        </div>
      ) : null}
    </div>
  );
}
