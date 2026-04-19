export type ToastVariant = "info" | "success" | "warning" | "error";

export type ToastPayload = {
  message: string;
  variant?: ToastVariant;
};

export const APP_TOAST_EVENT = "app:toast";

export function showToast(payload: ToastPayload): void {
  if (typeof window !== "undefined") {
    window.dispatchEvent(new CustomEvent<ToastPayload>(APP_TOAST_EVENT, { detail: payload }));
    return;
  }

  // Falls back during non-browser execution (SSR/tests) where window is unavailable.
  console.warn(`[toast:${payload.variant ?? "info"}] ${payload.message}`);
}
