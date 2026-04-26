import { useMemo, useState } from 'react';
import { Image, Pressable, ScrollView, StyleSheet, TextInput } from 'react-native';
import { Controller, useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import * as ImagePicker from 'expo-image-picker';

import { Text, View } from '@/components/Themed';
import type { Category } from '@/types/catalogue';
import type { UploadableImage } from '@/lib/services/file-upload-service';

const schema = z.object({
  title: z.string().trim().min(3, 'Title is required.').max(200, 'Title is too long.'),
  description: z.string().trim().min(10, 'Description must be at least 10 characters.').max(4000),
  priceUsd: z
    .number({ message: 'Enter a valid price.' })
    .positive('Price must be greater than 0.')
    .max(1_000_000),
  categoryId: z.string().min(1, 'Select a category.'),
  stockQuantity: z
    .number({ message: 'Enter a valid stock amount.' })
    .int('Stock must be a whole number.')
    .min(0, 'Stock cannot be negative.')
    .max(1_000_000),
  street: z.string().trim().min(2, 'Street is required.').max(200),
  suburb: z.string().trim().min(2, 'Suburb is required.').max(200),
  city: z.string().trim().min(2, 'City is required.').max(200),
  country: z.string().trim().min(2, 'Country is required.').max(200),
});

export type ListingFormValues = z.infer<typeof schema>;

export type ListingFormProps = {
  mode: 'create' | 'edit';
  categories: Category[];
  defaultValues?: Partial<ListingFormValues>;
  initialImages?: UploadableImage[];
  submitLabel: string;
  onSubmit: (values: ListingFormValues, images: UploadableImage[]) => Promise<void>;
};

const resolveMimeType = (value: string | null | undefined): UploadableImage['contentType'] | null => {
  if (value === 'image/jpeg' || value === 'image/png' || value === 'image/webp') {
    return value;
  }

  return null;
};

const pickImages = async (remainingSlots: number): Promise<UploadableImage[]> => {
  const permission = await ImagePicker.requestMediaLibraryPermissionsAsync();
  if (!permission.granted) {
    throw new Error('Photo library permission is required to select product images.');
  }

  const result = await ImagePicker.launchImageLibraryAsync({
    allowsEditing: false,
    mediaTypes: ImagePicker.MediaTypeOptions.Images,
    quality: 0.9,
    allowsMultipleSelection: true,
    selectionLimit: remainingSlots,
  });

  if (result.canceled) {
    return [];
  }

  const images: UploadableImage[] = [];
  for (const asset of result.assets) {
    const mimeType = resolveMimeType(asset.mimeType);
    if (!mimeType) {
      throw new Error('Only JPG, PNG, and WEBP images are supported.');
    }

    if (!asset.fileSize || asset.fileSize <= 0) {
      throw new Error('Unable to determine image size. Please choose another image.');
    }

    images.push({
      uri: asset.uri,
      contentType: mimeType,
      fileSizeBytes: asset.fileSize,
    });
  }

  return images;
};

const CategoryPicker = ({
  value,
  categories,
  onChange,
}: {
  value: string;
  categories: Category[];
  onChange: (value: string) => void;
}) => (
  <View style={styles.categoryGrid}>
    {categories.map((category) => {
      const selected = category.id === value;
      return (
        <Pressable
          key={category.id}
          style={[styles.categoryPill, selected ? styles.categoryPillSelected : null]}
          onPress={() => onChange(category.id)}
        >
          <Text style={[styles.categoryPillText, selected ? styles.categoryPillTextSelected : null]}>
            {category.name}
          </Text>
        </Pressable>
      );
    })}
  </View>
);

export const ListingForm = ({
  mode,
  categories,
  defaultValues,
  initialImages,
  submitLabel,
  onSubmit,
}: ListingFormProps) => {
  const [images, setImages] = useState<UploadableImage[]>(initialImages ?? []);
  const [screenError, setScreenError] = useState<string | null>(null);

  const form = useForm<ListingFormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: '',
      description: '',
      priceUsd: 0,
      categoryId: '',
      stockQuantity: 0,
      street: '',
      suburb: '',
      city: '',
      country: 'Zimbabwe',
      ...defaultValues,
    },
    mode: 'onSubmit',
  });

  const canPickMore = images.length < 5;
  const isSubmitting = form.formState.isSubmitting;
  const submitDisabled = isSubmitting || images.length === 0;

  const priceValue = form.watch('priceUsd');
  const formattedPrice = useMemo(() => {
    if (typeof priceValue !== 'number' || Number.isNaN(priceValue)) {
      return '';
    }

    return String(priceValue);
  }, [priceValue]);

  const stockValue = form.watch('stockQuantity');
  const formattedStock = useMemo(() => {
    if (typeof stockValue !== 'number' || Number.isNaN(stockValue)) {
      return '';
    }

    return String(stockValue);
  }, [stockValue]);

  const handleAddImages = async () => {
    setScreenError(null);
    try {
      const remaining = Math.max(0, 5 - images.length);
      const picked = await pickImages(remaining);
      if (picked.length > 0) {
        setImages((prev) => [...prev, ...picked].slice(0, 5));
      }
    } catch (error) {
      setScreenError(error instanceof Error ? error.message : 'Failed to select images.');
    }
  };

  const handleRemoveImage = (index: number) => {
    setImages((prev) => prev.filter((_, current) => current !== index));
  };

  const submit = form.handleSubmit(async (values) => {
    setScreenError(null);

    if (images.length === 0) {
      setScreenError('Please add at least one product image.');
      return;
    }

    try {
      await onSubmit(values, images);
    } catch (error) {
      setScreenError(error instanceof Error ? error.message : 'Failed to save listing.');
    }
  });

  return (
    <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
      <Text style={styles.sectionTitle}>Listing details</Text>

      <Controller
        control={form.control}
        name="title"
        render={({ field }) => (
          <TextInput
            style={styles.input}
            placeholder="Title"
            value={field.value}
            onChangeText={field.onChange}
            onBlur={field.onBlur}
          />
        )}
      />
      {form.formState.errors.title ? <Text style={styles.error}>{form.formState.errors.title.message}</Text> : null}

      <Controller
        control={form.control}
        name="description"
        render={({ field }) => (
          <TextInput
            style={[styles.input, styles.textarea]}
            placeholder="Description"
            value={field.value}
            onChangeText={field.onChange}
            onBlur={field.onBlur}
            multiline
          />
        )}
      />
      {form.formState.errors.description ? (
        <Text style={styles.error}>{form.formState.errors.description.message}</Text>
      ) : null}

      <Controller
        control={form.control}
        name="priceUsd"
        render={({ field }) => (
          <TextInput
            style={styles.input}
            placeholder="Price (USD)"
            keyboardType="decimal-pad"
            value={formattedPrice}
            onChangeText={(text) => field.onChange(Number(text.replace(',', '.')))}
            onBlur={field.onBlur}
          />
        )}
      />
      {form.formState.errors.priceUsd ? (
        <Text style={styles.error}>{form.formState.errors.priceUsd.message}</Text>
      ) : null}

      <Text style={styles.subheading}>Category</Text>
      <Controller
        control={form.control}
        name="categoryId"
        render={({ field }) => <CategoryPicker value={field.value} categories={categories} onChange={field.onChange} />}
      />
      {form.formState.errors.categoryId ? (
        <Text style={styles.error}>{form.formState.errors.categoryId.message}</Text>
      ) : null}

      <Controller
        control={form.control}
        name="stockQuantity"
        render={({ field }) => (
          <TextInput
            style={styles.input}
            placeholder="Stock quantity"
            keyboardType="number-pad"
            value={formattedStock}
            onChangeText={(text) => field.onChange(Number(text))}
            onBlur={field.onBlur}
          />
        )}
      />
      {form.formState.errors.stockQuantity ? (
        <Text style={styles.error}>{form.formState.errors.stockQuantity.message}</Text>
      ) : null}

      <Text style={styles.sectionTitle}>Pickup address</Text>

      <Controller
        control={form.control}
        name="street"
        render={({ field }) => (
          <TextInput style={styles.input} placeholder="Street" value={field.value} onChangeText={field.onChange} />
        )}
      />
      {form.formState.errors.street ? <Text style={styles.error}>{form.formState.errors.street.message}</Text> : null}

      <Controller
        control={form.control}
        name="suburb"
        render={({ field }) => (
          <TextInput style={styles.input} placeholder="Suburb" value={field.value} onChangeText={field.onChange} />
        )}
      />
      {form.formState.errors.suburb ? <Text style={styles.error}>{form.formState.errors.suburb.message}</Text> : null}

      <Controller
        control={form.control}
        name="city"
        render={({ field }) => (
          <TextInput style={styles.input} placeholder="City" value={field.value} onChangeText={field.onChange} />
        )}
      />
      {form.formState.errors.city ? <Text style={styles.error}>{form.formState.errors.city.message}</Text> : null}

      <Controller
        control={form.control}
        name="country"
        render={({ field }) => (
          <TextInput style={styles.input} placeholder="Country" value={field.value} onChangeText={field.onChange} />
        )}
      />
      {form.formState.errors.country ? <Text style={styles.error}>{form.formState.errors.country.message}</Text> : null}

      <Text style={styles.sectionTitle}>Images</Text>
      <Text style={styles.helper}>
        Add up to 5 images. At least 1 is required. (Editing currently requires re-selecting images.)
      </Text>

      <View style={styles.imageGrid}>
        {images.map((image, index) => (
          <View key={`${image.uri}-${index}`} style={styles.imageTile}>
            <Image source={{ uri: image.uri }} style={styles.imagePreview} />
            <Pressable style={styles.removeBadge} onPress={() => handleRemoveImage(index)}>
              <Text style={styles.removeBadgeText}>×</Text>
            </Pressable>
          </View>
        ))}
        {canPickMore ? (
          <Pressable style={styles.addTile} onPress={handleAddImages}>
            <Text style={styles.addTileText}>+ Add</Text>
          </Pressable>
        ) : null}
      </View>

      {screenError ? <Text style={styles.error}>{screenError}</Text> : null}

      <Pressable
        style={[styles.primaryButton, submitDisabled ? styles.disabledButton : null]}
        onPress={submit}
        disabled={submitDisabled}
      >
        <Text style={styles.primaryButtonText}>{isSubmitting ? 'Saving...' : submitLabel}</Text>
      </Pressable>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    padding: 16,
    gap: 10,
    paddingBottom: 28,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: '700',
    marginTop: 6,
  },
  subheading: {
    fontSize: 14,
    fontWeight: '700',
    marginTop: 6,
  },
  helper: {
    color: '#475569',
    fontSize: 12,
    lineHeight: 18,
  },
  input: {
    borderWidth: 1,
    borderColor: '#d4d4d8',
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 10,
    fontSize: 16,
  },
  textarea: {
    minHeight: 110,
    textAlignVertical: 'top',
  },
  error: {
    color: '#dc2626',
    fontWeight: '500',
  },
  categoryGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
  },
  categoryPill: {
    borderWidth: 1,
    borderColor: '#cbd5e1',
    borderRadius: 999,
    paddingHorizontal: 12,
    paddingVertical: 8,
  },
  categoryPillSelected: {
    borderColor: '#0f766e',
    backgroundColor: '#f0fdfa',
  },
  categoryPillText: {
    fontWeight: '700',
    color: '#334155',
    fontSize: 12,
  },
  categoryPillTextSelected: {
    color: '#0f766e',
  },
  imageGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 10,
  },
  imageTile: {
    width: 104,
    height: 104,
    borderRadius: 12,
    overflow: 'hidden',
    position: 'relative',
    backgroundColor: '#f1f5f9',
  },
  imagePreview: {
    width: '100%',
    height: '100%',
  },
  addTile: {
    width: 104,
    height: 104,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#0f766e',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#f0fdfa',
  },
  addTileText: {
    color: '#0f766e',
    fontWeight: '800',
  },
  removeBadge: {
    position: 'absolute',
    top: 6,
    right: 6,
    width: 24,
    height: 24,
    borderRadius: 12,
    backgroundColor: 'rgba(15, 23, 42, 0.7)',
    alignItems: 'center',
    justifyContent: 'center',
  },
  removeBadgeText: {
    color: '#fff',
    fontSize: 18,
    lineHeight: 18,
    marginTop: -1,
  },
  primaryButton: {
    backgroundColor: '#0f766e',
    borderRadius: 12,
    paddingVertical: 14,
    alignItems: 'center',
    marginTop: 8,
  },
  disabledButton: {
    opacity: 0.6,
  },
  primaryButtonText: {
    color: '#ffffff',
    fontSize: 16,
    fontWeight: '800',
  },
});

