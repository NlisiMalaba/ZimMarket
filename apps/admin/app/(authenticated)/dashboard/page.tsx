"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState, useSyncExternalStore } from "react";
import { DollarSign, Package, Truck, UserSearch, Users } from "lucide-react";

import { ApiError, api } from "@/lib/api";
import { getCurrentUserRole, subscribeToSession } from "@/lib/auth-session";
import { getCurrencyLabel, getOrderStatusLabel, getPaymentStatusLabel } from "@/lib/domain-enums";
import {
  MetricHighlightCard,
  MonthlyGoalCard,
  OpsMixDonut,
  OverviewAreaChart,
} from "@/components/dashboard/dashboard-widgets";

type OperationalStats = {
  ordersToday: number;
  pendingSellers: number;
  pendingDrivers: number;
  activeDrivers: number;
  lowStockProducts: number;
};

/** Supports current API shape plus legacy combined KYC / revenue fields. */
type OperationalStatsResponse = OperationalStats & {
  pendingKycCount?: number;
  revenueTodayUsd?: number;
};

type FinanceStats = {
  revenueTodayUsd: number;
  revenueMonthUsd: number;
  revenueYearUsd: number;
  revenueAllTimeUsd: number;
};

type RecentOrder = {
  orderId: string;
  customerId: string;
  status: number | string;
  paymentStatus: number | string;
  totalAmount: number;
  totalCurrency: number | string;
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

function statsSeed(stats: OperationalStats | null, finance: FinanceStats | null): number {
  if (!stats && !finance) {
    return 1;
  }
  const mixed =
    ((stats?.ordersToday ?? 0) * 7919) ^
    Math.round((finance?.revenueTodayUsd ?? 0) * 100) * 7933 ^
    (stats?.pendingSellers ?? 0) * 7949 ^
    (stats?.pendingDrivers ?? 0) * 7967;
  return mixed | 0;
}

function greeting(): string {
  const h = new Date().getHours();
  if (h < 12) return "Good morning";
  if (h < 17) return "Good afternoon";
  return "Good evening";
}

function normalizeOperationalStats(raw: OperationalStatsResponse): OperationalStats {
  const pendingSellers = Number(raw.pendingSellers ?? 0);
  const pendingDrivers = Number(raw.pendingDrivers ?? 0);
  const legacyPendingKyc = raw.pendingKycCount;

  return {
    ordersToday: Number(raw.ordersToday ?? 0),
    pendingSellers:
      pendingSellers > 0 || pendingDrivers > 0 || legacyPendingKyc === undefined
        ? pendingSellers
        : Number(legacyPendingKyc),
    pendingDrivers,
    activeDrivers: Number(raw.activeDrivers ?? 0),
    lowStockProducts: Number(raw.lowStockProducts ?? 0),
  };
}

function legacyFinanceFromOperational(raw: OperationalStatsResponse): FinanceStats | null {
  if (raw.revenueTodayUsd === undefined) {
    return null;
  }

  return {
    revenueTodayUsd: Number(raw.revenueTodayUsd),
    revenueMonthUsd: 0,
    revenueYearUsd: 0,
    revenueAllTimeUsd: 0,
  };
}

function PendingApprovalCard({
  title,
  count,
  href,
  seed,
}: {
  title: string;
  count: number;
  href: string;
  seed: number;
}) {
  return (
    <Link
      href={href}
      className="group flex flex-col rounded-2xl border border-border/70 bg-card p-5 shadow-sm transition-colors hover:border-orange-500/40 hover:bg-muted/20"
    >
      <p className="text-sm font-medium text-muted-foreground">{title}</p>
      <p className="mt-2 text-3xl font-semibold tracking-tight text-foreground tabular-nums">{count}</p>
      <p className="mt-2 text-xs font-medium text-orange-600 group-hover:underline dark:text-orange-400">
        Review pending approvals →
      </p>
      <span className="sr-only">Seed {seed}</span>
    </Link>
  );
}

export default function DashboardPage() {
  const role = useSyncExternalStore(subscribeToSession, getCurrentUserRole, getCurrentUserRole);
  const isSuperAdmin = role === "SuperAdmin";

  const [stats, setStats] = useState<OperationalStats | null>(null);
  const [finance, setFinance] = useState<FinanceStats | null>(null);
  const [recentOrders, setRecentOrders] = useState<RecentOrder[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [financeNotice, setFinanceNotice] = useState<string | null>(null);

  const loadDashboard = useCallback(async () => {
    setIsLoading(true);
    const errors: string[] = [];
    let operationalRaw: OperationalStatsResponse | null = null;

    const [statsResult, ordersResult, financeResult] = await Promise.allSettled([
      api.get<OperationalStatsResponse>("/api/v1/admin/dashboard"),
      api.get<PagedList<RecentOrder>>("/api/v1/admin/orders", {
        query: { page: 1, pageSize: 10 },
      }),
      isSuperAdmin ? api.get<FinanceStats>("/api/v1/admin/dashboard/finance") : Promise.resolve(null),
    ]);

    if (statsResult.status === "fulfilled") {
      operationalRaw = statsResult.value;
      setStats(normalizeOperationalStats(statsResult.value));
    } else {
      setStats(null);
      errors.push(
        statsResult.reason instanceof ApiError
          ? statsResult.reason.message
          : "Unable to load operational statistics.",
      );
    }

    if (ordersResult.status === "fulfilled") {
      setRecentOrders(ordersResult.value.items);
    } else {
      setRecentOrders([]);
      errors.push(
        ordersResult.reason instanceof ApiError
          ? ordersResult.reason.message
          : "Unable to load recent orders.",
      );
    }

    if (financeResult.status === "fulfilled") {
      setFinance(financeResult.value);
      setFinanceNotice(null);
    } else if (isSuperAdmin && financeResult.status === "rejected") {
      const financeError =
        financeResult.reason instanceof ApiError ? financeResult.reason : null;
      const legacyFinance = operationalRaw ? legacyFinanceFromOperational(operationalRaw) : null;

      if (legacyFinance) {
        setFinance(legacyFinance);
        setFinanceNotice(
          financeError?.status === 404
            ? "Full revenue breakdown (month/year/all time) requires an API restart with the latest build. Showing today's revenue from the dashboard endpoint."
            : (financeError?.message ??
              "Full revenue breakdown is unavailable. Showing today's revenue only."),
        );
      } else {
        setFinance(null);
        if (financeError?.status !== 404) {
          errors.push(financeError?.message ?? "Unable to load financial statistics.");
        } else {
          setFinanceNotice(
            "Financial statistics are not available until the API is rebuilt and restarted (missing /api/v1/admin/dashboard/finance).",
          );
        }
      }
    } else {
      setFinance(null);
      setFinanceNotice(null);
    }

    setErrorMessage(errors.length > 0 ? errors.join(" ") : null);
    setIsLoading(false);
  }, [isSuperAdmin]);

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

  const seed = statsSeed(stats, finance);

  const opsSlices = useMemo(() => {
    const o = stats?.ordersToday ?? 0;
    const ps = stats?.pendingSellers ?? 0;
    const pd = stats?.pendingDrivers ?? 0;
    const d = stats?.activeDrivers ?? 0;
    const l = stats?.lowStockProducts ?? 0;
    const bump = o + ps + pd + d + l === 0 ? 1 : 0;
    return [
      { label: "Orders today", value: Math.max(0, o) + bump * 0.2, color: "rgb(249 115 22)" },
      { label: "Pending sellers", value: Math.max(0, ps) + bump * 0.2, color: "rgb(51 65 85)" },
      { label: "Pending drivers", value: Math.max(0, pd) + bump * 0.2, color: "rgb(20 184 166)" },
      { label: "Active drivers", value: Math.max(0, d) + bump * 0.2, color: "rgb(14 165 233)" },
      { label: "Low stock SKUs", value: Math.max(0, l) + bump * 0.2, color: "rgb(245 158 11)" },
    ];
  }, [stats]);

  const revenueTarget = useMemo(() => {
    const r = finance?.revenueTodayUsd ?? 0;
    return r > 0 ? Math.round(Math.max(r * 1.15, r + 500)) : 55_000;
  }, [finance]);

  const subtitle = isSuperAdmin
    ? "Operations and financial overview. Refreshes every 30s."
    : "Pending approvals, orders, and live operations. Refreshes every 30s.";

  return (
    <div className="mx-auto max-w-[1400px] space-y-8">
      <header className="flex flex-col gap-1 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-3xl font-semibold tracking-tight text-foreground">Dashboard</h1>
          <p className="mt-2 max-w-xl text-sm text-muted-foreground">
            {greeting()} — {subtitle}
          </p>
        </div>
      </header>

      {errorMessage ? (
        <div className="rounded-2xl border border-destructive/30 bg-destructive/10 px-5 py-4 text-sm text-destructive shadow-sm">
          {errorMessage}
        </div>
      ) : null}

      {financeNotice && !errorMessage ? (
        <div className="rounded-2xl border border-amber-500/30 bg-amber-500/10 px-5 py-4 text-sm text-amber-900 shadow-sm dark:text-amber-200">
          {financeNotice}
        </div>
      ) : null}

      <section>
        <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-muted-foreground">
          Pending approvals
        </h2>
        <div className="grid gap-4 sm:grid-cols-2">
          <PendingApprovalCard
            title="Sellers awaiting approval"
            count={stats?.pendingSellers ?? 0}
            href="/sellers"
            seed={seed + 3}
          />
          <PendingApprovalCard
            title="Drivers awaiting approval"
            count={stats?.pendingDrivers ?? 0}
            href="/drivers"
            seed={seed + 5}
          />
        </div>
      </section>

      <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <MetricHighlightCard
          title="Orders today"
          value={String(stats?.ordersToday ?? 0)}
          seed={seed + 17}
          accent="orange"
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
          title="Low stock SKUs"
          value={String(stats?.lowStockProducts ?? 0)}
          seed={seed + 29}
          accent="amber"
          icon={UserSearch}
        />
        <MetricHighlightCard
          title="Total pending KYC"
          value={String((stats?.pendingSellers ?? 0) + (stats?.pendingDrivers ?? 0))}
          seed={seed + 31}
          accent="slate"
          icon={Users}
        />
      </section>

      {isSuperAdmin ? (
        <>
          <section>
            <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-muted-foreground">
              Revenue (USD, paid orders)
            </h2>
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
              <MetricHighlightCard
                title="Today"
                value={formatCurrencyUsd(finance?.revenueTodayUsd ?? 0)}
                seed={seed + 41}
                accent="orange"
                icon={DollarSign}
              />
              <MetricHighlightCard
                title="This month"
                value={formatCurrencyUsd(finance?.revenueMonthUsd ?? 0)}
                seed={seed + 43}
                accent="slate"
                icon={DollarSign}
              />
              <MetricHighlightCard
                title="This year"
                value={formatCurrencyUsd(finance?.revenueYearUsd ?? 0)}
                seed={seed + 47}
                accent="teal"
                icon={DollarSign}
              />
              <MetricHighlightCard
                title="All time"
                value={formatCurrencyUsd(finance?.revenueAllTimeUsd ?? 0)}
                seed={seed + 53}
                accent="amber"
                icon={DollarSign}
              />
            </div>
          </section>

          <section className="grid gap-6 lg:grid-cols-12">
            <div className="lg:col-span-8">
              <OverviewAreaChart revenueUsd={finance?.revenueTodayUsd ?? 0} seed={seed + 101} />
            </div>
            <div className="flex flex-col gap-6 lg:col-span-4">
              <OpsMixDonut slices={opsSlices} />
              <MonthlyGoalCard
                title="Daily revenue goal"
                current={finance?.revenueTodayUsd ?? 0}
                target={revenueTarget}
                formatter={formatCurrencyUsd}
              />
            </div>
          </section>
        </>
      ) : (
        <section className="max-w-md">
          <OpsMixDonut slices={opsSlices} />
        </section>
      )}

      <section className="overflow-hidden rounded-2xl border border-border/70 bg-card shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border/70 px-6 py-4">
          <div>
            <h2 className="text-lg font-semibold tracking-tight">Recent orders</h2>
            <p className="text-sm text-muted-foreground">
              Latest 10 records —{" "}
              <Link href="/orders" className="font-medium text-orange-600 hover:underline dark:text-orange-400">
                view all orders
              </Link>
            </p>
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
                    <span className="rounded-full bg-muted px-2 py-0.5 text-xs font-medium">
                      {getOrderStatusLabel(order.status)}
                    </span>
                  </td>
                  <td className="whitespace-nowrap px-6 py-3 text-muted-foreground">
                    {getPaymentStatusLabel(order.paymentStatus)}
                  </td>
                  <td className="whitespace-nowrap px-6 py-3 font-medium tabular-nums">
                    {getCurrencyLabel(order.totalCurrency)} {order.totalAmount.toFixed(2)}
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
