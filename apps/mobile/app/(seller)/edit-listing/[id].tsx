import { useLocalSearchParams } from 'expo-router';
import { router } from 'expo-router';
import { ActivityIndicator, StyleSheet } from 'react-native';
import { useQuery } from '@tanstack/react-query';

import { ListingFormScreen } from '@/app/(seller)/listing-form-screen';
import type { ListingFormValues } from '@/components/seller/listing-form';
import { Text, View } from '@/components/Themed';
import type { UploadableImage } from '@/lib/services/file-upload-service';
import { sellerProductsService } from '@/lib/services/seller-products-service';

const readId = (value: string | string[] | undefined): string => {
  if (Array.isArray(value)) {
    return value[0] ?? '';
  }

  return value ?? '';
};

export default function EditListingScreen() {
  const params = useLocalSearchParams();
  const listingId = readId(params.id);

  const productQuery = useQuery({
    queryKey: ['seller-product', listingId],
    queryFn: () => sellerProductsService.getProduct(listingId),
    enabled: listingId.length > 0,
  });

  if (productQuery.isLoading) {
    return (
      <View style={styles.stateContainer}>
        <ActivityIndicator size="large" color="#0f766e" />
      </View>
    );
  }

  if (productQuery.isError || !productQuery.data) {
    return (
      <View style={styles.stateContainer}>
        <Text style={styles.errorText}>Failed to load listing.</Text>
      </View>
    );
  }

  const product = productQuery.data;
  const previousStockQuantity = typeof product.stockQuantity === 'number' ? product.stockQuantity : undefined;

  const handleSubmit = async (values: ListingFormValues, images: UploadableImage[]) => {
    await sellerProductsService.updateListing({
      productId: listingId,
      title: values.title,
      description: values.description,
      priceUsd: values.priceUsd,
      categoryId: values.categoryId,
      pickupAddress: {
        street: values.street,
        suburb: values.suburb,
        city: values.city,
        country: values.country,
      },
      images,
      previousStockQuantity,
      nextStockQuantity: values.stockQuantity,
    });

    router.replace('/(seller)/listings' as never);
  };

  return (
    <ListingFormScreen
      mode="edit"
      submitLabel="Save changes"
      defaultValues={{
        title: product.title ?? '',
        description: product.description ?? '',
        priceUsd: product.priceUsd ?? 0,
        stockQuantity: previousStockQuantity ?? 0,
        categoryId: product.categoryId ?? '',
        street: product.pickupAddress?.street ?? '',
        suburb: product.pickupAddress?.suburb ?? '',
        city: product.pickupAddress?.city ?? '',
        country: product.pickupAddress?.country ?? 'Zimbabwe',
      }}
      initialImages={[]}
      onSubmit={handleSubmit}
    />
  );
}

const styles = StyleSheet.create({
  stateContainer: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
    gap: 12,
  },
  errorText: {
    color: '#dc2626',
    textAlign: 'center',
  },
});
