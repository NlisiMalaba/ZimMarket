"use client";

import Image from "next/image";
import Link from "next/link";
import { useCallback, useEffect, useState, useSyncExternalStore } from "react";
import { Pencil, Plus, Trash2 } from "lucide-react";

import { KycGate } from "@/components/products/kyc-gate";
import { ApiError } from "@/lib/api";
import { getKycStatus, subscribeToSession } from "@/lib/auth-session";
import { formatCurrencyUsd, getProductStatusLabel } from "@/lib/domain-enums";
import { isSellerKycApproved } from "@/lib/seller-kyc";
import {
  daysUntilPermanentDeletion,
  sellerProductsService,
  type SellerProductListScope,
  type SellerProductSummary,
} from "@/lib/seller-products";
import { cn } from "@/lib/utils";

const pageSize = 20;

export default function SellerProductsPage() {
  const kycStatus = useSyncExternalStore(subscribeToSession, getKycStatus, getKycStatus);
  const kycApproved = isSellerKycApproved(kycStatus);

  const [scope, setScope] = useState<SellerProductListScope>("active");
  const [products, setProducts] = useState<SellerProductSummary[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const loadProducts = useCallback(async () => {
    try {
      const response = await sellerProductsService.listProducts({ page, pageSize, scope });
      setProducts(response.items);
      setTotalCount(response.totalCount);
      setErrorMessage(null);
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage("Unable to load products.");
      }
    } finally {
      setIsLoading(false);
    }
  }, [page, scope]);

  useEffect(() => {
    setIsLoading(true);
    void loadProducts();
  }, [loadProducts]);

  const onDelete = async (productId: string) => {
    const confirmed = window.confirm(
      "Delete this product? It will be hidden from your store and images will be removed. The listing record is permanently deleted after 30 days.",
    );

    if (!confirmed) {
      return;
    }

    setDeletingId(productId);
    try {
      await sellerProductsService.deleteProduct(productId);
      await loadProducts();
    } catch (error) {
      window.alert(error instanceof Error ? error.message : "Unable to delete product.");
    } finally {
      setDeletingId(null);
    }
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return (
    <div className="mx-auto max-w-[1400px] space-y-8">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-3xl font-semibold tracking-tight text-foreground">Products</h1>
          <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
            Manage your listings, update stock, and remove products. Deleted listings are permanently
            removed after 30 days.
          </p>
        </div>
        {kycApproved ? (
          <Link
            href="/products/new"
            className="inline-flex h-10 items-center gap-2 rounded-xl bg-foreground px-4 text-sm font-medium text-background hover:opacity-90"
          >
            <Plus className="size-4" />
            Add product
          </Link>
        ) : null}
      </header>

      {!kycApproved ? <KycGate>{null}</KycGate> : null}

      <div className="flex flex-wrap gap-2">
        {(["active", "deleted"] as const).map((tab) => (
          <button
            key={tab}
            type="button"
            onClick={() => {
              setScope(tab);
              setPage(1);
            }}
            className={cn(
              "rounded-xl px-4 py-2 text-sm font-medium transition-colors",
              scope === tab
                ? "bg-foreground text-background"
                : "bg-muted text-muted-foreground hover:text-foreground",
            )}
          >
            {tab === "active" ? "Active" : "Deleted"}
          </button>
        ))}
      </div>

      {errorMessage ? (
        <div className="rounded-2xl border border-destructive/30 bg-destructive/10 px-5 py-4 text-sm text-destructive">
          {errorMessage}
        </div>
      ) : null}

      <section className="overflow-hidden rounded-2xl border border-border/70 bg-card shadow-sm">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-border/70">
            <thead>
              <tr className="text-left text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                <th className="px-6 py-3">Product</th>
                <th className="px-6 py-3">Status</th>
                <th className="px-6 py-3">Price</th>
                <th className="px-6 py-3">Stock</th>
                <th className="px-6 py-3">Category</th>
                {scope === "deleted" ? <th className="px-6 py-3">Purge in</th> : null}
                <th className="px-6 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border/60 text-sm">
              {products.map((product) => (
                <tr key={product.productId} className="hover:bg-muted/30">
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-3">
                      <div className="relative size-12 overflow-hidden rounded-lg bg-muted">
                        {product.primaryImageUrl ? (
                          <Image
                            src={product.primaryImageUrl}
                            alt=""
                            fill
                            className="object-cover"
                            unoptimized
                          />
                        ) : null}
                      </div>
                      <div>
                        <p className="font-medium text-foreground">{product.title}</p>
                        <p className="text-xs text-muted-foreground">
                          {product.productId.slice(0, 8)}
                        </p>
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <span className="rounded-full bg-muted px-2 py-0.5 text-xs font-medium">
                      {getProductStatusLabel(product.status)}
                    </span>
                  </td>
                  <td className="px-6 py-4 tabular-nums">{formatCurrencyUsd(product.priceAmount)}</td>
                  <td className="px-6 py-4 tabular-nums">{product.stockQuantity}</td>
                  <td className="px-6 py-4 text-muted-foreground">{product.categoryName}</td>
                  {scope === "deleted" ? (
                    <td className="px-6 py-4 text-muted-foreground">
                      {daysUntilPermanentDeletion(product.updatedAt)} days
                    </td>
                  ) : null}
                  <td className="px-6 py-4">
                    <div className="flex justify-end gap-2">
                      {scope === "active" && kycApproved ? (
                        <>
                          <Link
                            href={`/products/${product.productId}/edit`}
                            className="inline-flex size-9 items-center justify-center rounded-lg border border-border/80 hover:bg-muted/60"
                            aria-label="Edit product"
                          >
                            <Pencil className="size-4" />
                          </Link>
                          <button
                            type="button"
                            onClick={() => void onDelete(product.productId)}
                            disabled={deletingId === product.productId}
                            className="inline-flex size-9 items-center justify-center rounded-lg border border-destructive/30 text-destructive hover:bg-destructive/10 disabled:opacity-50"
                            aria-label="Delete product"
                          >
                            <Trash2 className="size-4" />
                          </button>
                        </>
                      ) : null}
                    </div>
                  </td>
                </tr>
              ))}
              {!isLoading && products.length === 0 ? (
                <tr>
                  <td
                    className="px-6 py-12 text-center text-muted-foreground"
                    colSpan={scope === "deleted" ? 7 : 6}
                  >
                    {scope === "active"
                      ? "No products yet. Add your first listing to start selling."
                      : "No deleted products."}
                  </td>
                </tr>
              ) : null}
              {isLoading ? (
                <tr>
                  <td
                    className="px-6 py-12 text-center text-muted-foreground"
                    colSpan={scope === "deleted" ? 7 : 6}
                  >
                    Loading products…
                  </td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>

        {totalPages > 1 ? (
          <div className="flex items-center justify-between border-t border-border/70 px-6 py-4 text-sm">
            <p className="text-muted-foreground">
              Page {page} of {totalPages} ({totalCount} total)
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
    </div>
  );
}
