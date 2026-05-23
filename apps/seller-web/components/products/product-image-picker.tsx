"use client";

import Image from "next/image";
import { useRef } from "react";
import { ImagePlus, X } from "lucide-react";

import { MAX_PRODUCT_IMAGES } from "@/lib/seller-products";
import { resolveProductImageContentType } from "@/lib/file-upload";
import type { ExistingProductImage } from "@/lib/seller-products";
import { cn } from "@/lib/utils";

type PendingImage = {
  id: string;
  file: File;
  previewUrl: string;
};

type ProductImagePickerProps = {
  existingImages: ExistingProductImage[];
  pendingImages: PendingImage[];
  onAddFiles: (files: File[]) => void;
  onRemoveExisting: (key: string) => void;
  onRemovePending: (id: string) => void;
  disabled?: boolean;
};

export function ProductImagePicker({
  existingImages,
  pendingImages,
  onAddFiles,
  onRemoveExisting,
  onRemovePending,
  disabled = false,
}: ProductImagePickerProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const totalCount = existingImages.length + pendingImages.length;
  const remainingSlots = MAX_PRODUCT_IMAGES - totalCount;

  const onFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const selected = Array.from(event.target.files ?? []);
    event.target.value = "";

    if (selected.length === 0) {
      return;
    }

    const accepted: File[] = [];
    for (const file of selected.slice(0, remainingSlots)) {
      if (!resolveProductImageContentType(file)) {
        window.alert("Only JPG, PNG, and WEBP images are supported.");
        continue;
      }

      accepted.push(file);
    }

    if (accepted.length > 0) {
      onAddFiles(accepted);
    }
  };

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="text-sm font-medium text-foreground">Product images</p>
        <p className="text-xs text-muted-foreground">
          {totalCount}/{MAX_PRODUCT_IMAGES} (JPG, PNG, WEBP)
        </p>
      </div>

      <div className="flex flex-wrap gap-3">
        {existingImages.map((image) => (
          <div
            key={image.key}
            className="relative size-24 overflow-hidden rounded-xl border border-border/70 bg-muted"
          >
            <Image src={image.url} alt="" fill className="object-cover" unoptimized />
            <button
              type="button"
              disabled={disabled}
              onClick={() => onRemoveExisting(image.key)}
              className="absolute right-1 top-1 flex size-6 items-center justify-center rounded-full bg-black/60 text-white hover:bg-black/80 disabled:opacity-50"
              aria-label="Remove image"
            >
              <X className="size-3.5" />
            </button>
          </div>
        ))}

        {pendingImages.map((image) => (
          <div
            key={image.id}
            className="relative size-24 overflow-hidden rounded-xl border border-border/70 bg-muted"
          >
            <Image src={image.previewUrl} alt="" fill className="object-cover" unoptimized />
            <button
              type="button"
              disabled={disabled}
              onClick={() => onRemovePending(image.id)}
              className="absolute right-1 top-1 flex size-6 items-center justify-center rounded-full bg-black/60 text-white hover:bg-black/80 disabled:opacity-50"
              aria-label="Remove image"
            >
              <X className="size-3.5" />
            </button>
          </div>
        ))}

        {remainingSlots > 0 ? (
          <button
            type="button"
            disabled={disabled}
            onClick={() => inputRef.current?.click()}
            className={cn(
              "flex size-24 flex-col items-center justify-center gap-1 rounded-xl border border-dashed border-border/80 bg-muted/40 text-muted-foreground transition-colors hover:bg-muted/70 hover:text-foreground disabled:opacity-50",
            )}
          >
            <ImagePlus className="size-5" />
            <span className="text-[10px] font-medium">Add</span>
          </button>
        ) : null}
      </div>

      <input
        ref={inputRef}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        multiple
        className="hidden"
        onChange={onFileChange}
      />
    </div>
  );
}

export type { PendingImage };
