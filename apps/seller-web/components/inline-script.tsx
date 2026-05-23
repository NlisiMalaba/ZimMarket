"use client";

/**
 * Inline script that runs on full page load (type=text/javascript) and is inert on
 * client hydration (type=text/plain). See Next.js "Preventing flash before hydration".
 */
export function InlineScript({ html }: { html: string }) {
  return (
    <script
      type={typeof window === "undefined" ? "text/javascript" : "text/plain"}
      suppressHydrationWarning
      dangerouslySetInnerHTML={{ __html: html }}
    />
  );
}
