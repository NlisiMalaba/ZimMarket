"use client";

import Link from "next/link";
import { use, useEffect, useState, useSyncExternalStore } from "react";

import { ProductForm } from "@/components/products/product-form";
import { KycGate } from "@/components/products/kyc-gate";
import { getKycStatus, subscribeToSession } from "@/lib/auth-session";
import { isSellerKycApproved } from "@/lib/seller-kyc";
import {
  DELETED_PRODUCT_RETENTION_DAYS,
  sellerProductsService,
  type Category,
  type SellerProductDetail,
} from "@/lib/seller-products";

type EditProductPageProps = {
  params: Promise<{ productId: string }>;
};

export default function EditProductPage({ params }: EditProductPageProps) {
  const { productId } = use(params);
  const kycStatus = useSyncExternalStore(subscribeToSession, getKycStatus, getKycStatus);
  const kycApproved = isSellerKycApproved(kycStatus);

  const [categories, setCategories] = useState<Category[]>([]);
  const [product, setProduct] = useState<SellerProductDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    const load = async () => {
      try {
        const [loadedCategories, loadedProduct] = await Promise.all([
          sellerProductsService.listCategories(),
          sellerProductsService.getProduct(productId),
        ]);

        if (isMounted) {
          setCategories(loadedCategories);
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

  if (!kycApproved) {
    return (
      <div className="mx-auto max-w-[900px] space-y-6">
        <Link href="/products" className="text-sm text-muted-foreground hover:text-foreground">
          ← Back to products
        </Link>
        <KycGate>{null}</KycGate>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-[1100px] space-y-6">
      <div>
        <Link href="/products" className="text-sm text-muted-foreground hover:text-foreground">
          ← Back to products
        </Link>
        <h1 className="mt-3 text-3xl font-semibold tracking-tight text-foreground">Edit product</h1>
        {isDeleted ? (
          <p className="mt-1 text-sm text-amber-700 dark:text-amber-400">
            This product was deleted. It will be permanently removed after{" "}
            {DELETED_PRODUCT_RETENTION_DAYS} days and cannot be edited.
          </p>
        ) : (
          <p className="mt-1 text-sm text-muted-foreground">
            Update listing details, stock, and images. Removing images deletes them from storage.
          </p>
        )}
      </div>

      {errorMessage ? (
        <p className="rounded-xl border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {errorMessage}
        </p>
      ) : null}

      {isLoading || !product ? (
        <p className="text-sm text-muted-foreground">Loading product…</p>
      ) : (
        <ProductForm
          mode="edit"
          productId={product.productId}
          previousStockQuantity={product.stockQuantity}
          categories={categories}
          readOnly={isDeleted}
          initialValues={{
            title: product.title,
            description: product.description,
            priceUsd: product.priceAmount,
            categoryId: product.categoryId,
            stockQuantity: product.stockQuantity,
            pickupAddress: product.pickupAddress,
          }}
          initialExistingImages={product.imageKeys.map((key, index) => ({
            key,
            url: product.imageUrls[index] ?? "",
          }))}
          onDelete={
            isDeleted
              ? undefined
              : async () => {
                  await sellerProductsService.deleteProduct(product.productId);
                }
          }
        />
      )}
    </div>
  );
}
