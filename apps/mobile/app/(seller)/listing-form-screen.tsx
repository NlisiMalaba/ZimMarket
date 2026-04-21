import { ActivityIndicator, StyleSheet } from 'react-native';
import { useQuery } from '@tanstack/react-query';

import { ListingForm, type ListingFormValues } from '@/components/seller/listing-form';
import { Text, View } from '@/components/Themed';
import type { UploadableImage } from '@/lib/services/file-upload-service';
import { sellerProductsService } from '@/lib/services/seller-products-service';

export type ListingFormScreenProps = {
  mode: 'create' | 'edit';
  submitLabel: string;
  defaultValues?: Partial<ListingFormValues>;
  initialImages?: UploadableImage[];
  onSubmit: (values: ListingFormValues, images: UploadableImage[]) => Promise<void>;
};

export const ListingFormScreen = ({
  mode,
  submitLabel,
  defaultValues,
  initialImages,
  onSubmit,
}: ListingFormScreenProps) => {
  const categoriesQuery = useQuery({
    queryKey: ['categories'],
    queryFn: () => sellerProductsService.listCategories(),
  });

  if (categoriesQuery.isLoading) {
    return (
      <View style={styles.stateContainer}>
        <ActivityIndicator size="large" color="#0f766e" />
      </View>
    );
  }

  if (categoriesQuery.isError) {
    return (
      <View style={styles.stateContainer}>
        <Text style={styles.errorText}>Failed to load categories.</Text>
      </View>
    );
  }

  return (
    <ListingForm
      mode={mode}
      categories={categoriesQuery.data ?? []}
      defaultValues={defaultValues}
      initialImages={initialImages}
      submitLabel={submitLabel}
      onSubmit={onSubmit}
    />
  );
};

const styles = StyleSheet.create({
  stateContainer: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
  },
  errorText: {
    color: '#dc2626',
    textAlign: 'center',
  },
});

