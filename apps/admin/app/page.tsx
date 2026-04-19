import Link from "next/link";

export default function Home() {
  return (
    <main className="mx-auto flex min-h-screen w-full max-w-md items-center px-6">
      <div className="w-full rounded-xl border bg-card p-6 shadow-sm">
        <h1 className="text-xl font-semibold">ZimMarket Admin</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Use the login page to access protected admin tools.
        </p>
        <Link
          href="/login"
          className="mt-6 inline-flex h-9 w-full items-center justify-center rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground"
        >
          Go to Login
        </Link>
      </div>
    </main>
  );
}
