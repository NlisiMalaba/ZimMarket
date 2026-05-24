"use client";

import { useCallback, useEffect, useMemo, useState } from "react";

import { ApiError, api } from "@/lib/api";
import {
  getWarehouseQcStatusLabel,
  getWarehouseQcStatusValue,
  type WarehouseQcStatusName,
} from "@/lib/domain-enums";

type PagedList<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};

type WarehouseItem = {
  warehouseItemId: string;
  orderId: string;
  customerId: string;
  productId: string;
  arrivedAt: string;
  qcStatus: number | WarehouseQcStatusName;
  qcNotes?: string | null;
  batchId?: string | null;
  warehouseItemCreatedAt: string;
  orderStatus: string;
  orderPaymentStatus: string;
  orderTotalAmount: number;
  orderTotalCurrency: number | string;
  orderCreatedAt: string;
};

type DriverLocation = {
  driverId: string;
  latitude?: number | null;
  longitude?: number | null;
  updatedAtUtc?: string | null;
};

type TabKey = "arrival" | "qc" | "unbatched";

const tabOptions: Array<{ key: TabKey; label: string }> = [
  { key: "arrival", label: "Record Arrival" },
  { key: "qc", label: "QC Queue" },
  { key: "unbatched", label: "Unbatched Items" },
];

function shortId(value: string): string {
  return value.slice(0, 8);
}

function formatDateTime(value: string): string {
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? value : parsed.toLocaleString();
}

export default function WarehousePage() {
  const [activeTab, setActiveTab] = useState<TabKey>("arrival");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const [arrivalOrderId, setArrivalOrderId] = useState("");
  const [arrivalNotes, setArrivalNotes] = useState("");
  const [isSubmittingArrival, setIsSubmittingArrival] = useState(false);

  const [qcItems, setQcItems] = useState<WarehouseItem[]>([]);
  const [qcPage, setQcPage] = useState(1);
  const [qcTotalCount, setQcTotalCount] = useState(0);
  const [isLoadingQc, setIsLoadingQc] = useState(false);
  const [selectedQcItemId, setSelectedQcItemId] = useState<string | null>(null);
  const [qcNotes, setQcNotes] = useState("");
  const [isUpdatingQc, setIsUpdatingQc] = useState(false);

  const [unbatchedItems, setUnbatchedItems] = useState<WarehouseItem[]>([]);
  const [selectedOrderIds, setSelectedOrderIds] = useState<string[]>([]);
  const [drivers, setDrivers] = useState<DriverLocation[]>([]);
  const [selectedDriverId, setSelectedDriverId] = useState("");
  const [isLoadingUnbatched, setIsLoadingUnbatched] = useState(false);
  const [isCreatingBatch, setIsCreatingBatch] = useState(false);

  const qcTotalPages = Math.max(1, Math.ceil(qcTotalCount / 20));

  const selectedQcItem = useMemo(
    () => qcItems.find((item) => item.warehouseItemId === selectedQcItemId) ?? null,
    [qcItems, selectedQcItemId],
  );

  const loadQcItems = useCallback(async () => {
    setIsLoadingQc(true);

    try {
      const response = await api.get<PagedList<WarehouseItem>>("/api/v1/warehouse/items", {
        query: {
          qcStatus: "Pending",
          page: qcPage,
          pageSize: 20,
        },
      });

      setQcItems(response.items);
      setQcTotalCount(response.totalCount);
      setErrorMessage(null);
      setSelectedQcItemId((current) => current ?? response.items[0]?.warehouseItemId ?? null);
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Unable to load QC queue.");
    } finally {
      setIsLoadingQc(false);
    }
  }, [qcPage]);

  const loadUnbatched = useCallback(async () => {
    setIsLoadingUnbatched(true);

    try {
      const [itemsResponse, driversResponse] = await Promise.all([
        api.get<WarehouseItem[]>("/api/v1/warehouse/items/unbatched"),
        api.get<DriverLocation[]>("/api/v1/batches/drivers/locations"),
      ]);

      setUnbatchedItems(itemsResponse);
      setDrivers(driversResponse);
      setSelectedOrderIds((current) =>
        current.filter((orderId) => itemsResponse.some((item) => item.orderId === orderId)),
      );
      setSelectedDriverId((current) =>
        current && driversResponse.some((driver) => driver.driverId === current) ? current : "",
      );
      setErrorMessage(null);
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Unable to load unbatched items.");
    } finally {
      setIsLoadingUnbatched(false);
    }
  }, []);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      void loadQcItems();
      void loadUnbatched();
    }, 0);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [loadQcItems, loadUnbatched]);

  const submitArrival = async () => {
    if (!arrivalOrderId.trim()) {
      setErrorMessage("Order ID is required.");
      return;
    }

    setIsSubmittingArrival(true);

    try {
      await api.post<null, { orderId: string; notes?: string }>(
        "/api/v1/warehouse/arrivals",
        {
          orderId: arrivalOrderId.trim(),
          notes: arrivalNotes.trim() || undefined,
        },
      );

      setArrivalOrderId("");
      setArrivalNotes("");
      setSuccessMessage("Arrival recorded successfully.");
      setErrorMessage(null);
      await loadQcItems();
      await loadUnbatched();
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to record arrival.");
    } finally {
      setIsSubmittingArrival(false);
    }
  };

  const updateQcStatus = async (qcStatus: "Passed" | "Failed") => {
    if (!selectedQcItem) {
      setErrorMessage("Select a QC item first.");
      return;
    }

    setIsUpdatingQc(true);

    const itemId = selectedQcItem.warehouseItemId;
    setQcItems((current) => current.filter((item) => item.warehouseItemId !== itemId));
    setSelectedQcItemId(null);

    try {
      await api.patch<null, { qcStatus: number; notes?: string }>(
        `/api/v1/warehouse/items/${itemId}/qc`,
        {
          qcStatus: getWarehouseQcStatusValue(qcStatus),
          notes: qcNotes.trim() || undefined,
        },
      );

      setQcNotes("");
      setSuccessMessage(`QC marked as ${qcStatus.toLowerCase()}.`);
      setErrorMessage(null);
      await loadQcItems();
      await loadUnbatched();
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to update QC status.");
      await loadQcItems();
    } finally {
      setIsUpdatingQc(false);
    }
  };

  const toggleOrderSelection = (orderId: string) => {
    setSelectedOrderIds((current) =>
      current.includes(orderId) ? current.filter((id) => id !== orderId) : [...current, orderId],
    );
  };

  const createBatch = async () => {
    if (selectedOrderIds.length === 0) {
      setErrorMessage("Select at least one unbatched order.");
      return;
    }

    if (!selectedDriverId) {
      setErrorMessage("Select a driver for the batch.");
      return;
    }

    setIsCreatingBatch(true);

    try {
      await api.post<{ batchId: string }, { orderIds: string[]; driverId: string }>(
        "/api/v1/batches",
        {
          orderIds: selectedOrderIds,
          driverId: selectedDriverId,
        },
      );

      setSelectedOrderIds([]);
      setSelectedDriverId("");
      setSuccessMessage("Delivery batch created successfully.");
      setErrorMessage(null);
      await loadUnbatched();
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Failed to create batch.");
    } finally {
      setIsCreatingBatch(false);
    }
  };

  return (
    <section className="space-y-6">
      <header className="rounded-xl border bg-card p-6 shadow-sm">
        <h1 className="text-2xl font-semibold">Warehouse</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Record arrivals, process quality checks, and create delivery batches.
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

      <div className="flex flex-wrap gap-2">
        {tabOptions.map((tab) => (
          <button
            key={tab.key}
            type="button"
            className={`rounded-md border px-3 py-2 text-sm ${
              activeTab === tab.key ? "bg-primary text-primary-foreground" : "bg-background"
            }`}
            onClick={() => setActiveTab(tab.key)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {activeTab === "arrival" ? (
        <section className="rounded-xl border bg-card p-4 shadow-sm">
          <h2 className="text-sm font-semibold">Record Arrival</h2>
          <div className="mt-4 grid gap-3 md:grid-cols-2">
            <input
              value={arrivalOrderId}
              onChange={(event) => setArrivalOrderId(event.target.value)}
              placeholder="Order ID (GUID)"
              className="rounded-md border bg-background px-3 py-2 text-sm"
            />
            <input
              value={arrivalNotes}
              onChange={(event) => setArrivalNotes(event.target.value)}
              placeholder="Notes (optional)"
              className="rounded-md border bg-background px-3 py-2 text-sm"
            />
          </div>
          <button
            type="button"
            className="mt-4 rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground disabled:opacity-50"
            onClick={() => {
              void submitArrival();
            }}
            disabled={isSubmittingArrival}
          >
            {isSubmittingArrival ? "Submitting..." : "Submit Arrival"}
          </button>
        </section>
      ) : null}

      {activeTab === "qc" ? (
        <div className="grid gap-6 xl:grid-cols-[1.2fr_1fr]">
          <section className="rounded-xl border bg-card shadow-sm">
            <div className="border-b px-4 py-3">
              <h2 className="text-sm font-semibold">QC Queue (Pending)</h2>
            </div>
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y">
                <thead>
                  <tr className="text-left text-xs text-muted-foreground">
                    <th className="px-4 py-3 font-medium">Warehouse Item</th>
                    <th className="px-4 py-3 font-medium">Order</th>
                    <th className="px-4 py-3 font-medium">Product</th>
                    <th className="px-4 py-3 font-medium">Arrived</th>
                  </tr>
                </thead>
                <tbody className="divide-y text-sm">
                  {qcItems.map((item) => (
                    <tr
                      key={item.warehouseItemId}
                      className={`cursor-pointer hover:bg-muted/50 ${
                        selectedQcItemId === item.warehouseItemId ? "bg-muted/50" : ""
                      }`}
                      onClick={() => setSelectedQcItemId(item.warehouseItemId)}
                    >
                      <td className="px-4 py-3 font-mono text-xs">{shortId(item.warehouseItemId)}</td>
                      <td className="px-4 py-3 font-mono text-xs">{shortId(item.orderId)}</td>
                      <td className="px-4 py-3 font-mono text-xs">{shortId(item.productId)}</td>
                      <td className="px-4 py-3">{formatDateTime(item.arrivedAt)}</td>
                    </tr>
                  ))}
                  {!isLoadingQc && qcItems.length === 0 ? (
                    <tr>
                      <td className="px-4 py-6 text-center text-muted-foreground" colSpan={4}>
                        No pending QC items.
                      </td>
                    </tr>
                  ) : null}
                  {isLoadingQc ? (
                    <tr>
                      <td className="px-4 py-6 text-center text-muted-foreground" colSpan={4}>
                        Loading QC queue...
                      </td>
                    </tr>
                  ) : null}
                </tbody>
              </table>
            </div>
            <div className="flex items-center justify-between border-t px-4 py-3 text-sm">
              <p className="text-muted-foreground">
                Page {qcPage} of {qcTotalPages}
              </p>
              <div className="flex gap-2">
                <button
                  type="button"
                  className="rounded-md border px-3 py-1.5 disabled:opacity-50"
                  disabled={qcPage <= 1 || isLoadingQc}
                  onClick={() => setQcPage((current) => Math.max(1, current - 1))}
                >
                  Previous
                </button>
                <button
                  type="button"
                  className="rounded-md border px-3 py-1.5 disabled:opacity-50"
                  disabled={qcPage >= qcTotalPages || isLoadingQc}
                  onClick={() => setQcPage((current) => Math.min(qcTotalPages, current + 1))}
                >
                  Next
                </button>
              </div>
            </div>
          </section>

          <aside className="rounded-xl border bg-card p-4 shadow-sm">
            <h2 className="text-sm font-semibold">QC Decision</h2>
            {!selectedQcItem ? (
              <p className="mt-4 text-sm text-muted-foreground">Select an item from the queue.</p>
            ) : (
              <div className="mt-4 space-y-3 text-sm">
                <p>
                  <span className="font-medium">Order:</span>{" "}
                  <span className="font-mono text-xs">{selectedQcItem.orderId}</span>
                </p>
                <p>
                  <span className="font-medium">Product:</span>{" "}
                  <span className="font-mono text-xs">{selectedQcItem.productId}</span>
                </p>
                <textarea
                  value={qcNotes}
                  onChange={(event) => setQcNotes(event.target.value)}
                  placeholder="QC notes (optional)"
                  className="h-24 w-full rounded-md border bg-background px-3 py-2 text-sm outline-none ring-ring focus:ring-2"
                />
                <div className="flex gap-2">
                  <button
                    type="button"
                    className="rounded-md bg-emerald-600 px-3 py-2 text-sm font-medium text-white disabled:opacity-50"
                    onClick={() => {
                      void updateQcStatus("Passed");
                    }}
                    disabled={isUpdatingQc}
                  >
                    Pass
                  </button>
                  <button
                    type="button"
                    className="rounded-md bg-destructive px-3 py-2 text-sm font-medium text-white disabled:opacity-50"
                    onClick={() => {
                      void updateQcStatus("Failed");
                    }}
                    disabled={isUpdatingQc}
                  >
                    Fail
                  </button>
                </div>
              </div>
            )}
          </aside>
        </div>
      ) : null}

      {activeTab === "unbatched" ? (
        <section className="rounded-xl border bg-card p-4 shadow-sm">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <h2 className="text-sm font-semibold">Unbatched Items</h2>
            <button
              type="button"
              className="rounded-md border px-3 py-1.5 text-sm"
              onClick={() => {
                void loadUnbatched();
              }}
              disabled={isLoadingUnbatched}
            >
              Refresh
            </button>
          </div>

          <div className="mt-4 overflow-x-auto">
            <table className="min-w-full divide-y">
              <thead>
                <tr className="text-left text-xs text-muted-foreground">
                  <th className="px-4 py-3 font-medium">Select</th>
                  <th className="px-4 py-3 font-medium">Order</th>
                  <th className="px-4 py-3 font-medium">Customer</th>
                  <th className="px-4 py-3 font-medium">Product</th>
                  <th className="px-4 py-3 font-medium">QC</th>
                </tr>
              </thead>
              <tbody className="divide-y text-sm">
                {unbatchedItems.map((item) => (
                  <tr key={`${item.warehouseItemId}-${item.orderId}`}>
                    <td className="px-4 py-3">
                      <input
                        type="checkbox"
                        checked={selectedOrderIds.includes(item.orderId)}
                        onChange={() => toggleOrderSelection(item.orderId)}
                      />
                    </td>
                    <td className="px-4 py-3 font-mono text-xs">{shortId(item.orderId)}</td>
                    <td className="px-4 py-3 font-mono text-xs">{shortId(item.customerId)}</td>
                    <td className="px-4 py-3 font-mono text-xs">{shortId(item.productId)}</td>
                    <td className="px-4 py-3">{getWarehouseQcStatusLabel(item.qcStatus)}</td>
                  </tr>
                ))}
                {!isLoadingUnbatched && unbatchedItems.length === 0 ? (
                  <tr>
                    <td className="px-4 py-6 text-center text-muted-foreground" colSpan={5}>
                      No unbatched items ready for dispatch.
                    </td>
                  </tr>
                ) : null}
                {isLoadingUnbatched ? (
                  <tr>
                    <td className="px-4 py-6 text-center text-muted-foreground" colSpan={5}>
                      Loading unbatched items...
                    </td>
                  </tr>
                ) : null}
              </tbody>
            </table>
          </div>

          <div className="mt-4 grid gap-3 md:grid-cols-[1fr_auto]">
            <select
              value={selectedDriverId}
              onChange={(event) => setSelectedDriverId(event.target.value)}
              className="rounded-md border bg-background px-3 py-2 text-sm"
            >
              <option value="">Assign driver...</option>
              {drivers.map((driver) => (
                <option key={driver.driverId} value={driver.driverId}>
                  {driver.driverId}
                </option>
              ))}
            </select>
            <button
              type="button"
              className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground disabled:opacity-50"
              onClick={() => {
                void createBatch();
              }}
              disabled={isCreatingBatch}
            >
              {isCreatingBatch ? "Creating..." : "Create Batch"}
            </button>
          </div>
        </section>
      ) : null}
    </section>
  );
}
