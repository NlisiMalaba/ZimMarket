import Link from "next/link";

export function KycGate({ children }: { children: React.ReactNode }) {
  return (
    <div className="rounded-2xl border border-amber-500/30 bg-amber-500/10 px-6 py-8 text-center shadow-sm">
      <h2 className="text-lg font-semibold text-foreground">Verification required</h2>
      <p className="mx-auto mt-2 max-w-md text-sm text-muted-foreground">
        Your seller account must be KYC-approved before you can create or edit product listings.
        Images are removed when you delete a listing; records are permanently purged after 30 days.
      </p>
      <Link
        href="/verification"
        className="mt-6 inline-flex h-10 items-center justify-center rounded-xl bg-foreground px-5 text-sm font-medium text-background hover:opacity-90"
      >
        View verification status
      </Link>
      {children}
    </div>
  );
}
