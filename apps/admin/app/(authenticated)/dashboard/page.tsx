"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { DollarSign, Package, Truck, UserSearch } from "lucide-react";

import { ApiError, api } from "@/lib/api";
import {
  MetricHighlightCard,
  MonthlyGoalCard,
  OpsMixDonut,
  OverviewAreaChart,
} from "@/components/dashboard/dashboard-widgets";

type ApiSuccessResponse<T> = {
  data: T;
};

type DashboardStats = {
  ordersToday: number;
  revenueTodayUsd: number;
  activeDrivers: number;
  pendingKycCount: number;
  lowStockProducts: number;
};

type RecentOrder = {
  orderId: string;
  customerId: string;
  status: string;
  paymentStatus: string;
  totalAmount: number;
  totalCurrency: string;
  lineItemCount: number;
  createdAt: string;
};

type PagedList<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};

const refreshIntervalMs = 30_000;

function formatCurrencyUsd(amount: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    maximumFractionDigits: 2,
  }).format(amount);
}

function formatDateTime(value: string): string {
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return value;
  }

  return parsed.toLocaleString();
}

function statsSeed(stats: DashboardStats | null): number {
  if (!stats) {
    return 1;
  }
  return (
    ((stats.ordersToday * 7919) ^
      (Math.round(stats.revenueTodayUsd * 100) * 7933) ^
      (stats.activeDrivers * 7949) ^
      (stats.pendingKycCount * 7967)) >>>
    0
  );
}

function greeting(): string {
  const h = new Date().getHours();
  if (h < 12) return "Good morning";
  if (h < 17) return "Good afternoon";
  return "Good evening";
}

export default function DashboardPage() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [recentOrders, setRecentOrders] = useState<RecentOrder[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const loadDashboard = useCallback(async () => {
    try {
      const [statsResponse, ordersResponse] = await Promise.all([
        api.get<ApiSuccessResponse<DashboardStats>>("/api/v1/admin/dashboard"),
        api.get<ApiSuccessResponse<PagedList<RecentOrder>>>("/api/v1/admin/orders", {
          query: {
            page: 1,
            pageSize: 10,
          },
        }),
      ]);

      setStats(statsResponse.data);
      setRecentOrders(ordersResponse.data.items);
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

  const seed = statsSeed(stats);

  const opsSlices = useMemo(() => {
    const o = stats?.ordersToday ?? 0;
    const d = stats?.activeDrivers ?? 0;
    const k = stats?.pendingKycCount ?? 0;
    const l = stats?.lowStockProducts ?? 0;
    const bump = o + d + k + l === 0 ? 1 : 0;
    return [
      { label: "Orders today", value: Math.max(0, o) + bump * 0.25, color: "rgb(249 115 22)" },
      { label: "Active drivers", value: Math.max(0, d) + bump * 0.25, color: "rgb(20 184 166)" },
      { label: "Pending KYC", value: Math.max(0, k) + bump * 0.25, color: "rgb(51 65 85)" },
      { label: "Low stock SKUs", value: Math.max(0, l) + bump * 0.25, color: "rgb(245 158 11)" },
    ];
  }, [stats]);

  const revenueTarget = useMemo(() => {
    const r = stats?.revenueTodayUsd ?? 0;
    return r > 0 ? Math.round(Math.max(r * 1.15, r + 500)) : 55_000;
  }, [stats]);

  return (
    <div className="mx-auto max-w-[1400px] space-y-8">
      <header className="flex flex-col gap-1 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-3xl font-semibold tracking-tight text-foreground">Dashboard</h1>
          <p className="mt-2 max-w-xl text-sm text-muted-foreground">
            {greeting()} — here&apos;s what&apos;s happening across ZimMarket operations. Refreshes every{" "}
            {refreshIntervalMs / 1000}s.
          </p>
        </div>
      </header>

      {errorMessage ? (
        <div className="rounded-2xl border border-destructive/30 bg-destructive/10 px-5 py-4 text-sm text-destructive shadow-sm">
          {errorMessage}
        </div>
      ) : null}

      <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <MetricHighlightCard
          title="Total revenue (today)"
          value={formatCurrencyUsd(stats?.revenueTodayUsd ?? 0)}
          seed={seed + 11}
          accent="orange"
          icon={DollarSign}
        />
        <MetricHighlightCard
          title="Orders today"
          value={String(stats?.ordersToday ?? 0)}
          seed={seed + 17}
          accent="slate"
          icon={Package}
        />
        <MetricHighlightCard
          title="Active drivers"
          value={String(stats?.activeDrivers ?? 0)}
          seed={seed + 23}
          accent="teal"
          icon={Truck}
        />
        <MetricHighlightCard
          title="Pending KYC"
          value={String(stats?.pendingKycCount ?? 0)}
          seed={seed + 29}
          accent="amber"
          icon={UserSearch}
        />
      </section>

      <section className="grid gap-6 lg:grid-cols-12">
        <div className="lg:col-span-8">
          <OverviewAreaChart revenueUsd={stats?.revenueTodayUsd ?? 0} seed={seed + 101} />
        </div>
        <div className="flex flex-col gap-6 lg:col-span-4">
          <OpsMixDonut slices={opsSlices} />
          <MonthlyGoalCard
            title="Daily revenue goal"
            current={stats?.revenueTodayUsd ?? 0}
            target={revenueTarget}
            formatter={formatCurrencyUsd}
          />
        </div>
      </section>

      <section className="overflow-hidden rounded-2xl border border-border/70 bg-card shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border/70 px-6 py-4">
          <div>
            <h2 className="text-lg font-semibold tracking-tight">Recent orders</h2>
            <p className="text-sm text-muted-foreground">Latest 10 records from the API</p>
          </div>
          {isLoading ? (
            <span className="text-xs font-medium text-muted-foreground">Updating…</span>
          ) : null}
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-border/70">
            <thead>
              <tr className="text-left text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                <th className="px-6 py-3">Order</th>
                <th className="px-6 py-3">Customer</th>
                <th className="px-6 py-3">Status</th>
                <th className="px-6 py-3">Payment</th>
                <th className="px-6 py-3">Amount</th>
                <th className="px-6 py-3">Items</th>
                <th className="px-6 py-3">Created</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border/60 text-sm">
              {recentOrders.map((order) => (
                <tr key={order.orderId} className="bg-card hover:bg-muted/30">
                  <td className="whitespace-nowrap px-6 py-3 font-mono text-xs">{order.orderId.slice(0, 8)}</td>
                  <td className="whitespace-nowrap px-6 py-3 font-mono text-xs">{order.customerId.slice(0, 8)}</td>
                  <td className="whitespace-nowrap px-6 py-3">
                    <span className="rounded-full bg-muted px-2 py-0.5 text-xs font-medium">{order.status}</span>
                  </td>
                  <td className="whitespace-nowrap px-6 py-3 text-muted-foreground">{order.paymentStatus}</td>
                  <td className="whitespace-nowrap px-6 py-3 font-medium tabular-nums">
                    {order.totalCurrency} {order.totalAmount.toFixed(2)}
                  </td>
                  <td className="whitespace-nowrap px-6 py-3 tabular-nums">{order.lineItemCount}</td>
                  <td className="whitespace-nowrap px-6 py-3 text-muted-foreground">
                    {formatDateTime(order.createdAt)}
                  </td>
                </tr>
              ))}
              {!isLoading && recentOrders.length === 0 ? (
                <tr>
                  <td className="px-6 py-12 text-center text-muted-foreground" colSpan={7}>
                    No recent orders found.
                  </td>
                </tr>
              ) : null}
              {isLoading ? (
                <tr>
                  <td className="px-6 py-12 text-center text-muted-foreground" colSpan={7}>
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
