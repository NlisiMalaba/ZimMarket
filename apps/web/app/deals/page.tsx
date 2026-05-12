export const metadata = {
  title: "Deals",
};

export default function DealsPage() {
  return (
    <div className="mx-auto max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
      <h1 className="text-2xl font-semibold text-neutral-900">Today&apos;s deals</h1>
      <p className="mt-2 text-neutral-600">Promoted offers will load from the API.</p>
    </div>
  );
}
