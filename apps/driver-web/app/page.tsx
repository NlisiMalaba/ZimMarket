import Link from "next/link";

export default function DriverHomePage() {
  return (
    <div className="mx-auto max-w-5xl px-4 py-14 sm:px-6">
      <p className="text-sm font-semibold uppercase tracking-wide text-emerald-800">Driver subdomain</p>
      <h1 className="mt-2 text-3xl font-semibold tracking-tight text-emerald-950 sm:text-4xl">
        Deliver and earn on your schedule
      </h1>
      <p className="mt-4 max-w-2xl text-lg text-neutral-700">
        This site is for delivery partners. Active routing and batch pickup stay in the driver mobile app; use
        the web for onboarding, documents, and support.
      </p>
      <div className="mt-10 flex flex-wrap gap-3">
        <Link
          href="/register"
          className="inline-flex rounded-md bg-emerald-700 px-5 py-2.5 text-sm font-medium text-white hover:bg-emerald-800"
        >
          Start driver application
        </Link>
        <Link
          href="/login"
          className="inline-flex rounded-md border border-emerald-300 bg-white px-5 py-2.5 text-sm font-medium text-emerald-950 hover:bg-emerald-50"
        >
          Driver sign in
        </Link>
      </div>
    </div>
  );
}
