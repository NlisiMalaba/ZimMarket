"use client";

import { useCallback, useEffect, useMemo, useState } from "react";

import { ApiError, api } from "@/lib/api";

type UserRole = "Seller" | "Driver";

type ApiSuccessResponse<T> = {
  data: T;
};

type PagedList<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};

type KycDocumentSasDto = {
  storageKey: string;
  url: string;
  expiresAt: string;
};

type PendingKycQueueItemDto = {
  userId: string;
  email: string;
  fullName: string;
  role: UserRole;
  businessName?: string | null;
  licenseNumber?: string | null;
  vehicleRegistration?: string | null;
  nationalId?: KycDocumentSasDto | null;
  proofOfResidence?: KycDocumentSasDto | null;
  licenseDocument?: KycDocumentSasDto | null;
  vehicleDocument?: KycDocumentSasDto | null;
};

type KycReviewPageProps = {
  role: UserRole;
  title: string;
  description: string;
};

const defaultPageSize = 10;

export function KycReviewPage({ role, title, description }: KycReviewPageProps) {
  const [items, setItems] = useState<PendingKycQueueItemDto[]>([]);
  const [selectedItem, setSelectedItem] = useState<PendingKycQueueItemDto | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [isMutating, setIsMutating] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isRejectModalOpen, setIsRejectModalOpen] = useState(false);
  const [rejectReason, setRejectReason] = useState("");

  const totalPages = useMemo(() => Math.max(1, Math.ceil(totalCount / defaultPageSize)), [totalCount]);

  const loadPage = useCallback(async () => {
    setIsLoading(true);

    try {
      const response = await api.get<ApiSuccessResponse<PagedList<PendingKycQueueItemDto>>>("/api/v1/admin/kyc", {
        query: {
          role,
          page: currentPage,
          pageSize: defaultPageSize,
        },
      });

      setItems(response.data.items);
      setTotalCount(response.data.totalCount);
      setErrorMessage(null);

      if (response.data.items.length === 0) {
        setSelectedItem(null);
      } else if (!selectedItem) {
        setSelectedItem(response.data.items[0]);
      } else {
        const stillVisible = response.data.items.find((item) => item.userId === selectedItem.userId);
        setSelectedItem(stillVisible ?? response.data.items[0]);
      }
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Unable to load pending KYC submissions.");
    } finally {
      setIsLoading(false);
    }
  }, [currentPage, role, selectedItem]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      void loadPage();
    }, 0);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [loadPage]);

  const removeItemOptimistically = (userId: string) => {
    setItems((previous) => previous.filter((item) => item.userId !== userId));
    setTotalCount((previous) => Math.max(0, previous - 1));
    setSelectedItem((previous) => (previous?.userId === userId ? null : previous));
  };

  const approve = async () => {
    if (!selectedItem || isMutating) {
      return;
    }

    const target = selectedItem;
    removeItemOptimistically(target.userId);
    setIsMutating(true);

    try {
      await api.post<ApiSuccessResponse<null>, { role: UserRole }>(
        `/api/v1/admin/kyc/${target.userId}/approve`,
        { role },
      );
      setErrorMessage(null);
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to approve KYC submission.");
      await loadPage();
    } finally {
      setIsMutating(false);
    }
  };

  const reject = async () => {
    if (!selectedItem || isMutating) {
      return;
    }

    if (!rejectReason.trim()) {
      setErrorMessage("Rejection reason is required.");
      return;
    }

    const target = selectedItem;
    const reason = rejectReason.trim();
    removeItemOptimistically(target.userId);
    setIsRejectModalOpen(false);
    setRejectReason("");
    setIsMutating(true);

    try {
      await api.post<ApiSuccessResponse<null>, { role: UserRole; reason: string }>(
        `/api/v1/admin/kyc/${target.userId}/reject`,
        { role, reason },
      );
      setErrorMessage(null);
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to reject KYC submission.");
      await loadPage();
    } finally {
      setIsMutating(false);
    }
  };

  const documents = selectedItem
    ? [
        { label: "National ID", doc: selectedItem.nationalId },
        { label: "Proof of Residence", doc: selectedItem.proofOfResidence },
        { label: "License Document", doc: selectedItem.licenseDocument },
        { label: "Vehicle Document", doc: selectedItem.vehicleDocument },
      ].filter((entry) => Boolean(entry.doc))
    : [];

  return (
    <section className="space-y-6">
      <header className="rounded-xl border bg-card p-6 shadow-sm">
        <h1 className="text-2xl font-semibold">{title}</h1>
        <p className="mt-2 text-sm text-muted-foreground">{description}</p>
      </header>

      {errorMessage ? (
        <div className="rounded-md border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {errorMessage}
        </div>
      ) : null}

      <div className="grid gap-6 xl:grid-cols-[1.2fr_1fr]">
        <section className="rounded-xl border bg-card shadow-sm">
          <div className="border-b px-4 py-3">
            <h2 className="text-sm font-semibold">Pending Submissions</h2>
          </div>
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y">
              <thead>
                <tr className="text-left text-xs text-muted-foreground">
                  <th className="px-4 py-3 font-medium">Name</th>
                  <th className="px-4 py-3 font-medium">Email</th>
                  <th className="px-4 py-3 font-medium">Role</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y text-sm">
                {items.map((item) => (
                  <tr
                    key={item.userId}
                    className={`cursor-pointer transition-colors hover:bg-muted/50 ${
                      selectedItem?.userId === item.userId ? "bg-muted/50" : ""
                    }`}
                    onClick={() => setSelectedItem(item)}
                  >
                    <td className="px-4 py-3">{item.fullName}</td>
                    <td className="px-4 py-3">{item.email}</td>
                    <td className="px-4 py-3">{item.role}</td>
                    <td className="px-4 py-3">
                      <span className="inline-flex rounded-full bg-amber-100 px-2 py-1 text-xs font-medium text-amber-800">
                        Pending Review
                      </span>
                    </td>
                  </tr>
                ))}
                {!isLoading && items.length === 0 ? (
                  <tr>
                    <td className="px-4 py-6 text-center text-muted-foreground" colSpan={4}>
                      No pending submissions.
                    </td>
                  </tr>
                ) : null}
                {isLoading ? (
                  <tr>
                    <td className="px-4 py-6 text-center text-muted-foreground" colSpan={4}>
                      Loading pending submissions...
                    </td>
                  </tr>
                ) : null}
              </tbody>
            </table>
          </div>
          <div className="flex items-center justify-between border-t px-4 py-3 text-sm">
            <p className="text-muted-foreground">
              Page {currentPage} of {totalPages}
            </p>
            <div className="flex gap-2">
              <button
                type="button"
                className="rounded-md border px-3 py-1.5 disabled:opacity-50"
                disabled={currentPage <= 1 || isLoading}
                onClick={() => setCurrentPage((page) => Math.max(1, page - 1))}
              >
                Previous
              </button>
              <button
                type="button"
                className="rounded-md border px-3 py-1.5 disabled:opacity-50"
                disabled={currentPage >= totalPages || isLoading}
                onClick={() => setCurrentPage((page) => Math.min(totalPages, page + 1))}
              >
                Next
              </button>
            </div>
          </div>
        </section>

        <aside className="rounded-xl border bg-card p-4 shadow-sm">
          <h2 className="text-sm font-semibold">Submission Details</h2>

          {!selectedItem ? (
            <p className="mt-4 text-sm text-muted-foreground">Select a submission to review documents.</p>
          ) : (
            <div className="mt-4 space-y-4">
              <div className="space-y-1 text-sm">
                <p>
                  <span className="font-medium">Name:</span> {selectedItem.fullName}
                </p>
                <p>
                  <span className="font-medium">Email:</span> {selectedItem.email}
                </p>
                {selectedItem.businessName ? (
                  <p>
                    <span className="font-medium">Business:</span> {selectedItem.businessName}
                  </p>
                ) : null}
                {selectedItem.licenseNumber ? (
                  <p>
                    <span className="font-medium">License No:</span> {selectedItem.licenseNumber}
                  </p>
                ) : null}
                {selectedItem.vehicleRegistration ? (
                  <p>
                    <span className="font-medium">Vehicle Registration:</span> {selectedItem.vehicleRegistration}
                  </p>
                ) : null}
              </div>

              <div className="space-y-3">
                {documents.map(({ label, doc }) => (
                  <details key={label} className="rounded-md border p-2">
                    <summary className="cursor-pointer text-sm font-medium">{label}</summary>
                    {doc ? (
                      <iframe
                        title={`${label} preview`}
                        src={doc.url}
                        className="mt-2 h-64 w-full rounded-md border"
                      />
                    ) : null}
                  </details>
                ))}
              </div>

              <div className="flex gap-2 pt-2">
                <button
                  type="button"
                  className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground disabled:opacity-50"
                  disabled={isMutating}
                  onClick={approve}
                >
                  Approve
                </button>
                <button
                  type="button"
                  className="rounded-md border border-destructive px-3 py-2 text-sm font-medium text-destructive disabled:opacity-50"
                  disabled={isMutating}
                  onClick={() => setIsRejectModalOpen(true)}
                >
                  Reject
                </button>
              </div>
            </div>
          )}
        </aside>
      </div>

      {isRejectModalOpen ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="w-full max-w-md rounded-xl border bg-card p-4 shadow-lg">
            <h3 className="text-base font-semibold">Reject KYC Submission</h3>
            <p className="mt-1 text-sm text-muted-foreground">
              Provide a reason that will be recorded for this rejection.
            </p>
            <textarea
              className="mt-3 h-28 w-full rounded-md border bg-background px-3 py-2 text-sm outline-none ring-ring focus:ring-2"
              value={rejectReason}
              onChange={(event) => setRejectReason(event.target.value)}
              placeholder="Enter rejection reason..."
            />
            <div className="mt-4 flex justify-end gap-2">
              <button
                type="button"
                className="rounded-md border px-3 py-2 text-sm"
                onClick={() => {
                  setIsRejectModalOpen(false);
                  setRejectReason("");
                }}
              >
                Cancel
              </button>
              <button
                type="button"
                className="rounded-md bg-destructive px-3 py-2 text-sm font-medium text-white"
                onClick={reject}
              >
                Confirm Reject
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </section>
  );
}
