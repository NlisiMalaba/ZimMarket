import { env } from "@/lib/env";

export function SiteFooter() {
  return (
    <footer className="border-t border-neutral-200 bg-neutral-100 text-sm text-neutral-700">
      <div className="mx-auto max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
        <div className="grid gap-8 sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <p className="font-semibold text-neutral-900">Get to know us</p>
            <ul className="mt-3 space-y-2">
              <li>
                <a href="/about" className="hover:text-store-accent hover:underline">
                  About ZimMarket
                </a>
              </li>
              <li>
                <a href="/help" className="hover:text-store-accent hover:underline">
                  Help
                </a>
              </li>
            </ul>
          </div>
          <div>
            <p className="font-semibold text-neutral-900">Make money with us</p>
            <ul className="mt-3 space-y-2">
              <li>
                <a href={`${env.sellerSiteUrl}/register`} className="hover:text-store-accent hover:underline">
                  Sell on ZimMarket
                </a>
              </li>
              <li>
                <a href={`${env.sellerSiteUrl}`} className="hover:text-store-accent hover:underline">
                  Seller portal
                </a>
              </li>
            </ul>
          </div>
          <div>
            <p className="font-semibold text-neutral-900">Deliver with us</p>
            <ul className="mt-3 space-y-2">
              <li>
                <a href={`${env.driverSiteUrl}/register`} className="hover:text-store-accent hover:underline">
                  Drive for ZimMarket
                </a>
              </li>
              <li>
                <a href={`${env.driverSiteUrl}`} className="hover:text-store-accent hover:underline">
                  Driver portal
                </a>
              </li>
            </ul>
          </div>
          <div>
            <p className="font-semibold text-neutral-900">Shopping</p>
            <ul className="mt-3 space-y-2">
              <li>
                <a href="/returns" className="hover:text-store-accent hover:underline">
                  Returns
                </a>
              </li>
              <li>
                <a href="/privacy" className="hover:text-store-accent hover:underline">
                  Privacy
                </a>
              </li>
            </ul>
          </div>
        </div>
        <p className="mt-10 text-xs text-neutral-500">© {new Date().getFullYear()} ZimMarket. All rights reserved.</p>
      </div>
    </footer>
  );
}
