"use client";

import { FormEvent, useState } from "react";

import { SELLER_KYC_FILE_TYPE } from "@/lib/seller-kyc";
import { submitSellerKycDocuments, uploadSellerKycDocument } from "@/lib/seller-kyc-upload";

type SellerKycFormProps = {
  onSubmitted: () => void;
  rejectionReason?: string | null;
};

export function SellerKycForm({ onSubmitted, rejectionReason }: SellerKycFormProps) {
  const [nationalIdFile, setNationalIdFile] = useState<File | null>(null);
  const [proofFile, setProofFile] = useState<File | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setErrorMessage(null);

    if (!nationalIdFile || !proofFile) {
      setErrorMessage("Please select both your national ID photo and proof of residence.");
      return;
    }

    setIsSubmitting(true);

    try {
      const nationalIdKey = await uploadSellerKycDocument(nationalIdFile, SELLER_KYC_FILE_TYPE.nationalId);
      const proofOfResidenceKey = await uploadSellerKycDocument(proofFile, SELLER_KYC_FILE_TYPE.proofOfResidence);

      await submitSellerKycDocuments({ nationalIdKey, proofOfResidenceKey });
      onSubmitted();
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : "Failed to submit KYC documents.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form className="space-y-6" onSubmit={onSubmit}>
      <p className="text-sm text-muted-foreground">
        Upload a clear photo of your national ID and a recent proof of residence (utility bill, bank statement, or
        lease). Both files are required before you can list products.
      </p>

      {rejectionReason ? (
        <div className="rounded-xl border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          <p className="font-medium">Previous rejection</p>
          <p className="mt-1">{rejectionReason}</p>
        </div>
      ) : null}

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2 rounded-xl border border-border/70 bg-card p-4">
          <label className="text-sm font-medium text-foreground" htmlFor="nationalId">
            National ID photo
          </label>
          <input
            id="nationalId"
            name="nationalId"
            type="file"
            accept="image/jpeg,image/png,image/webp"
            required
            className="block w-full text-sm text-muted-foreground file:mr-3 file:rounded-lg file:border-0 file:bg-foreground file:px-3 file:py-2 file:text-sm file:font-medium file:text-background"
            onChange={(event) => setNationalIdFile(event.target.files?.[0] ?? null)}
          />
          {nationalIdFile ? (
            <p className="text-xs text-muted-foreground">Selected: {nationalIdFile.name}</p>
          ) : null}
        </div>

        <div className="space-y-2 rounded-xl border border-border/70 bg-card p-4">
          <label className="text-sm font-medium text-foreground" htmlFor="proofOfResidence">
            Proof of residence
          </label>
          <input
            id="proofOfResidence"
            name="proofOfResidence"
            type="file"
            accept="image/jpeg,image/png,image/webp"
            required
            className="block w-full text-sm text-muted-foreground file:mr-3 file:rounded-lg file:border-0 file:bg-foreground file:px-3 file:py-2 file:text-sm file:font-medium file:text-background"
            onChange={(event) => setProofFile(event.target.files?.[0] ?? null)}
          />
          {proofFile ? <p className="text-xs text-muted-foreground">Selected: {proofFile.name}</p> : null}
        </div>
      </div>

      {errorMessage ? (
        <p className="rounded-xl border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {errorMessage}
        </p>
      ) : null}

      <button
        type="submit"
        disabled={isSubmitting}
        className="inline-flex h-10 items-center justify-center rounded-xl bg-foreground px-5 text-sm font-medium text-background hover:opacity-90 disabled:opacity-60"
      >
        {isSubmitting ? "Submitting documents…" : "Submit national ID and proof of residence"}
      </button>
    </form>
  );
}
