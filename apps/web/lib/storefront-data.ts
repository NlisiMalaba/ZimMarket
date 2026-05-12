export type StorefrontCategory = {
  slug: string;
  name: string;
  description: string;
  icon: "electronics" | "phone" | "fashion" | "home" | "beauty" | "auto" | "deals";
  accent: string;
};

export type StorefrontProduct = {
  id: string;
  slug: string;
  name: string;
  image: string;
  priceUsd: number;
  compareAtUsd?: number;
  rating: number;
  reviewCount: number;
  sellerName: string;
  verifiedSeller: boolean;
  deliveryEstimate: string;
  warehouseVerified?: boolean;
  badge?: "trending" | "new" | "deal";
};

export type HeroSlide = {
  id: string;
  eyebrow: string;
  title: string;
  subtitle: string;
  ctaLabel: string;
  ctaHref: string;
  secondaryCtaLabel?: string;
  secondaryCtaHref?: string;
  image: string;
  gradient: string;
};

export const STOREFRONT_CATEGORIES: StorefrontCategory[] = [
  {
    slug: "electronics",
    name: "Electronics",
    description: "Laptops, audio, chargers—the unglamorous stuff that has to work",
    icon: "electronics",
    accent: "from-sky-500/15 to-brand/10",
  },
  {
    slug: "phones",
    name: "Phones",
    description: "Smartphones & accessories",
    icon: "phone",
    accent: "from-violet-500/15 to-brand/10",
  },
  {
    slug: "fashion",
    name: "Fashion",
    description: "Apparel & footwear",
    icon: "fashion",
    accent: "from-rose-500/12 to-brand/8",
  },
  {
    slug: "home-living",
    name: "Home & Living",
    description: "Furniture & décor",
    icon: "home",
    accent: "from-amber-500/12 to-brand/8",
  },
  {
    slug: "beauty",
    name: "Beauty",
    description: "Skincare & wellness",
    icon: "beauty",
    accent: "from-fuchsia-500/12 to-brand/8",
  },
  {
    slug: "automotive",
    name: "Automotive",
    description: "Parts & care",
    icon: "auto",
    accent: "from-slate-500/20 to-brand/10",
  },
  {
    slug: "deals",
    name: "Deals",
    description: "Curated savings",
    icon: "deals",
    accent: "from-cta/20 to-brand/10",
  },
];

export const HERO_SLIDES: HeroSlide[] = [
  {
    id: "1",
    eyebrow: "This week",
    title: "Upgrade your everyday tech",
    subtitle:
      "If you’re buying a laptop or headphones online, you want the boring stuff done right—clear seller history, real photos, and delivery you can plan your day around.",
    ctaLabel: "Browse electronics",
    ctaHref: "/categories/electronics",
    secondaryCtaLabel: "See what’s on sale",
    secondaryCtaHref: "/deals",
    image:
      "https://images.unsplash.com/photo-1498049794561-7780e7231661?auto=format&fit=crop&w=1600&q=80",
    gradient: "from-[#0f172a]/92 via-[#1e3a8a]/55 to-[#1e3a8a]/25",
  },
  {
    id: "2",
    eyebrow: "From the rail",
    title: "Clothes you’ll actually wear",
    subtitle:
      "We’re picky about who sells fashion here—fewer mystery shops, more sellers who answer messages and ship on time.",
    ctaLabel: "Shop fashion",
    ctaHref: "/categories/fashion",
    secondaryCtaLabel: "What just landed",
    secondaryCtaHref: "/search?tag=new",
    image:
      "https://images.unsplash.com/photo-1441986300917-64674bd600d8?auto=format&fit=crop&w=1600&q=80",
    gradient: "from-[#0f172a]/90 via-slate-900/60 to-indigo-900/30",
  },
  {
    id: "3",
    eyebrow: "Automotive",
    title: "Parts, dash cams, and car care you can trust",
    subtitle:
      "Clear fitment notes, honest photos, and delivery windows you can plan around—whether you’re commuting in Harare or road-tripping from Bulawayo.",
    ctaLabel: "Shop automotive",
    ctaHref: "/categories/automotive",
    secondaryCtaLabel: "How delivery works",
    secondaryCtaHref: "/help",
    image:
      "https://images.unsplash.com/photo-1494976388539-d1058494cdd8?auto=format&fit=crop&w=1600&q=80",
    gradient: "from-[#0f172a]/92 via-slate-900/65 to-slate-800/35",
  },
  {
    id: "4",
    eyebrow: "Orders",
    title: "Follow it like a parcel should be followed",
    subtitle:
      "No more “it’s on the way” with no context. You’ll see handoffs, delays (when they happen), and who to talk to—without hunting for a support email.",
    ctaLabel: "Open your orders",
    ctaHref: "/orders",
    secondaryCtaLabel: "Keep browsing",
    secondaryCtaHref: "/categories",
    image:
      "https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?auto=format&fit=crop&w=1600&q=80",
    gradient: "from-[#020617]/92 via-[#1e3a8a]/50 to-slate-800/35",
  },
];

const baseProducts: StorefrontProduct[] = [
  {
    id: "p1",
    slug: "wireless-noise-headphones",
    name: "Studio-grade wireless headphones",
    image: "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?auto=format&fit=crop&w=800&q=80",
    priceUsd: 189,
    compareAtUsd: 249,
    rating: 4.8,
    reviewCount: 3240,
    sellerName: "Harare Audio Co.",
    verifiedSeller: true,
    deliveryEstimate: "Tomorrow · 09:00–13:00",
    warehouseVerified: true,
    badge: "deal",
  },
  {
    id: "p2",
    slug: "ultrabook-14",
    name: "14\" Ultrabook — 16GB / 512GB",
    image: "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?auto=format&fit=crop&w=800&q=80",
    priceUsd: 899,
    compareAtUsd: 1049,
    rating: 4.7,
    reviewCount: 812,
    sellerName: "Verified Tech ZW",
    verifiedSeller: true,
    deliveryEstimate: "2-day · Bulawayo",
    warehouseVerified: true,
    badge: "trending",
  },
  {
    id: "p3",
    slug: "smartwatch-pro",
    name: "Smartwatch Pro — sapphire glass",
    image: "https://images.unsplash.com/photo-1523275335684-37898b6baf30?auto=format&fit=crop&w=800&q=80",
    priceUsd: 279,
    rating: 4.6,
    reviewCount: 1544,
    sellerName: "Pulse Retail",
    verifiedSeller: true,
    deliveryEstimate: "Same-day · Harare CBD",
    badge: "new",
  },
  {
    id: "p4",
    slug: "minimal-running-shoes",
    name: "Minimal running shoes",
    image: "https://images.unsplash.com/photo-1542291026-7eec264c27ff?auto=format&fit=crop&w=800&q=80",
    priceUsd: 119,
    compareAtUsd: 149,
    rating: 4.5,
    reviewCount: 620,
    sellerName: "Stride Collective",
    verifiedSeller: true,
    deliveryEstimate: "Wed · 12:00–16:00",
  },
  {
    id: "p5",
    slug: "ergonomic-mesh-office-chair",
    name: "Ergonomic mesh office chair",
    image: "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?auto=format&fit=crop&w=800&q=80",
    priceUsd: 165,
    rating: 4.9,
    reviewCount: 210,
    sellerName: "HomeNest ZW",
    verifiedSeller: true,
    deliveryEstimate: "Thu · Morning slot",
    warehouseVerified: true,
  },
  {
    id: "p6",
    slug: "portable-bluetooth-speaker-ipx7",
    name: "Portable Bluetooth speaker (IPX7)",
    image: "https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?auto=format&fit=crop&w=800&q=80",
    priceUsd: 79,
    compareAtUsd: 99,
    rating: 4.8,
    reviewCount: 980,
    sellerName: "Harare Audio Co.",
    verifiedSeller: true,
    deliveryEstimate: "Today · 15:00–18:00",
    badge: "deal",
  },
  {
    id: "p7",
    slug: "daily-radiance-serum",
    name: "Daily radiance serum 30ml",
    image: "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?auto=format&fit=crop&w=800&q=80",
    priceUsd: 36,
    rating: 4.4,
    reviewCount: 412,
    sellerName: "Lumière Beauty",
    verifiedSeller: false,
    deliveryEstimate: "Fri · Standard",
  },
  {
    id: "p8",
    slug: "dash-camera-4k",
    name: "4K dash camera with night mode",
    image: "https://images.unsplash.com/photo-1486262715619-67b85e0b08d3?auto=format&fit=crop&w=800&q=80",
    priceUsd: 129,
    compareAtUsd: 179,
    rating: 4.3,
    reviewCount: 305,
    sellerName: "AutoSure Parts",
    verifiedSeller: true,
    deliveryEstimate: "Sat · 10:00–14:00",
    badge: "trending",
  },
];

export const STOREFRONT_PRODUCTS: StorefrontProduct[] = baseProducts;

export function getProductBySlug(slug: string): StorefrontProduct | undefined {
  return STOREFRONT_PRODUCTS.find((p) => p.slug === slug);
}

export function getProductsByBadge(badge: NonNullable<StorefrontProduct["badge"]>): StorefrontProduct[] {
  return STOREFRONT_PRODUCTS.filter((p) => p.badge === badge);
}

export function getCategoryBySlug(slug: string): StorefrontCategory | undefined {
  return STOREFRONT_CATEGORIES.find((c) => c.slug === slug);
}

export type MegaNavColumn = {
  title: string;
  href: string;
  items: { label: string; href: string }[];
};

export const MEGA_NAV: MegaNavColumn[] = [
  {
    title: "Electronics & office",
    href: "/categories/electronics",
    items: [
      { label: "Laptops", href: "/search?q=laptops" },
      { label: "Audio", href: "/search?q=audio" },
      { label: "Monitors", href: "/search?q=monitors" },
      { label: "Networking", href: "/search?q=networking" },
    ],
  },
  {
    title: "Phones & wearables",
    href: "/categories/phones",
    items: [
      { label: "Smartphones", href: "/search?q=phones" },
      { label: "Cases", href: "/search?q=phone+cases" },
      { label: "Smartwatches", href: "/search?q=watch" },
      { label: "Chargers", href: "/search?q=chargers" },
    ],
  },
  {
    title: "Home & living",
    href: "/categories/home-living",
    items: [
      { label: "Furniture", href: "/search?q=furniture" },
      { label: "Bedding", href: "/search?q=bedding" },
      { label: "Lighting", href: "/search?q=lighting" },
      { label: "Storage", href: "/search?q=storage" },
    ],
  },
  {
    title: "Beauty & auto",
    href: "/categories/beauty",
    items: [
      { label: "Beauty", href: "/categories/beauty" },
      { label: "Automotive", href: "/categories/automotive" },
      { label: "Phone accessories", href: "/categories/phones" },
      { label: "Deals", href: "/deals" },
    ],
  },
];
