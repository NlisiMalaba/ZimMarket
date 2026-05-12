export const metadata = {
  title: "Register as a driver",
};

export default function DriverRegisterPage() {
  return (
    <div className="mx-auto max-w-lg px-4 py-12 sm:px-6">
      <h1 className="text-2xl font-semibold text-emerald-950">Driver application</h1>
      <p className="mt-2 text-neutral-700">
        Connect this flow to the driver onboarding API (vehicle, license, KYC uploads) used by the mobile app.
      </p>
    </div>
  );
}
