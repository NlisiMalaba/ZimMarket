"use client";

import { SellerSettingsForm } from "@/components/settings/seller-settings-form";

export default function SellerSettingsPage() {
  return (
    <div className="mx-auto max-w-[900px] space-y-6">
      <div>
        <h1 className="text-3xl font-semibold tracking-tight text-foreground">Account settings</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Manage your business profile, password, photo, and default pickup address for new listings.
        </p>
      </div>

      <SellerSettingsForm />
    </div>
  );
}
