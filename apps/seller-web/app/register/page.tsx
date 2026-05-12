export const metadata = {
  title: "Register as a seller",
};

export default function SellerRegisterPage() {
  return (
    <div className="mx-auto max-w-lg px-4 py-12 sm:px-6">
      <h1 className="text-2xl font-semibold text-slate-900">Seller registration</h1>
      <p className="mt-2 text-slate-600">
        Wire this page to the same seller onboarding API used by the mobile app (business details, KYC, bank
        payout).
      </p>
    </div>
  );
}
