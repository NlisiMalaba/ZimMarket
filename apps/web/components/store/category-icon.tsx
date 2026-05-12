import type { StorefrontCategory } from "@/lib/storefront-data";

export function CategoryIcon({ icon, className }: { icon: StorefrontCategory["icon"]; className?: string }) {
  const cn = className ?? "h-7 w-7";
  switch (icon) {
    case "electronics":
      return (
        <svg className={cn} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" aria-hidden>
          <rect x="3" y="4" width="18" height="12" rx="2" />
          <path d="M8 20h8M12 16v4" strokeLinecap="round" />
        </svg>
      );
    case "phone":
      return (
        <svg className={cn} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" aria-hidden>
          <rect x="7" y="3" width="10" height="18" rx="2" />
          <path d="M10 18h4" strokeLinecap="round" />
        </svg>
      );
    case "fashion":
      return (
        <svg className={cn} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" aria-hidden>
          <path d="M6 4l3 4h6l3-4" strokeLinejoin="round" />
          <path d="M9 8v12h6V8" strokeLinejoin="round" />
        </svg>
      );
    case "home":
      return (
        <svg className={cn} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" aria-hidden>
          <path d="M4 10.5L12 4l8 6.5V20a1 1 0 0 1-1 1h-5v-6H10v6H5a1 1 0 0 1-1-1v-9.5z" strokeLinejoin="round" />
        </svg>
      );
    case "beauty":
      return (
        <svg className={cn} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" aria-hidden>
          <path d="M9 3h6v6a3 3 0 1 1-6 0V3z" />
          <path d="M8 21h8M12 12v9" strokeLinecap="round" />
        </svg>
      );
    case "auto":
      return (
        <svg className={cn} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" aria-hidden>
          <circle cx="7" cy="17" r="2.5" />
          <circle cx="17" cy="17" r="2.5" />
          <path d="M5.5 17H3l2-6h11l2 6h-2.5M9 11l1-4h5l1 4" strokeLinejoin="round" />
        </svg>
      );
    case "deals":
      return (
        <svg className={cn} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" aria-hidden>
          <path d="M4 12l2-2 4 4 8-8 2 2-10 10-6-6z" strokeLinejoin="round" />
        </svg>
      );
    default:
      return null;
  }
}
