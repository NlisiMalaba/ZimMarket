import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";

import { DriverFooter } from "@/components/driver-footer";
import { DriverHeader } from "@/components/driver-header";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: {
    default: "ZimMarket — Drivers",
    template: "%s · ZimMarket Drivers",
  },
  description: "Deliver with ZimMarket: onboarding, documents, and earnings.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}>
      <body className="min-h-full flex flex-col bg-emerald-50/40 text-neutral-900">
        <DriverHeader />
        <main className="flex-1">{children}</main>
        <DriverFooter />
      </body>
    </html>
  );
}
