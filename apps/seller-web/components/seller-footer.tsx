import { env } from "@/lib/env";

export function SellerFooter() {
  return (
    <footer className="border-t border-slate-200 bg-slate-100 text-sm text-slate-600 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-400">
      <div className="mx-auto max-w-5xl px-4 py-8 sm:px-6">
        <p className="font-medium text-slate-800 dark:text-slate-200">Not selling? Try the other portals.</p>
        <ul className="mt-3 flex flex-wrap gap-x-6 gap-y-2">
          <li>
            <a
              href={env.customerSiteUrl}
              className="hover:text-slate-900 hover:underline dark:hover:text-slate-100"
            >
              Customer storefront
            </a>
          </li>
          <li>
            <a
              href={`${env.driverSiteUrl}/register`}
              className="hover:text-slate-900 hover:underline dark:hover:text-slate-100"
            >
              Register as a driver
            </a>
          </li>
        </ul>
        <p className="mt-6 text-xs text-slate-500 dark:text-slate-500">© {new Date().getFullYear()} ZimMarket</p>
      </div>
    </footer>
  );
}
