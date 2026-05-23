"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";

import {
  ProductImagePicker,
  type PendingImage,
} from "@/components/products/product-image-picker";
import type { Category, ExistingProductImage, ProductFormValues } from "@/lib/seller-products";
import { sellerProductsService } from "@/lib/seller-products";
import { cn } from "@/lib/utils";

type ProductFormProps = {
  mode: "create" | "edit";
  categories: Category[];
  initialValues: ProductFormValues;
  initialExistingImages?: ExistingProductImage[];
  productId?: string;
  previousStockQuantity?: number;
  readOnly?: boolean;
  onDelete?: () => Promise<void>;
};

const defaultCountry = "Zimbabwe";

function createPendingImage(file: File): PendingImage {
  return {
    id: `${file.name}-${file.size}-${file.lastModified}-${Math.random().toString(36).slice(2)}`,
    file,
    previewUrl: URL.createObjectURL(file),
  };
}

export function ProductForm({
  mode,
  categories,
  initialValues,
  initialExistingImages = [],
  productId,
  previousStockQuantity,
  readOnly = false,
  onDelete,
}: ProductFormProps) {
  const router = useRouter();
  const [values, setValues] = useState<ProductFormValues>({
    ...initialValues,
    pickupAddress: {
      ...initialValues.pickupAddress,
      country: initialValues.pickupAddress.country || defaultCountry,
    },
  });
  const [existingImages, setExistingImages] = useState(initialExistingImages);
  const [pendingImages, setPendingImages] = useState<PendingImage[]>([]);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  const canSubmit = useMemo(() => {
    const hasImages = existingImages.length + pendingImages.length > 0;
    return (
      values.title.trim().length >= 3 &&
      values.description.trim().length >= 10 &&
      values.priceUsd > 0 &&
      values.categoryId.length > 0 &&
      values.stockQuantity >= 0 &&
      values.pickupAddress.street.trim().length > 0 &&
      values.pickupAddress.suburb.trim().length > 0 &&
      values.pickupAddress.city.trim().length > 0 &&
      hasImages
    );
  }, [existingImages.length, pendingImages.length, values]);

  const updateField = <K extends keyof ProductFormValues>(key: K, value: ProductFormValues[K]) => {
    setValues((current) => ({ ...current, [key]: value }));
  };

  const onAddFiles = (files: File[]) => {
    setPendingImages((current) => [...current, ...files.map(createPendingImage)]);
  };

  const onSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (readOnly || !canSubmit) {
      return;
    }

    setErrorMessage(null);
    setIsSubmitting(true);

    try {
      if (mode === "create") {
        const createdId = await sellerProductsService.createProduct(
          values,
          pendingImages.map((image) => image.file),
        );
        router.replace(`/products/${createdId}/edit`);
        router.refresh();
        return;
      }

      if (!productId) {
        throw new Error("Product id is required for updates.");
      }

      await sellerProductsService.updateProduct({
        productId,
        values,
        retainedImageKeys: existingImages.map((image) => image.key),
        newImageFiles: pendingImages.map((image) => image.file),
        previousStockQuantity: previousStockQuantity ?? values.stockQuantity,
      });

      router.push("/products");
      router.refresh();
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : "Unable to save product.");
    } finally {
      setIsSubmitting(false);
    }
  };

  const onDeleteClick = async () => {
    if (!onDelete || readOnly) {
      return;
    }

    const confirmed = window.confirm(
      "Delete this product? It will be hidden from your store and images will be removed. The listing record is permanently deleted after 30 days.",
    );

    if (!confirmed) {
      return;
    }

    setIsDeleting(true);
    setErrorMessage(null);

    try {
      await onDelete();
      router.push("/products");
      router.refresh();
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : "Unable to delete product.");
    } finally {
      setIsDeleting(false);
    }
  };

  const inputClassName =
    "w-full rounded-xl border border-border/80 bg-background px-3 py-2 text-sm outline-none focus:border-foreground/40 focus:ring-2 focus:ring-foreground/10 disabled:opacity-60";

  return (
    <form className="space-y-8" onSubmit={onSubmit}>
      <section className="grid gap-6 lg:grid-cols-2">
        <div className="space-y-4">
          <div className="space-y-2">
            <label className="text-sm font-medium" htmlFor="title">
              Title
            </label>
            <input
              id="title"
              className={inputClassName}
              value={values.title}
              disabled={readOnly || isSubmitting}
              onChange={(event) => updateField("title", event.target.value)}
              required
            />
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium" htmlFor="description">
              Description
            </label>
            <textarea
              id="description"
              rows={6}
              className={inputClassName}
              value={values.description}
              disabled={readOnly || isSubmitting}
              onChange={(event) => updateField("description", event.target.value)}
              required
            />
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium" htmlFor="priceUsd">
                Price (USD)
              </label>
              <input
                id="priceUsd"
                type="number"
                min={0.01}
                step={0.01}
                className={inputClassName}
                value={values.priceUsd}
                disabled={readOnly || isSubmitting}
                onChange={(event) => updateField("priceUsd", Number(event.target.value))}
                required
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium" htmlFor="stockQuantity">
                Stock quantity
              </label>
              <input
                id="stockQuantity"
                type="number"
                min={0}
                step={1}
                className={inputClassName}
                value={values.stockQuantity}
                disabled={readOnly || isSubmitting}
                onChange={(event) => updateField("stockQuantity", Number(event.target.value))}
                required
              />
            </div>
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium" htmlFor="categoryId">
              Category
            </label>
            <select
              id="categoryId"
              className={inputClassName}
              value={values.categoryId}
              disabled={readOnly || isSubmitting}
              onChange={(event) => updateField("categoryId", event.target.value)}
              required
            >
              <option value="">Select a category</option>
              {categories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className="space-y-4">
          <p className="text-sm font-medium text-foreground">Pickup address</p>
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2 sm:col-span-2">
              <label className="text-xs text-muted-foreground" htmlFor="street">
                Street
              </label>
              <input
                id="street"
                className={inputClassName}
                value={values.pickupAddress.street}
                disabled={readOnly || isSubmitting}
                onChange={(event) =>
                  updateField("pickupAddress", {
                    ...values.pickupAddress,
                    street: event.target.value,
                  })
                }
                required
              />
            </div>
            <div className="space-y-2">
              <label className="text-xs text-muted-foreground" htmlFor="suburb">
                Suburb
              </label>
              <input
                id="suburb"
                className={inputClassName}
                value={values.pickupAddress.suburb}
                disabled={readOnly || isSubmitting}
                onChange={(event) =>
                  updateField("pickupAddress", {
                    ...values.pickupAddress,
                    suburb: event.target.value,
                  })
                }
                required
              />
            </div>
            <div className="space-y-2">
              <label className="text-xs text-muted-foreground" htmlFor="city">
                City
              </label>
              <input
                id="city"
                className={inputClassName}
                value={values.pickupAddress.city}
                disabled={readOnly || isSubmitting}
                onChange={(event) =>
                  updateField("pickupAddress", {
                    ...values.pickupAddress,
                    city: event.target.value,
                  })
                }
                required
              />
            </div>
            <div className="space-y-2 sm:col-span-2">
              <label className="text-xs text-muted-foreground" htmlFor="country">
                Country
              </label>
              <input
                id="country"
                className={inputClassName}
                value={values.pickupAddress.country}
                disabled={readOnly || isSubmitting}
                onChange={(event) =>
                  updateField("pickupAddress", {
                    ...values.pickupAddress,
                    country: event.target.value,
                  })
                }
                required
              />
            </div>
          </div>

          <ProductImagePicker
            existingImages={existingImages}
            pendingImages={pendingImages}
            onAddFiles={onAddFiles}
            onRemoveExisting={(key) =>
              setExistingImages((current) => current.filter((image) => image.key !== key))
            }
            onRemovePending={(id) =>
              setPendingImages((current) => current.filter((image) => image.id !== id))
            }
            disabled={readOnly || isSubmitting}
          />
        </div>
      </section>

      {errorMessage ? (
        <p className="rounded-xl border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {errorMessage}
        </p>
      ) : null}

      {!readOnly ? (
        <div className="flex flex-wrap items-center gap-3">
          <button
            type="submit"
            disabled={!canSubmit || isSubmitting || isDeleting}
            className={cn(
              "inline-flex h-10 items-center justify-center rounded-xl bg-foreground px-5 text-sm font-medium text-background hover:opacity-90 disabled:opacity-50",
            )}
          >
            {isSubmitting ? "Saving…" : mode === "create" ? "Create product" : "Save changes"}
          </button>
          <button
            type="button"
            onClick={() => router.push("/products")}
            className="inline-flex h-10 items-center justify-center rounded-xl border border-border/80 px-5 text-sm font-medium hover:bg-muted/60"
          >
            Cancel
          </button>
          {mode === "edit" && onDelete ? (
            <button
              type="button"
              onClick={() => void onDeleteClick()}
              disabled={isSubmitting || isDeleting}
              className="inline-flex h-10 items-center justify-center rounded-xl border border-destructive/40 px-5 text-sm font-medium text-destructive hover:bg-destructive/10 disabled:opacity-50"
            >
              {isDeleting ? "Deleting…" : "Delete product"}
            </button>
          ) : null}
        </div>
      ) : null}
    </form>
  );
}
