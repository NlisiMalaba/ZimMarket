"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { GoogleMap, InfoWindow, MarkerF, useJsApiLoader } from "@react-google-maps/api";
import * as signalR from "@microsoft/signalr";

import { ApiError, api } from "@/lib/api";
import { getAccessToken } from "@/lib/auth-session";
import { env } from "@/lib/env";

type DriverLocation = {
  driverId: string;
  latitude?: number | null;
  longitude?: number | null;
  updatedAtUtc?: string | null;
};

type DeliveryBatchStatus =
  | "Created"
  | "Assigned"
  | "OutForDelivery"
  | "Collected"
  | "Completed"
  | "Cancelled";

type DeliveryBatchListItemDto = {
  batchId: string;
  driverId: string;
  warehouseId: string;
  status: DeliveryBatchStatus;
  orderCount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
};

type PagedList<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};

type DriverMapInfo = {
  name: string;
  currentBatchId: string | null;
  ordersCount: number;
};

const mapContainerStyle = {
  width: "100%",
  height: "100%",
};

const defaultCenter = {
  lat: -17.8252,
  lng: 31.0335,
};

function buildTrackingHubUrl(): string {
  const apiUrl = env.apiUrl.endsWith("/") ? env.apiUrl.slice(0, -1) : env.apiUrl;
  return `${apiUrl}/hubs/tracking`;
}

export function DriversLiveMap() {
  const [locations, setLocations] = useState<Record<string, DriverLocation>>({});
  const [driverInfo, setDriverInfo] = useState<Record<string, DriverMapInfo>>({});
  const [selectedDriverId, setSelectedDriverId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [connectionState, setConnectionState] = useState<"connecting" | "connected" | "disconnected">(
    "connecting",
  );

  const googleMapsApiKey = process.env.NEXT_PUBLIC_GOOGLE_MAPS_API_KEY;
  const { isLoaded: isMapLoaded, loadError } = useJsApiLoader({
    id: "zimmarket-admin-map-script",
    googleMapsApiKey: googleMapsApiKey ?? "",
  });

  const loadInitialData = useCallback(async () => {
    try {
      const [locationsResponse, batchesResponse] = await Promise.all([
        api.get<DriverLocation[]>("/api/v1/batches/drivers/locations"),
        api.get<PagedList<DeliveryBatchListItemDto>>("/api/v1/batches", {
          query: {
            page: 1,
            pageSize: 100,
          },
        }),
      ]);

      const nextLocations: Record<string, DriverLocation> = {};
      for (const location of locationsResponse) {
        nextLocations[location.driverId] = location;
      }
      setLocations(nextLocations);

      const activeStatuses = new Set<DeliveryBatchStatus>([
        "Created",
        "Assigned",
        "Collected",
        "OutForDelivery",
      ]);
      const nextInfo: Record<string, DriverMapInfo> = {};

      for (const location of locationsResponse) {
        nextInfo[location.driverId] = {
          name: `Driver ${location.driverId.slice(0, 8)}`,
          currentBatchId: null,
          ordersCount: 0,
        };
      }

      for (const batch of batchesResponse.items) {
        if (!activeStatuses.has(batch.status)) {
          continue;
        }

        nextInfo[batch.driverId] = {
          name: `Driver ${batch.driverId.slice(0, 8)}`,
          currentBatchId: batch.batchId,
          ordersCount: batch.orderCount,
        };
      }

      setDriverInfo(nextInfo);
      setErrorMessage(null);
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Unable to load driver map data.");
    }
  }, []);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      void loadInitialData();
    }, 0);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [loadInitialData]);

  useEffect(() => {
    const hubUrl = buildTrackingHubUrl();

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        // Hub auth is JWT bearer based (token is read from access_token query on /hubs/*).
        accessTokenFactory: () => getAccessToken() ?? "",
        withCredentials: false,
      })
      .withAutomaticReconnect()
      .build();

    connection.on("LocationUpdated", (driverId: string, latitude: number, longitude: number, timestampUtc: string) => {
      setLocations((current) => ({
        ...current,
        [driverId]: {
          driverId,
          latitude,
          longitude,
          updatedAtUtc: timestampUtc,
        },
      }));

      setDriverInfo((current) => ({
        ...current,
        [driverId]: current[driverId] ?? {
          name: `Driver ${driverId.slice(0, 8)}`,
          currentBatchId: null,
          ordersCount: 0,
        },
      }));
    });

    const start = async () => {
      try {
        setConnectionState("connecting");
        await connection.start();
        await connection.invoke("SubscribeToAdminMap");
        setConnectionState("connected");
      } catch (error) {
        setConnectionState("disconnected");
        setErrorMessage(
          error instanceof Error ? `Live tracking connection failed: ${error.message}` : "Live tracking connection failed.",
        );
      }
    };

    void start();

    return () => {
      void connection.stop();
    };
  }, []);

  const markers = useMemo(
    () =>
      Object.values(locations).filter(
        (location): location is DriverLocation & { latitude: number; longitude: number } =>
          typeof location.latitude === "number" && typeof location.longitude === "number",
      ),
    [locations],
  );

  const selectedLocation = selectedDriverId ? locations[selectedDriverId] : null;
  const selectedInfo = selectedDriverId ? driverInfo[selectedDriverId] : null;

  if (!googleMapsApiKey) {
    return (
      <div className="rounded-xl border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
        Missing `NEXT_PUBLIC_GOOGLE_MAPS_API_KEY` environment variable.
      </div>
    );
  }

  if (loadError) {
    return (
      <div className="rounded-xl border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
        Failed to load Google Maps.
      </div>
    );
  }

  return (
    <section className="space-y-4">
      <div className="rounded-xl border bg-card p-4 shadow-sm">
        <h1 className="text-2xl font-semibold">Drivers Live Map</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          SignalR status: <span className="font-medium">{connectionState}</span>
        </p>
      </div>

      {errorMessage ? (
        <div className="rounded-md border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {errorMessage}
        </div>
      ) : null}

      <div className="grid gap-6 xl:grid-cols-[1.5fr_1fr]">
        <div className="h-[560px] overflow-hidden rounded-xl border bg-card shadow-sm">
          {isMapLoaded ? (
            <GoogleMap mapContainerStyle={mapContainerStyle} center={defaultCenter} zoom={11}>
              {markers.map((marker) => (
                <MarkerF
                  key={marker.driverId}
                  position={{ lat: marker.latitude, lng: marker.longitude }}
                  onClick={() => setSelectedDriverId(marker.driverId)}
                />
              ))}
              {selectedLocation &&
              typeof selectedLocation.latitude === "number" &&
              typeof selectedLocation.longitude === "number" ? (
                <InfoWindow
                  position={{ lat: selectedLocation.latitude, lng: selectedLocation.longitude }}
                  onCloseClick={() => setSelectedDriverId(null)}
                >
                  <div className="space-y-1 text-xs">
                    <p className="font-semibold">{selectedInfo?.name ?? selectedDriverId}</p>
                    <p>Driver ID: {selectedDriverId}</p>
                    <p>Last update: {selectedLocation.updatedAtUtc ?? "N/A"}</p>
                  </div>
                </InfoWindow>
              ) : null}
            </GoogleMap>
          ) : (
            <div className="flex h-full items-center justify-center text-sm text-muted-foreground">
              Loading map...
            </div>
          )}
        </div>

        <aside className="rounded-xl border bg-card p-4 shadow-sm">
          <h2 className="text-sm font-semibold">Driver Info</h2>
          {!selectedDriverId ? (
            <p className="mt-4 text-sm text-muted-foreground">Click a marker to view driver details.</p>
          ) : (
            <div className="mt-4 space-y-2 text-sm">
              <p>
                <span className="font-medium">Name:</span> {selectedInfo?.name ?? `Driver ${selectedDriverId.slice(0, 8)}`}
              </p>
              <p>
                <span className="font-medium">Driver ID:</span>{" "}
                <span className="font-mono text-xs">{selectedDriverId}</span>
              </p>
              <p>
                <span className="font-medium">Current Batch:</span>{" "}
                {selectedInfo?.currentBatchId ? (
                  <span className="font-mono text-xs">{selectedInfo.currentBatchId}</span>
                ) : (
                  "Unassigned"
                )}
              </p>
              <p>
                <span className="font-medium">Orders Count:</span> {selectedInfo?.ordersCount ?? 0}
              </p>
              <p>
                <span className="font-medium">Last Location Update:</span>{" "}
                {selectedLocation?.updatedAtUtc ? new Date(selectedLocation.updatedAtUtc).toLocaleString() : "N/A"}
              </p>
            </div>
          )}
        </aside>
      </div>
    </section>
  );
}
