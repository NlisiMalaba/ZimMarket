"use client";

import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";

import { ApiError, api } from "@/lib/api";

type ApiSuccessResponse<T> = {
  data: T;
};

type AdminListItem = {
  userId: string;
  email: string;
  fullName: string;
  isActive: boolean;
  createdAtUtc?: string;
};

export default function SettingsPage() {
  const [email, setEmail] = useState("");
  const [fullName, setFullName] = useState("");
  const [temporaryPassword, setTemporaryPassword] = useState("");
  const [admins, setAdmins] = useState<AdminListItem[]>([]);
  const [isLoadingAdmins, setIsLoadingAdmins] = useState(false);
  const [isCreatingAdmin, setIsCreatingAdmin] = useState(false);
  const [deactivatingAdminId, setDeactivatingAdminId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [listEndpointUnavailable, setListEndpointUnavailable] = useState(false);

  const sortedAdmins = useMemo(
    () =>
      [...admins].sort((a, b) => {
        if (a.isActive === b.isActive) {
          return a.email.localeCompare(b.email);
        }

        return a.isActive ? -1 : 1;
      }),
    [admins],
  );

  const loadAdmins = useCallback(async () => {
    setIsLoadingAdmins(true);

    try {
      const response = await api.get<ApiSuccessResponse<AdminListItem[]>>("/api/v1/admin/admins");
      setAdmins(response.data);
      setListEndpointUnavailable(false);
      setErrorMessage(null);
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        setListEndpointUnavailable(true);
        setAdmins([]);
        setErrorMessage(null);
      } else {
        setErrorMessage(error instanceof ApiError ? error.message : "Unable to load admin users.");
      }
    } finally {
      setIsLoadingAdmins(false);
    }
  }, []);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      void loadAdmins();
    }, 0);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [loadAdmins]);

  const createAdmin = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    setIsCreatingAdmin(true);
    setErrorMessage(null);
    setSuccessMessage(null);

    try {
      await api.post<ApiSuccessResponse<unknown>, { email: string; fullName: string; password: string }>(
        "/api/v1/admin/admins",
        {
          email: email.trim(),
          fullName: fullName.trim(),
          password: temporaryPassword,
        },
      );

      setSuccessMessage("Admin created successfully.");
      setEmail("");
      setFullName("");
      setTemporaryPassword("");
      await loadAdmins();
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to create admin.");
    } finally {
      setIsCreatingAdmin(false);
    }
  };

  const deactivateAdmin = async (admin: AdminListItem) => {
    setDeactivatingAdminId(admin.userId);
    setErrorMessage(null);
    setSuccessMessage(null);

    setAdmins((current) =>
      current.map((item) => (item.userId === admin.userId ? { ...item, isActive: false } : item)),
    );

    try {
      await api.post<ApiSuccessResponse<null>, undefined>(`/api/v1/admin/users/${admin.userId}/deactivate`);
      setSuccessMessage(`Deactivated ${admin.email}.`);
    } catch (error) {
      setAdmins((current) =>
        current.map((item) => (item.userId === admin.userId ? { ...item, isActive: admin.isActive } : item)),
      );
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to deactivate admin.");
    } finally {
      setDeactivatingAdminId(null);
    }
  };

  return (
    <section className="space-y-6">
      <header className="rounded-xl border bg-card p-6 shadow-sm">
        <h1 className="text-2xl font-semibold">Settings</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          SuperAdmin-only controls for administrator provisioning and account lifecycle.
        </p>
      </header>

      {errorMessage ? (
        <div className="rounded-md border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {errorMessage}
        </div>
      ) : null}

      {successMessage ? (
        <div className="rounded-md border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
          {successMessage}
        </div>
      ) : null}

      <section className="rounded-xl border bg-card p-4 shadow-sm">
        <h2 className="text-sm font-semibold">Create Admin</h2>
        <form className="mt-4 grid gap-3 md:grid-cols-2" onSubmit={createAdmin}>
          <input
            type="email"
            required
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            placeholder="Admin email"
            className="rounded-md border bg-background px-3 py-2 text-sm"
          />
          <input
            required
            value={fullName}
            onChange={(event) => setFullName(event.target.value)}
            placeholder="Full name"
            className="rounded-md border bg-background px-3 py-2 text-sm"
          />
          <input
            required
            value={temporaryPassword}
            onChange={(event) => setTemporaryPassword(event.target.value)}
            placeholder="Temporary password"
            className="rounded-md border bg-background px-3 py-2 text-sm md:col-span-2"
          />
          <button
            type="submit"
            disabled={isCreatingAdmin}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground disabled:opacity-50 md:w-fit"
          >
            {isCreatingAdmin ? "Creating..." : "Create Admin"}
          </button>
        </form>
      </section>

      <section className="rounded-xl border bg-card shadow-sm">
        <div className="flex items-center justify-between border-b px-4 py-3">
          <h2 className="text-sm font-semibold">Existing Admins</h2>
          <button
            type="button"
            className="rounded-md border px-3 py-1.5 text-sm"
            onClick={() => {
              void loadAdmins();
            }}
            disabled={isLoadingAdmins}
          >
            Refresh
          </button>
        </div>

        {listEndpointUnavailable ? (
          <p className="px-4 py-4 text-sm text-muted-foreground">
            Admin listing endpoint is not available yet (`GET /api/v1/admin/admins` returned 404). Create and
            deactivate actions are active.
          </p>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y">
              <thead>
                <tr className="text-left text-xs text-muted-foreground">
                  <th className="px-4 py-3 font-medium">Name</th>
                  <th className="px-4 py-3 font-medium">Email</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                  <th className="px-4 py-3 font-medium">Created</th>
                  <th className="px-4 py-3 font-medium">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y text-sm">
                {sortedAdmins.map((admin) => (
                  <tr key={admin.userId}>
                    <td className="px-4 py-3">{admin.fullName}</td>
                    <td className="px-4 py-3">{admin.email}</td>
                    <td className="px-4 py-3">
                      <span
                        className={`inline-flex rounded-full px-2 py-1 text-xs font-medium ${
                          admin.isActive
                            ? "bg-emerald-100 text-emerald-800"
                            : "bg-zinc-200 text-zinc-700"
                        }`}
                      >
                        {admin.isActive ? "Active" : "Inactive"}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      {admin.createdAtUtc ? new Date(admin.createdAtUtc).toLocaleString() : "N/A"}
                    </td>
                    <td className="px-4 py-3">
                      <button
                        type="button"
                        disabled={!admin.isActive || deactivatingAdminId === admin.userId}
                        className="rounded-md border border-destructive px-3 py-1.5 text-xs text-destructive disabled:opacity-50"
                        onClick={() => {
                          void deactivateAdmin(admin);
                        }}
                      >
                        {deactivatingAdminId === admin.userId ? "Deactivating..." : "Deactivate"}
                      </button>
                    </td>
                  </tr>
                ))}
                {!isLoadingAdmins && sortedAdmins.length === 0 ? (
                  <tr>
                    <td className="px-4 py-6 text-center text-muted-foreground" colSpan={5}>
                      No admin records returned.
                    </td>
                  </tr>
                ) : null}
                {isLoadingAdmins ? (
                  <tr>
                    <td className="px-4 py-6 text-center text-muted-foreground" colSpan={5}>
                      Loading admins...
                    </td>
                  </tr>
                ) : null}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </section>
  );
}
