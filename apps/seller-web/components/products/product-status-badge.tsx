import { cn } from "@/lib/utils";

type ProductStatusBadgeProps = {
  status: number | string;
  className?: string;
};

function resolveStatus(status: number | string): number {
  return typeof status === "string" ? Number.parseInt(status, 10) : status;
}

export function ProductStatusBadge({ status, className }: ProductStatusBadgeProps) {
  const value = resolveStatus(status);

  if (value === 0) {
    return (
      <span
        className={cn(
          "inline-flex rounded-md bg-emerald-100 px-2.5 py-0.5 text-xs font-medium text-emerald-800 dark:bg-emerald-950/60 dark:text-emerald-300",
          className,
        )}
      >
        Active
      </span>
    );
  }

  if (value === 1) {
    return (
      <span
        className={cn(
          "inline-flex rounded-md bg-amber-100 px-2.5 py-0.5 text-xs font-medium text-amber-800 dark:bg-amber-950/60 dark:text-amber-300",
          className,
        )}
      >
        Draft
      </span>
    );
  }

  if (value === 2) {
    return (
      <span
        className={cn(
          "inline-flex rounded-md bg-zinc-200 px-2.5 py-0.5 text-xs font-medium text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300",
          className,
        )}
      >
        Archived
      </span>
    );
  }

  return (
    <span
      className={cn(
        "inline-flex rounded-md bg-muted px-2.5 py-0.5 text-xs font-medium text-muted-foreground",
        className,
      )}
    >
      Unknown
    </span>
  );
}
