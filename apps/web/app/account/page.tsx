export const metadata = {
  title: "Your account",
};

export default function AccountPage() {
  return (
    <div className="mx-auto max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
      <h1 className="text-2xl font-semibold text-neutral-900">Your account</h1>
      <p className="mt-2 text-neutral-600">Customer sign-in and profile will live here (web auth BFF, same API as mobile).</p>
    </div>
  );
}
