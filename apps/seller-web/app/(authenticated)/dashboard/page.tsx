"use client";

import { useCallback, useEffect, useMemo, useState, useSyncExternalStore } from "react";
import { DollarSign, Eye, Package, Tag } from "lucide-react";

import {
  MetricHighlightCard,
  MonthlyGoalCard,
  OrderMixDonut,
  OverviewAreaChart,
} from "@/components/dashboard/dashboard-widgets";
import { ApiError, api } from "@/lib/api";
import { getUserDisplayName, subscribeToSession } from "@/lib/auth-session";
import {
  formatCurrencyUsd,
  getOrderStatusLabel,
  getPaymentStatusLabel,
} from "@/lib/domain-enums";

type ApiSuccessResponse<T> = {
  data: T;
};

type SellerDashboardStats = {
  totalOrders: number;
  totalRevenueUsd: number;
  activeListings: number;
  lowStockCount: number;
};

type SellerOrder = {
  orderId: string;
  status: number | string;
  paymentStatus: number | string;
  totalUsd: number;
  sellerLineItemCount: number;
  createdAt: string;
};

type PagedList<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};

const refreshIntervalMs = 30_000;
const recentOrdersPageSize = 10;

function greeting(): string {
  const hour = new Date().getHours();
  if (hour < 12) return "Good morning";
  if (hour < 17) return "Good afternoon";
  return "Good evening";
}

function statsSeed(totalOrders: number, totalProducts: number, revenue: number): number {
  return ((totalOrders * 7919) ^ (Math.round(revenue * 100) * 7933) ^ (totalProducts * 7949)) >>> 0;
}

function formatDateTime(value: string): string {
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return value;
  }

  return parsed.toLocaleString();
}

export default function SellerDashboardPage() {
  const displayName = useSyncExternalStore(subscribeToSession, getUserDisplayName, getUserDisplayName);

  const [stats, setStats] = useState<SellerDashboardStats | null>(null);
  const [orders, setOrders] = useState<SellerOrder[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const loadDashboard = useCallback(async () => {
    try {
      const [statsResponse, ordersResponse] = await Promise.all([
        api.get<ApiSuccessResponse<SellerDashboardStats>>("/api/v1/seller/dashboard"),
        api.get<ApiSuccessResponse<PagedList<SellerOrder>>>("/api/v1/orders/seller", {
          query: { page: 1, pageSize: recentOrdersPageSize },
        }),
      ]);

      setStats(statsResponse.data);
      setOrders(ordersResponse.data.items);
      setErrorMessage(null);
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage("Unable to load dashboard data.");
      }
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    let isMounted = true;

    const run = (): void => {
      if (isMounted) {
        void loadDashboard();
      }
    };

    queueMicrotask(run);
    const intervalId = window.setInterval(run, refreshIntervalMs);

    return () => {
      isMounted = false;
      window.clearInterval(intervalId);
    };
  }, [loadDashboard]);

  const revenueUsd = stats?.totalRevenueUsd ?? 0;
  const totalOrders = stats?.totalOrders ?? 0;
  const activeListings = stats?.activeListings ?? 0;
  const lowStockCount = stats?.lowStockCount ?? 0;

  const seed = statsSeed(totalOrders, activeListings, revenueUsd);
  const revenueTarget = revenueUsd > 0 ? Math.round(Math.max(revenueUsd * 1.15, revenueUsd + 500)) : 55_000;

  const orderMixSlices = useMemo(() => {
    const pending = orders.filter((order) => Number(order.status) === 0).length;
    const inProgress = orders.filter((order) => {
      const status = Number(order.status);
      return status >= 1 && status <= 5;
    }).length;
    const delivered = orders.filter((order) => Number(order.status) === 6).length;
    const cancelled = orders.filter((order) => Number(order.status) === 7).length;
    const bump = pending + inProgress + delivered + cancelled === 0 ? 1 : 0;

    return [
      { label: "Pending", value: pending + bump * 0.25, color: "rgb(249 115 22)" },
      { label: "In progress", value: inProgress + bump * 0.25, color: "rgb(20 184 166)" },
      { label: "Delivered", value: delivered + bump * 0.25, color: "rgb(51 65 85)" },
      { label: "Cancelled", value: cancelled + bump * 0.25, color: "rgb(245 158 11)" },
    ];
  }, [orders]);

  return (
    <div className="mx-auto max-w-[1400px] space-y-8">
      <header className="flex flex-col gap-1">
        <h1 className="text-3xl font-semibold tracking-tight text-foreground">Dashboard</h1>
        <p className="max-w-xl text-sm text-muted-foreground">
          {greeting()}, {displayName}. Here&apos;s what&apos;s happening with your store today.
        </p>
      </header>

      {errorMessage ? (
        <div className="rounded-2xl border border-destructive/30 bg-destructive/10 px-5 py-4 text-sm text-destructive shadow-sm">
          {errorMessage}
        </div>
      ) : null}

      <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <MetricHighlightCard
          title="Total revenue"
          value={formatCurrencyUsd(revenueUsd)}
          seed={seed + 11}
          accent="orange"
          icon={DollarSign}
        />
        <MetricHighlightCard
          title="Active listings"
          value={String(activeListings)}
          seed={seed + 17}
          accent="teal"
          icon={Tag}
        />
        <MetricHighlightCard
          title="Total orders"
          value={String(totalOrders)}
          seed={seed + 23}
          accent="slate"
          icon={Package}
        />
        <MetricHighlightCard
          title="Low stock items"
          value={String(lowStockCount)}
          seed={seed + 29}
          accent="amber"
          icon={Eye}
        />
      </section>

      <section className="grid gap-6 lg:grid-cols-12">
        <div className="lg:col-span-8">
          <OverviewAreaChart revenueUsd={revenueUsd} seed={seed + 101} />
        </div>
        <div className="flex flex-col gap-6 lg:col-span-4">
          <OrderMixDonut
            title="Order pipeline"
            subtitle="Where your orders are in fulfilment"
            centerLabel={String(totalOrders)}
            centerCaption="orders"
            slices={orderMixSlices}
          />
          <MonthlyGoalCard
            title="Monthly goals"
            subtitle="Track progress toward your revenue target"
            current={revenueUsd}
            target={revenueTarget}
            formatter={formatCurrencyUsd}
          />
        </div>
      </section>

      <section className="overflow-hidden rounded-2xl border border-border/70 bg-card shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border/70 px-6 py-4">
          <div>
            <h2 className="text-lg font-semibold tracking-tight">Recent orders</h2>
            <p className="text-sm text-muted-foreground">Latest orders assigned to your store</p>
          </div>
          {isLoading ? <span className="text-xs font-medium text-muted-foreground">Updating…</span> : null}
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-border/70">
            <thead>
              <tr className="text-left text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                <th className="px-6 py-3">Order</th>
                <th className="px-6 py-3">Status</th>
                <th className="px-6 py-3">Payment</th>
                <th className="px-6 py-3">Amount</th>
                <th className="px-6 py-3">Items</th>
                <th className="px-6 py-3">Created</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border/60 text-sm">
              {orders.map((order) => (
                <tr key={order.orderId} className="bg-card hover:bg-muted/30">
                  <td className="whitespace-nowrap px-6 py-3 font-mono text-xs">{order.orderId.slice(0, 8)}</td>
                  <td className="whitespace-nowrap px-6 py-3">
                    <span className="rounded-full bg-muted px-2 py-0.5 text-xs font-medium">
                      {getOrderStatusLabel(order.status)}
                    </span>
                  </td>
                  <td className="whitespace-nowrap px-6 py-3 text-muted-foreground">
                    {getPaymentStatusLabel(order.paymentStatus)}
                  </td>
                  <td className="whitespace-nowrap px-6 py-3 font-medium tabular-nums">
                    {formatCurrencyUsd(Number(order.totalUsd))}
                  </td>
                  <td className="whitespace-nowrap px-6 py-3 tabular-nums">{order.sellerLineItemCount}</td>
                  <td className="whitespace-nowrap px-6 py-3 text-muted-foreground">
                    {formatDateTime(order.createdAt)}
                  </td>
                </tr>
              ))}
              {!isLoading && orders.length === 0 ? (
                <tr>
                  <td className="px-6 py-12 text-center text-muted-foreground" colSpan={6}>
                    No orders yet. They will appear here once customers buy your products.
                  </td>
                </tr>
              ) : null}
              {isLoading ? (
                <tr>
                  <td className="px-6 py-12 text-center text-muted-foreground" colSpan={6}>
                    Loading dashboard…
                  </td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
