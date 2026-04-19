"use client";

import { useCallback, useEffect, useMemo, useState } from "react";

import { ApiError, api } from "@/lib/api";

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

    const runLoad = async () => {
      await loadDashboard();
    };

    void runLoad();

    const intervalId = window.setInterval(() => {
      if (isMounted) {
        void loadDashboard();
      }
    }, refreshIntervalMs);

    return () => {
      isMounted = false;
      window.clearInterval(intervalId);
    };
  }, [loadDashboard]);

  const statCards = useMemo(
    () => [
      { label: "Orders Today", value: stats?.ordersToday ?? 0 },
      { label: "Revenue (USD)", value: formatCurrencyUsd(stats?.revenueTodayUsd ?? 0) },
      { label: "Active Drivers", value: stats?.activeDrivers ?? 0 },
      { label: "Pending KYC", value: stats?.pendingKycCount ?? 0 },
      { label: "Low Stock", value: stats?.lowStockProducts ?? 0 },
    ],
    [stats],
  );

  return (
    <section className="space-y-6">
      <header className="rounded-xl border bg-card p-6 shadow-sm">
        <h1 className="text-2xl font-semibold">Dashboard</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Operational overview refreshed every 30 seconds.
        </p>
      </header>

      {errorMessage ? (
        <div className="rounded-md border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {errorMessage}
        </div>
      ) : null}

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
        {statCards.map((card) => (
          <article key={card.label} className="rounded-xl border bg-card p-4 shadow-sm">
            <p className="text-xs text-muted-foreground">{card.label}</p>
            <p className="mt-2 text-2xl font-semibold">{card.value}</p>
          </article>
        ))}
      </div>

      <section className="rounded-xl border bg-card shadow-sm">
        <div className="border-b px-4 py-3">
          <h2 className="text-sm font-semibold">Recent Orders (Last 10)</h2>
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y">
            <thead>
              <tr className="text-left text-xs text-muted-foreground">
                <th className="px-4 py-3 font-medium">Order</th>
                <th className="px-4 py-3 font-medium">Customer</th>
                <th className="px-4 py-3 font-medium">Status</th>
                <th className="px-4 py-3 font-medium">Payment</th>
                <th className="px-4 py-3 font-medium">Amount</th>
                <th className="px-4 py-3 font-medium">Items</th>
                <th className="px-4 py-3 font-medium">Created</th>
              </tr>
            </thead>
            <tbody className="divide-y text-sm">
              {recentOrders.map((order) => (
                <tr key={order.orderId}>
                  <td className="px-4 py-3 font-mono text-xs">{order.orderId.slice(0, 8)}</td>
                  <td className="px-4 py-3 font-mono text-xs">{order.customerId.slice(0, 8)}</td>
                  <td className="px-4 py-3">{order.status}</td>
                  <td className="px-4 py-3">{order.paymentStatus}</td>
                  <td className="px-4 py-3">
                    {order.totalCurrency} {order.totalAmount.toFixed(2)}
                  </td>
                  <td className="px-4 py-3">{order.lineItemCount}</td>
                  <td className="px-4 py-3">{formatDateTime(order.createdAt)}</td>
                </tr>
              ))}
              {!isLoading && recentOrders.length === 0 ? (
                <tr>
                  <td className="px-4 py-6 text-center text-muted-foreground" colSpan={7}>
                    No recent orders found.
                  </td>
                </tr>
              ) : null}
              {isLoading ? (
                <tr>
                  <td className="px-4 py-6 text-center text-muted-foreground" colSpan={7}>
                    Loading dashboard...
                  </td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>
      </section>
    </section>
  );
}
