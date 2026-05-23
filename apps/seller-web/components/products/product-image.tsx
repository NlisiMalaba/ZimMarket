"use client";

import { useEffect, useState } from "react";
import { Package } from "lucide-react";

import { isAbortError, productImagesService } from "@/lib/product-images";
import { cn } from "@/lib/utils";

type ProductImageProps = {
  imageKey?: string | null;
  alt: string;
  className?: string;
  fill?: boolean;
  iconClassName?: string;
};

export function ProductImage({
  imageKey,
  alt,
  className,
  fill = true,
  iconClassName,
}: ProductImageProps) {
  const [src, setSrc] = useState<string | null>(null);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    const trimmedKey = imageKey?.trim();
    if (!trimmedKey) {
      setSrc(null);
      setFailed(false);
      return;
    }

    const abortController = new AbortController();
    let objectUrl: string | null = null;
    let cancelled = false;

    setFailed(false);
    setSrc(null);

    void (async () => {
      try {
        const loaded = await productImagesService.loadImageObjectUrl(
          trimmedKey,
          abortController.signal,
        );

        if (cancelled || abortController.signal.aborted) {
          if (loaded) {
            URL.revokeObjectURL(loaded);
          }

          return;
        }

        if (!loaded) {
          setFailed(true);
          return;
        }

        objectUrl = loaded;
        setSrc(loaded);
      } catch (error) {
        if (cancelled || abortController.signal.aborted || isAbortError(error)) {
          return;
        }

        setFailed(true);
      }
    })();

    return () => {
      cancelled = true;
      abortController.abort();
      if (objectUrl) {
        URL.revokeObjectURL(objectUrl);
      }
    };
  }, [imageKey]);

  if (!imageKey?.trim() || failed || !src) {
    return (
      <div
        className={cn(
          "flex items-center justify-center bg-muted text-muted-foreground",
          fill ? "absolute inset-0" : "size-full",
          className,
        )}
      >
        <Package className={cn("size-4", iconClassName)} aria-hidden />
      </div>
    );
  }

  return (
    // eslint-disable-next-line @next/next/no-img-element -- blob URLs from authenticated fetch
    <img
      src={src}
      alt={alt}
      className={cn(fill ? "absolute inset-0 size-full object-cover" : "size-full object-cover", className)}
    />
  );
}
