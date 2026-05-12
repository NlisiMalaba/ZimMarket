export const metadata = {
  title: "Your orders",
};

export default function OrdersPage() {
  return (
    <div className="mx-auto max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
      <h1 className="text-2xl font-semibold text-neutral-900">Your orders</h1>
      <p className="mt-2 text-neutral-600">Order history will load from the customer orders API.</p>
    </div>
  );
}
