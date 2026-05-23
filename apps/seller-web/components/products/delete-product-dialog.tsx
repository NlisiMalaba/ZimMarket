"use client";

import { Trash2 } from "lucide-react";

import { ConfirmDialog } from "@/components/ui/confirm-dialog";

export const DELETE_PRODUCT_DESCRIPTION =
  "It will be hidden from your store and images will be removed. The listing record is permanently deleted after 30 days.";

type DeleteProductDialogProps = {
  open: boolean;
  productTitle?: string;
  isDeleting?: boolean;
  errorMessage?: string | null;
  onConfirm: () => void;
  onCancel: () => void;
};

export function DeleteProductDialog({
  open,
  productTitle,
  isDeleting = false,
  errorMessage = null,
  onConfirm,
  onCancel,
}: DeleteProductDialogProps) {
  return (
    <ConfirmDialog
      open={open}
      title="Delete this product?"
      icon={
        <span className="mb-4 inline-flex size-10 items-center justify-center rounded-full bg-destructive/10 text-destructive">
          <Trash2 className="size-5" />
        </span>
      }
      description={
        <div className="space-y-3">
          {productTitle ? (
            <p>
              You are about to delete{" "}
              <span className="font-medium text-foreground">{productTitle}</span>.
            </p>
          ) : null}
          <p>{DELETE_PRODUCT_DESCRIPTION}</p>
        </div>
      }
      confirmLabel="Delete"
      cancelLabel="Cancel"
      isLoading={isDeleting}
      errorMessage={errorMessage}
      destructive
      onConfirm={onConfirm}
      onCancel={onCancel}
    />
  );
}
