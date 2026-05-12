import Link from "next/link";

export default function SellerHomePage() {
  return (
    <div className="mx-auto max-w-5xl px-4 py-14 sm:px-6">
      <p className="text-sm font-semibold uppercase tracking-wide text-slate-500">Seller subdomain</p>
      <h1 className="mt-2 text-3xl font-semibold tracking-tight text-slate-900 sm:text-4xl">
        Reach customers ready to buy
      </h1>
      <p className="mt-4 max-w-2xl text-lg text-slate-600">
        This portal is only for merchants. Shoppers stay on the main ZimMarket site; drivers use their own
        subdomain.
      </p>
      <div className="mt-10 flex flex-wrap gap-3">
        <Link
          href="/register"
          className="inline-flex rounded-md bg-slate-900 px-5 py-2.5 text-sm font-medium text-white hover:bg-slate-800"
        >
          Create seller account
        </Link>
        <Link
          href="/login"
          className="inline-flex rounded-md border border-slate-300 bg-white px-5 py-2.5 text-sm font-medium text-slate-800 hover:bg-slate-50"
        >
          Seller sign in
        </Link>
      </div>
    </div>
  );
}
