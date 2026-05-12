import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";

import { SellerFooter } from "@/components/seller-footer";
import { SellerHeader } from "@/components/seller-header";

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
    default: "ZimMarket — Sellers",
    template: "%s · ZimMarket Sellers",
  },
  description: "List products, manage orders, and grow your business on ZimMarket.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}>
      <body className="min-h-full flex flex-col bg-slate-50 text-slate-900">
        <SellerHeader />
        <main className="flex-1">{children}</main>
        <SellerFooter />
      </body>
    </html>
  );
}
