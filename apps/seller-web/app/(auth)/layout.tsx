import { SellerFooter } from "@/components/seller-footer";
import { SellerHeader } from "@/components/seller-header";

export default function AuthLayout({ children }: { children: React.ReactNode }) {
  return (
    <>
      <SellerHeader />
      <main className="flex-1">{children}</main>
      <SellerFooter />
    </>
  );
}
