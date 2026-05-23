"use client";

import Link from "next/link";
import { useSyncExternalStore } from "react";
import { Plus } from "lucide-react";

import { ProductsCatalog } from "@/components/products/products-catalog";
import { KycGate } from "@/components/products/kyc-gate";
import { getKycStatus, subscribeToSession } from "@/lib/auth-session";
import { isSellerKycApproved } from "@/lib/seller-kyc";

export default function SellerProductsPage() {
  const kycStatus = useSyncExternalStore(subscribeToSession, getKycStatus, getKycStatus);
  const kycApproved = isSellerKycApproved(kycStatus);

  return (
    <div className="mx-auto max-w-[1400px] space-y-6">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-3xl font-semibold tracking-tight text-foreground">Products</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Browse and manage your product catalog.
          </p>
        </div>
        {kycApproved ? (
          <Link
            href="/products/new"
            className="inline-flex h-10 items-center gap-2 rounded-lg bg-foreground px-4 text-sm font-medium text-background hover:opacity-90"
          >
            <Plus className="size-4" />
            Add Product
          </Link>
        ) : null}
      </header>

      {!kycApproved ? <KycGate>{null}</KycGate> : null}

      <ProductsCatalog kycApproved={kycApproved} />
    </div>
  );
}
