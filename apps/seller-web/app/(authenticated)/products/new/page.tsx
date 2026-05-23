"use client";

import Link from "next/link";
import { useEffect, useState, useSyncExternalStore } from "react";

import { ProductForm } from "@/components/products/product-form";
import { KycGate } from "@/components/products/kyc-gate";
import { getKycStatus, subscribeToSession } from "@/lib/auth-session";
import { isSellerKycApproved } from "@/lib/seller-kyc";
import { sellerProductsService, type Category } from "@/lib/seller-products";

const emptyValues = {
  title: "",
  description: "",
  priceUsd: 0,
  categoryId: "",
  stockQuantity: 0,
  pickupAddress: {
    street: "",
    suburb: "",
    city: "",
    country: "Zimbabwe",
  },
};

export default function NewProductPage() {
  const kycStatus = useSyncExternalStore(subscribeToSession, getKycStatus, getKycStatus);
  const kycApproved = isSellerKycApproved(kycStatus);
  const [categories, setCategories] = useState<Category[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    const load = async () => {
      try {
        const loadedCategories = await sellerProductsService.listCategories();
        if (isMounted) {
          setCategories(loadedCategories);
          setErrorMessage(null);
        }
      } catch (error) {
        if (isMounted) {
          setErrorMessage(error instanceof Error ? error.message : "Unable to load categories.");
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
  }, []);

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
        <h1 className="mt-3 text-3xl font-semibold tracking-tight text-foreground">Add product</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Create a new listing with photos, pricing, stock, and pickup details.
        </p>
      </div>

      {errorMessage ? (
        <p className="rounded-xl border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {errorMessage}
        </p>
      ) : null}

      {isLoading ? (
        <p className="text-sm text-muted-foreground">Loading categories…</p>
      ) : (
        <ProductForm mode="create" categories={categories} initialValues={emptyValues} />
      )}
    </div>
  );
}
