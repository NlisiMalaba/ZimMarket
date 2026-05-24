"use client";

import Image from "next/image";
import { FormEvent, useEffect, useMemo, useState } from "react";

import { uploadProfilePhoto } from "@/lib/profile-photo-upload";
import {
  changeSellerPassword,
  getSellerProfile,
  updateSellerProfile,
  type PickupAddress,
  type SellerProfile,
} from "@/lib/seller-settings";
import { setSessionProfile } from "@/lib/auth-session";
import { cn } from "@/lib/utils";

const defaultCountry = "Zimbabwe";

const emptyAddress: PickupAddress = {
  street: "",
  suburb: "",
  city: "",
  country: defaultCountry,
};

type ProfileFormState = {
  fullName: string;
  email: string;
  phone: string;
  businessName: string;
  profilePhotoKey: string | null;
  profilePhotoUrl: string | null;
  useDefaultPickupAddress: boolean;
  pickupAddress: PickupAddress;
};

function mapProfileToForm(profile: SellerProfile): ProfileFormState {
  const hasDefaultAddress = profile.defaultPickupAddress !== null;
  return {
    fullName: profile.fullName,
    email: profile.email,
    phone: profile.phone,
    businessName: profile.businessName,
    profilePhotoKey: profile.profilePhotoKey,
    profilePhotoUrl: profile.profilePhotoUrl,
    useDefaultPickupAddress: hasDefaultAddress,
    pickupAddress: profile.defaultPickupAddress ?? { ...emptyAddress },
  };
}

export function SellerSettingsForm() {
  const [form, setForm] = useState<ProfileFormState | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [profileMessage, setProfileMessage] = useState<string | null>(null);
  const [profileError, setProfileError] = useState<string | null>(null);
  const [isSavingProfile, setIsSavingProfile] = useState(false);
  const [isUploadingPhoto, setIsUploadingPhoto] = useState(false);

  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [passwordMessage, setPasswordMessage] = useState<string | null>(null);
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [isSavingPassword, setIsSavingPassword] = useState(false);

  useEffect(() => {
    let isMounted = true;

    const load = async () => {
      try {
        const profile = await getSellerProfile();
        if (isMounted) {
          setForm(mapProfileToForm(profile));
          setSessionProfile({
            fullName: profile.fullName,
            email: profile.email,
            profilePhotoUrl: profile.profilePhotoUrl,
          });
          setLoadError(null);
        }
      } catch (error) {
        if (isMounted) {
          setLoadError(error instanceof Error ? error.message : "Unable to load settings.");
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    };

    void load();

    return () => {
      isMounted = false;
    };
  }, []);

  const canSaveProfile = useMemo(() => {
    if (!form) {
      return false;
    }

    const addressValid =
      !form.useDefaultPickupAddress ||
      (form.pickupAddress.street.trim().length > 0 &&
        form.pickupAddress.suburb.trim().length > 0 &&
        form.pickupAddress.city.trim().length > 0);

    return (
      form.fullName.trim().length >= 2 &&
      form.email.trim().length > 0 &&
      form.phone.trim().length >= 8 &&
      form.businessName.trim().length >= 2 &&
      addressValid
    );
  }, [form]);

  const canSavePassword = useMemo(
    () =>
      currentPassword.length > 0 &&
      newPassword.length >= 8 &&
      confirmPassword === newPassword &&
      /[A-Z]/.test(newPassword) &&
      /\d/.test(newPassword),
    [confirmPassword, currentPassword, newPassword],
  );

  const updateForm = <K extends keyof ProfileFormState>(key: K, value: ProfileFormState[K]) => {
    setForm((current) => (current ? { ...current, [key]: value } : current));
  };

  const updateAddress = <K extends keyof PickupAddress>(key: K, value: PickupAddress[K]) => {
    setForm((current) =>
      current
        ? {
            ...current,
            pickupAddress: { ...current.pickupAddress, [key]: value },
          }
        : current,
    );
  };

  const onProfileSubmit = async (event: FormEvent) => {
    event.preventDefault();
    if (!form || !canSaveProfile) {
      return;
    }

    setProfileError(null);
    setProfileMessage(null);
    setIsSavingProfile(true);

    try {
      await updateSellerProfile({
        fullName: form.fullName.trim(),
        email: form.email.trim(),
        phone: form.phone.trim(),
        businessName: form.businessName.trim(),
        profilePhotoKey: form.profilePhotoKey,
        defaultPickupAddress: form.useDefaultPickupAddress
          ? {
              street: form.pickupAddress.street.trim(),
              suburb: form.pickupAddress.suburb.trim(),
              city: form.pickupAddress.city.trim(),
              country: form.pickupAddress.country.trim() || defaultCountry,
            }
          : null,
        clearDefaultPickupAddress: !form.useDefaultPickupAddress,
      });

      const refreshed = await getSellerProfile();
      const nextForm = mapProfileToForm(refreshed);
      setForm(nextForm);
      setSessionProfile({
        fullName: refreshed.fullName,
        email: refreshed.email,
        profilePhotoUrl: refreshed.profilePhotoUrl,
      });
      setProfileMessage("Account settings saved.");
    } catch (error) {
      setProfileError(error instanceof Error ? error.message : "Unable to save settings.");
    } finally {
      setIsSavingProfile(false);
    }
  };

  const onPhotoSelected = async (file: File | null) => {
    if (!file || !form) {
      return;
    }

    setProfileError(null);
    setIsUploadingPhoto(true);

    try {
      const fileKey = await uploadProfilePhoto(file);
      const previewUrl = URL.createObjectURL(file);
      updateForm("profilePhotoKey", fileKey);
      updateForm("profilePhotoUrl", previewUrl);
      setSessionProfile({
        fullName: form.fullName,
        email: form.email,
        profilePhotoUrl: previewUrl,
      });
      setProfileMessage("Profile photo updated.");
    } catch (error) {
      setProfileError(error instanceof Error ? error.message : "Unable to upload profile photo.");
    } finally {
      setIsUploadingPhoto(false);
    }
  };

  const onPasswordSubmit = async (event: FormEvent) => {
    event.preventDefault();
    if (!canSavePassword) {
      setPasswordError("Enter your current password and a valid new password.");
      return;
    }

    if (newPassword !== confirmPassword) {
      setPasswordError("New password and confirmation do not match.");
      return;
    }

    setPasswordError(null);
    setPasswordMessage(null);
    setIsSavingPassword(true);

    try {
      await changeSellerPassword({ currentPassword, newPassword });
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
      setPasswordMessage("Password updated. Sign in again on other devices if needed.");
    } catch (error) {
      setPasswordError(error instanceof Error ? error.message : "Unable to change password.");
    } finally {
      setIsSavingPassword(false);
    }
  };

  if (isLoading) {
    return <p className="text-sm text-muted-foreground">Loading account settings…</p>;
  }

  if (loadError || !form) {
    return (
      <p className="rounded-xl border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
        {loadError ?? "Unable to load settings."}
      </p>
    );
  }

  return (
    <div className="space-y-8">
      <form className="space-y-6 rounded-2xl border border-border/70 bg-card p-6 shadow-sm" onSubmit={onProfileSubmit}>
        <div>
          <h2 className="text-lg font-semibold text-foreground">Business profile</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            Contact details shown to buyers and used for your seller account.
          </p>
        </div>

        <div className="flex flex-col gap-4 sm:flex-row sm:items-center">
          <div className="relative size-20 shrink-0 overflow-hidden rounded-full bg-muted">
            {form.profilePhotoUrl ? (
              <Image
                src={form.profilePhotoUrl}
                alt="Profile"
                fill
                className="object-cover"
                unoptimized
              />
            ) : (
              <div className="flex size-full items-center justify-center text-lg font-semibold text-muted-foreground">
                {form.businessName.slice(0, 2).toUpperCase()}
              </div>
            )}
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium text-foreground" htmlFor="profilePhoto">
              Profile photo
            </label>
            <input
              id="profilePhoto"
              type="file"
              accept="image/jpeg,image/png,image/webp"
              disabled={isUploadingPhoto}
              className="block w-full text-sm text-muted-foreground file:mr-3 file:rounded-lg file:border-0 file:bg-foreground file:px-3 file:py-2 file:text-sm file:font-medium file:text-background"
              onChange={(event) => void onPhotoSelected(event.target.files?.[0] ?? null)}
            />
            <p className="text-xs text-muted-foreground">
              JPG, PNG, or WEBP up to 2 MB. Replacing a photo removes the previous one immediately.
            </p>
          </div>
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2 sm:col-span-2">
            <label className="text-sm font-medium" htmlFor="businessName">
              Business name
            </label>
            <input
              id="businessName"
              className="w-full rounded-xl border border-border bg-background px-3 py-2 text-sm"
              value={form.businessName}
              onChange={(event) => updateForm("businessName", event.target.value)}
              required
            />
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium" htmlFor="fullName">
              Contact name
            </label>
            <input
              id="fullName"
              className="w-full rounded-xl border border-border bg-background px-3 py-2 text-sm"
              value={form.fullName}
              onChange={(event) => updateForm("fullName", event.target.value)}
              required
            />
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium" htmlFor="phone">
              Contact phone
            </label>
            <input
              id="phone"
              className="w-full rounded-xl border border-border bg-background px-3 py-2 text-sm"
              value={form.phone}
              onChange={(event) => updateForm("phone", event.target.value)}
              required
            />
          </div>
          <div className="space-y-2 sm:col-span-2">
            <label className="text-sm font-medium" htmlFor="email">
              Email
            </label>
            <input
              id="email"
              type="email"
              className="w-full rounded-xl border border-border bg-background px-3 py-2 text-sm"
              value={form.email}
              onChange={(event) => updateForm("email", event.target.value)}
              required
            />
          </div>
        </div>

        <div className="space-y-4 border-t border-border/60 pt-6">
          <div className="flex items-center justify-between gap-4">
            <div>
              <h3 className="text-sm font-semibold text-foreground">Default pickup address</h3>
              <p className="text-xs text-muted-foreground">
                Pre-fills new product listings. Orders still use each product&apos;s pickup address.
              </p>
            </div>
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={form.useDefaultPickupAddress}
                onChange={(event) => updateForm("useDefaultPickupAddress", event.target.checked)}
              />
              Enabled
            </label>
          </div>

          {form.useDefaultPickupAddress ? (
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2 sm:col-span-2">
                <label className="text-sm font-medium" htmlFor="street">
                  Street
                </label>
                <input
                  id="street"
                  className="w-full rounded-xl border border-border bg-background px-3 py-2 text-sm"
                  value={form.pickupAddress.street}
                  onChange={(event) => updateAddress("street", event.target.value)}
                />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium" htmlFor="suburb">
                  Suburb
                </label>
                <input
                  id="suburb"
                  className="w-full rounded-xl border border-border bg-background px-3 py-2 text-sm"
                  value={form.pickupAddress.suburb}
                  onChange={(event) => updateAddress("suburb", event.target.value)}
                />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium" htmlFor="city">
                  City
                </label>
                <input
                  id="city"
                  className="w-full rounded-xl border border-border bg-background px-3 py-2 text-sm"
                  value={form.pickupAddress.city}
                  onChange={(event) => updateAddress("city", event.target.value)}
                />
              </div>
              <div className="space-y-2 sm:col-span-2">
                <label className="text-sm font-medium" htmlFor="country">
                  Country
                </label>
                <input
                  id="country"
                  className="w-full rounded-xl border border-border bg-background px-3 py-2 text-sm"
                  value={form.pickupAddress.country}
                  onChange={(event) => updateAddress("country", event.target.value)}
                />
              </div>
            </div>
          ) : null}
        </div>

        {profileError ? (
          <p className="text-sm text-destructive">{profileError}</p>
        ) : null}
        {profileMessage ? (
          <p className="text-sm font-medium text-emerald-600 dark:text-emerald-400">{profileMessage}</p>
        ) : null}

        <button
          type="submit"
          disabled={!canSaveProfile || isSavingProfile || isUploadingPhoto}
          className={cn(
            "rounded-xl bg-foreground px-4 py-2.5 text-sm font-semibold text-background transition-opacity",
            (!canSaveProfile || isSavingProfile || isUploadingPhoto) && "opacity-60",
          )}
        >
          {isSavingProfile ? "Saving…" : "Save account settings"}
        </button>
      </form>

      <form
        className="space-y-4 rounded-2xl border border-border/70 bg-card p-6 shadow-sm"
        onSubmit={onPasswordSubmit}
      >
        <div>
          <h2 className="text-lg font-semibold text-foreground">Password</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            Use at least 8 characters with one uppercase letter and one number.
          </p>
        </div>

        <div className="grid gap-4 sm:max-w-md">
          <div className="space-y-2">
            <label className="text-sm font-medium" htmlFor="currentPassword">
              Current password
            </label>
            <input
              id="currentPassword"
              type="password"
              autoComplete="current-password"
              className="w-full rounded-xl border border-border bg-background px-3 py-2 text-sm"
              value={currentPassword}
              onChange={(event) => setCurrentPassword(event.target.value)}
            />
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium" htmlFor="newPassword">
              New password
            </label>
            <input
              id="newPassword"
              type="password"
              autoComplete="new-password"
              className="w-full rounded-xl border border-border bg-background px-3 py-2 text-sm"
              value={newPassword}
              onChange={(event) => setNewPassword(event.target.value)}
            />
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium" htmlFor="confirmPassword">
              Confirm new password
            </label>
            <input
              id="confirmPassword"
              type="password"
              autoComplete="new-password"
              className="w-full rounded-xl border border-border bg-background px-3 py-2 text-sm"
              value={confirmPassword}
              onChange={(event) => setConfirmPassword(event.target.value)}
            />
          </div>
        </div>

        {passwordError ? <p className="text-sm text-destructive">{passwordError}</p> : null}
        {passwordMessage ? (
          <p className="text-sm font-medium text-emerald-600 dark:text-emerald-400">{passwordMessage}</p>
        ) : null}

        <button
          type="submit"
          disabled={!canSavePassword || isSavingPassword}
          className={cn(
            "rounded-xl border border-border px-4 py-2.5 text-sm font-semibold transition-opacity",
            (!canSavePassword || isSavingPassword) && "opacity-60",
          )}
        >
          {isSavingPassword ? "Updating…" : "Change password"}
        </button>
      </form>
    </div>
  );
}
