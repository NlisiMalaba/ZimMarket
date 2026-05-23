"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { use, useEffect, useState, useSyncExternalStore } from "react";
import {
  Calendar,
  ChevronRight,
  DollarSign,
  Layers,
  Package,
  Pencil,
  Trash2,
} from "lucide-react";

import { DeleteProductDialog } from "@/components/products/delete-product-dialog";
import { ProductImage } from "@/components/products/product-image";
import { ProductStatusBadge } from "@/components/products/product-status-badge";
import { getKycStatus, subscribeToSession } from "@/lib/auth-session";
import { formatCurrencyUsd } from "@/lib/domain-enums";
import { isSellerKycApproved } from "@/lib/seller-kyc";
import {
  DELETED_PRODUCT_RETENTION_DAYS,
  daysUntilPermanentDeletion,
  sellerProductsService,
  type SellerProductDetail,
} from "@/lib/seller-products";

type ViewProductPageProps = {
  params: Promise<{ productId: string }>;
};

function formatDate(value: string): string {
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

export default function ViewProductPage({ params }: ViewProductPageProps) {
  const router = useRouter();
  const { productId } = use(params);
  const kycStatus = useSyncExternalStore(subscribeToSession, getKycStatus, getKycStatus);
  const kycApproved = isSellerKycApproved(kycStatus);

  const [product, setProduct] = useState<SellerProductDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isDeleting, setIsDeleting] = useState(false);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    const load = async () => {
      try {
        const loadedProduct = await sellerProductsService.getProduct(productId);
        if (isMounted) {
          setProduct(loadedProduct);
          setErrorMessage(null);
        }
      } catch (error) {
        if (isMounted) {
          setErrorMessage(error instanceof Error ? error.message : "Unable to load product.");
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    };

    void load();

    return () => {
      isMounted = false;
    };
  }, [productId]);

  const isDeleted = Number(product?.status) === 2;
  const primaryImageKey = product?.imageKeys[0] ?? null;
  const canManage = !isDeleted && kycApproved;

  const handleDeleteConfirm = async () => {
    if (!product) {
      return;
    }

    setIsDeleting(true);
    setDeleteError(null);

    try {
      await sellerProductsService.deleteProduct(product.productId);
      router.push("/products");
    } catch (error) {
      setDeleteError(error instanceof Error ? error.message : "Unable to delete product.");
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <div className="mx-auto max-w-[1100px] space-y-6">
      <nav className="flex flex-wrap items-center gap-1.5 text-sm text-muted-foreground">
        <Link href="/dashboard" className="hover:text-foreground">
          Dashboard
        </Link>
        <ChevronRight className="size-3.5 shrink-0" />
        <Link href="/products" className="hover:text-foreground">
          Products
        </Link>
        {product ? (
          <>
            <ChevronRight className="size-3.5 shrink-0" />
            <span className="truncate text-foreground">{product.title}</span>
          </>
        ) : null}
      </nav>

      <div className="flex flex-wrap items-center justify-between gap-4">
        <h1 className="text-3xl font-semibold tracking-tight text-foreground">
          {product?.title ?? "Product details"}
        </h1>
        {canManage ? (
          <div className="flex items-center gap-2">
            <Link
              href={`/products/${productId}/edit`}
              className="inline-flex h-10 items-center gap-2 rounded-lg border border-border/80 bg-background px-4 text-sm font-medium hover:bg-muted/60"
            >
              <Pencil className="size-4" />
              Edit
            </Link>
            <button
              type="button"
              onClick={() => {
                setDeleteError(null);
                setShowDeleteDialog(true);
              }}
              disabled={isDeleting}
              className="inline-flex h-10 items-center gap-2 rounded-lg bg-destructive px-4 text-sm font-medium text-destructive-foreground hover:bg-destructive/90 disabled:opacity-50"
            >
              <Trash2 className="size-4" />
              Delete
            </button>
          </div>
        ) : null}
      </div>

      {errorMessage ? (
        <p className="rounded-xl border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {errorMessage}
        </p>
      ) : null}

      {isLoading || !product ? (
        <p className="text-sm text-muted-foreground">Loading product…</p>
      ) : (
        <div className="grid gap-6 lg:grid-cols-12">
          <section className="rounded-xl border border-border/80 bg-card p-6 lg:col-span-8">
            <h2 className="text-lg font-semibold text-foreground">Product Details</h2>

            {isDeleted ? (
              <p className="mt-4 rounded-lg bg-muted px-3 py-2 text-sm text-muted-foreground">
                Archived — permanently removed in {daysUntilPermanentDeletion(product.updatedAt)} of{" "}
                {DELETED_PRODUCT_RETENTION_DAYS} days.
              </p>
            ) : null}

            <div className="mt-6 space-y-6">
              <div>
                <p className="text-sm text-muted-foreground">Name</p>
                <p className="mt-1 font-medium text-foreground">{product.title}</p>
              </div>

              <div>
                <p className="text-sm text-muted-foreground">Description</p>
                <p className="mt-1 whitespace-pre-wrap text-sm leading-relaxed text-foreground">
                  {product.description}
                </p>
              </div>

              <div className="grid gap-6 border-t border-border/80 pt-6 sm:grid-cols-3">
                <div>
                  <p className="text-sm text-muted-foreground">Status</p>
                  <ProductStatusBadge status={product.status} className="mt-2" />
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">Category</p>
                  <span className="mt-2 inline-flex rounded-full bg-muted px-3 py-1 text-sm font-medium text-foreground">
                    {product.categoryName}
                  </span>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">Created</p>
                  <p className="mt-2 inline-flex items-center gap-1.5 text-sm font-medium text-foreground">
                    <Calendar className="size-4 text-muted-foreground" />
                    {formatDate(product.createdAt)}
                  </p>
                </div>
              </div>
            </div>
          </section>

          <div className="space-y-6 lg:col-span-4">
            <section className="rounded-xl border border-border/80 bg-card p-6">
              <h2 className="text-lg font-semibold text-foreground">Image</h2>
              <div className="relative mt-4 flex aspect-square items-center justify-center overflow-hidden rounded-xl border border-border/80 bg-muted/40">
                <ProductImage
                  imageKey={primaryImageKey}
                  alt={product.title}
                  iconClassName="size-16"
                />
              </div>
            </section>

            <section className="rounded-xl border border-border/80 bg-card p-6">
              <h2 className="text-lg font-semibold text-foreground">Pricing &amp; Inventory</h2>
              <div className="mt-4 divide-y divide-border/80">
                <div className="flex items-center justify-between py-4 first:pt-0">
                  <div className="flex items-center gap-3 text-sm text-muted-foreground">
                    <DollarSign className="size-4" />
                    Price
                  </div>
                  <p className="text-lg font-semibold tabular-nums text-foreground">
                    {formatCurrencyUsd(product.priceAmount)}
                  </p>
                </div>
                <div className="flex items-center justify-between py-4 last:pb-0">
                  <div className="flex items-center gap-3 text-sm text-muted-foreground">
                    <Layers className="size-4" />
                    Stock
                  </div>
                  <p className="text-lg font-semibold tabular-nums text-foreground">
                    {product.stockQuantity}
                  </p>
                </div>
              </div>
            </section>
          </div>
        </div>
      )}

      <DeleteProductDialog
        open={showDeleteDialog}
        productTitle={product?.title}
        isDeleting={isDeleting}
        errorMessage={deleteError}
        onConfirm={() => void handleDeleteConfirm()}
        onCancel={() => {
          if (!isDeleting) {
            setShowDeleteDialog(false);
            setDeleteError(null);
          }
        }}
      />
    </div>
  );
}
