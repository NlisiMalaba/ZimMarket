import { env } from "@/lib/env";

export function DriverFooter() {
  return (
    <footer className="border-t border-emerald-200 bg-emerald-50/80 text-sm text-neutral-700">
      <div className="mx-auto max-w-5xl px-4 py-8 sm:px-6">
        <p className="font-medium text-emerald-950">Wrong portal?</p>
        <ul className="mt-3 flex flex-wrap gap-x-6 gap-y-2">
          <li>
            <a href={env.customerSiteUrl} className="hover:text-emerald-900 hover:underline">
              Customer storefront
            </a>
          </li>
          <li>
            <a href={`${env.sellerSiteUrl}/register`} className="hover:text-emerald-900 hover:underline">
              Register as a seller
            </a>
          </li>
        </ul>
        <p className="mt-6 text-xs text-neutral-500">© {new Date().getFullYear()} ZimMarket</p>
      </div>
    </footer>
  );
}
