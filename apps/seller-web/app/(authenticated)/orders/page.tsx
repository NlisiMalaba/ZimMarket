"use client";

import Link from "next/link";

import { OrdersCatalog } from "@/components/orders/orders-catalog";

export default function SellerOrdersPage() {
  return (
    <div className="mx-auto max-w-[1400px] space-y-6">
      <nav className="text-sm text-muted-foreground">
        <Link href="/dashboard" className="hover:text-foreground">
          Dashboard
        </Link>
        <span className="mx-2">›</span>
        <span className="text-foreground">Orders</span>
      </nav>

      <header>
        <h1 className="text-3xl font-semibold tracking-tight text-foreground">Orders</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Manage and track all customer orders.
        </p>
      </header>

      <OrdersCatalog />
    </div>
  );
}
