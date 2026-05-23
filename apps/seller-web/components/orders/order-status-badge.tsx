import { getSellerOrderDisplayStatus, type SellerOrderDisplayStatus } from "@/lib/domain-enums";
import { cn } from "@/lib/utils";

const statusStyles: Record<SellerOrderDisplayStatus, string> = {
  Completed: "bg-emerald-600 text-white",
  Processing: "bg-foreground text-background",
  Pending: "bg-orange-500 text-white",
  Cancelled: "bg-red-600 text-white",
};

type OrderStatusBadgeProps = {
  status: number | string;
  className?: string;
};

export function OrderStatusBadge({ status, className }: OrderStatusBadgeProps) {
  const label = getSellerOrderDisplayStatus(status);

  return (
    <span
      className={cn(
        "inline-flex rounded-full px-2.5 py-1 text-xs font-semibold",
        statusStyles[label],
        className,
      )}
    >
      {label}
    </span>
  );
}
