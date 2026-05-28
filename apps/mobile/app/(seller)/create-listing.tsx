import { router } from 'expo-router';

import { ListingFormScreen } from '@/app/(seller)/listing-form-screen';
import { KycGate } from '@/components/seller/kyc-gate';
import type { ListingFormValues } from '@/components/seller/listing-form';
import { isSellerKycApproved } from '@/lib/seller-kyc';
import type { UploadableImage } from '@/lib/services/file-upload-service';
import { sellerProductsService } from '@/lib/services/seller-products-service';
import { useAuthStore } from '@/store/auth-store';

export default function CreateListingScreen() {
  const kycStatus = useAuthStore((state) => state.kycStatus);

  if (!isSellerKycApproved(kycStatus)) {
    return <KycGate actionLabel="Complete verification" />;
  }

  const handleSubmit = async (values: ListingFormValues, images: UploadableImage[]) => {
    await sellerProductsService.createListing({
      title: values.title,
      description: values.description,
      priceUsd: values.priceUsd,
      categoryId: values.categoryId,
      stockQuantity: values.stockQuantity,
      pickupAddress: {
        street: values.street,
        suburb: values.suburb,
        city: values.city,
        country: values.country,
      },
      images,
    });

    router.replace('/(seller)/listings' as never);
  };

  return <ListingFormScreen mode="create" submitLabel="Create listing" onSubmit={handleSubmit} />;
}
