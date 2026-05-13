import Link from "next/link";

export function TrustStrip() {
  return (
    <div className="border-y border-border/70 bg-page-elevated/80 dark:border-slate-800/80 dark:bg-slate-950/50">
      <div className="container-store flex flex-col gap-5 py-5 sm:flex-row sm:items-start sm:justify-between sm:gap-10">
        <p className="max-w-3xl text-[15px] leading-relaxed text-foreground/90 sm:text-sm">
          <span className="font-semibold text-foreground">We get it—</span>
          buying from someone you’ve never met can feel odd. ZimMarket is built around the unglamorous bits: clearer
          seller signals, payments that don’t feel sketchy, and delivery updates that read like a human wrote them—not
          a template.
        </p>
        <div className="flex shrink-0 flex-col gap-2 text-sm sm:items-end sm:text-right">
          <Link href="/help" className="font-medium text-brand underline-offset-4 hover:underline">
            How we handle disputes
          </Link>
          <Link href="/privacy" className="text-muted underline-offset-4 hover:text-foreground hover:underline">
            Privacy (plain language)
          </Link>
        </div>
      </div>
    </div>
  );
}

export function BuyerProtectionBanner() {
  return (
    <aside className="container-store py-8">
      <div className="rounded-none border border-border/70 bg-page-elevated p-6 sm:p-8 dark:border-slate-800/80 dark:bg-slate-900/40">
        <div className="flex flex-col gap-8 lg:flex-row lg:items-start lg:justify-between lg:gap-12">
          <div className="max-w-2xl">
            <p className="text-sm font-medium text-muted">A note on trust</p>
            <h2 className="mt-2 font-display text-xl font-semibold tracking-tight text-foreground sm:text-2xl">
              We can’t promise nothing will ever go wrong. We can promise the story won’t disappear.
            </h2>
            <p className="mt-3 text-sm leading-relaxed text-muted">
              Orders keep a paper-trail-friendly history: what you paid, what the seller committed to, and where
              delivery is. If something looks off, you’re not starting from zero with support.
            </p>
          </div>
          <ul className="space-y-3 text-sm leading-relaxed text-muted lg:max-w-sm">
            <li className="border-l-2 border-brand/35 pl-4">
              <span className="font-medium text-foreground">Payments:</span> encrypted in transit, with clear
              receipts you can screenshot without shame.
            </li>
            <li className="border-l-2 border-emerald-500/35 pl-4">
              <span className="font-medium text-foreground">Delivery:</span> estimates are honest when routes are busy;
              we’d rather under-promise.
            </li>
            <li className="border-l-2 border-amber-500/35 pl-4">
              <span className="font-medium text-foreground">Sellers:</span> verification is a signal, not a personality
              test—good sellers earn repeat buyers the old-fashioned way.
            </li>
          </ul>
        </div>
      </div>
    </aside>
  );
}
