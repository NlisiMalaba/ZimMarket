import { useEffect, useMemo, useState } from 'react';
import {
  ActivityIndicator,
  FlatList,
  Image,
  Pressable,
  StyleSheet,
  TextInput,
} from 'react-native';
import { useInfiniteQuery } from '@tanstack/react-query';
import { router } from 'expo-router';

import { Text, View } from '@/components/Themed';
import { productsService } from '@/lib/services/products-service';
import type { Product, ProductCategory } from '@/types/product';

const PAGE_SIZE = 12;
const DEFAULT_USD_TO_ZWL_RATE = 30;
const categories: ProductCategory[] = [
  'All',
  'Groceries',
  'Fresh Produce',
  'Beverages',
  'Household',
  'Snacks',
  'Dairy',
];

const formatCurrency = (value: number, currency: 'USD' | 'ZWL'): string =>
  new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
  }).format(value);

const getProductPriceZwl = (product: Product, usdToZwlRate: number): number =>
  typeof product.priceZwl === 'number' ? product.priceZwl : product.priceUsd * usdToZwlRate;

const ProductCard = ({
  product,
  usdToZwlRate,
}: {
  product: Product;
  usdToZwlRate: number;
}) => (
  <Pressable
    style={styles.card}
    onPress={() =>
      router.push({
        pathname: '/(customer)/product/[id]',
        params: { id: product.id },
      })
    }
  >
    <Image
      source={{
        uri:
          product.imageUrl && product.imageUrl.length > 0
            ? product.imageUrl
            : 'https://placehold.co/600x400/png',
      }}
      style={styles.productImage}
      resizeMode="cover"
    />
    <Text style={styles.productTitle} numberOfLines={2}>
      {product.title}
    </Text>
    <Text style={styles.priceUsd}>{formatCurrency(product.priceUsd, 'USD')}</Text>
    <Text style={styles.priceZwl}>{formatCurrency(getProductPriceZwl(product, usdToZwlRate), 'ZWL')}</Text>
  </Pressable>
);

export default function CustomerHomeScreen() {
  const [selectedCategory, setSelectedCategory] = useState<ProductCategory>('All');
  const [searchInput, setSearchInput] = useState('');
  const [searchTerm, setSearchTerm] = useState('');

  useEffect(() => {
    const timeout = setTimeout(() => {
      setSearchTerm(searchInput.trim());
    }, 350);

    return () => clearTimeout(timeout);
  }, [searchInput]);

  const productsQuery = useInfiniteQuery({
    queryKey: ['products', selectedCategory, searchTerm],
    initialPageParam: 1,
    queryFn: async ({ pageParam }) =>
      productsService.list({
        page: pageParam,
        pageSize: PAGE_SIZE,
        search: searchTerm,
        category: selectedCategory,
      }),
    getNextPageParam: (lastPage) => (lastPage.hasNextPage ? lastPage.page + 1 : undefined),
  });

  const products = useMemo(
    () => productsQuery.data?.pages.flatMap((page) => page.items) ?? [],
    [productsQuery.data?.pages]
  );
  const usdToZwlRate =
    productsQuery.data?.pages.find((page) => typeof page.usdToZwlRate === 'number')?.usdToZwlRate ??
    DEFAULT_USD_TO_ZWL_RATE;

  const renderHeader = () => (
    <View style={styles.headerArea}>
      <Text style={styles.heading}>Browse products</Text>
      <TextInput
        style={styles.searchInput}
        placeholder="Search for products"
        value={searchInput}
        onChangeText={setSearchInput}
      />
      <FlatList
        data={categories}
        keyExtractor={(item) => item}
        horizontal
        showsHorizontalScrollIndicator={false}
        contentContainerStyle={styles.categoriesContainer}
        renderItem={({ item }) => {
          const isSelected = item === selectedCategory;

          return (
            <Pressable
              style={[styles.categoryChip, isSelected ? styles.categoryChipActive : null]}
              onPress={() => setSelectedCategory(item)}
            >
              <Text style={[styles.categoryText, isSelected ? styles.categoryTextActive : null]}>
                {item}
              </Text>
            </Pressable>
          );
        }}
      />
    </View>
  );

  if (productsQuery.isLoading) {
    return (
      <View style={styles.stateContainer}>
        <ActivityIndicator size="large" color="#0f766e" />
      </View>
    );
  }

  if (productsQuery.isError) {
    return (
      <View style={styles.stateContainer}>
        <Text style={styles.errorText}>Failed to load products. Pull to refresh and try again.</Text>
      </View>
    );
  }

  return (
    <FlatList
      data={products}
      keyExtractor={(item) => item.id}
      numColumns={2}
      columnWrapperStyle={styles.column}
      contentContainerStyle={styles.listContent}
      ListHeaderComponent={renderHeader}
      renderItem={({ item }) => <ProductCard product={item} usdToZwlRate={usdToZwlRate} />}
      onEndReachedThreshold={0.5}
      onEndReached={() => {
        if (productsQuery.hasNextPage && !productsQuery.isFetchingNextPage) {
          productsQuery.fetchNextPage();
        }
      }}
      refreshing={productsQuery.isRefetching}
      onRefresh={() => {
        void productsQuery.refetch();
      }}
      ListFooterComponent={
        productsQuery.isFetchingNextPage ? (
          <View style={styles.footer}>
            <ActivityIndicator size="small" color="#0f766e" />
          </View>
        ) : null
      }
      ListEmptyComponent={
        <View style={styles.emptyState}>
          <Text>No products found for the selected filters.</Text>
        </View>
      }
    />
  );
}

const styles = StyleSheet.create({
  headerArea: {
    gap: 12,
    marginBottom: 12,
  },
  heading: {
    fontSize: 24,
    fontWeight: '700',
    marginTop: 8,
  },
  searchInput: {
    borderWidth: 1,
    borderColor: '#d4d4d8',
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 10,
    fontSize: 16,
    backgroundColor: '#ffffff',
  },
  categoriesContainer: {
    paddingVertical: 2,
    gap: 8,
  },
  categoryChip: {
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: '#d4d4d8',
  },
  categoryChipActive: {
    backgroundColor: '#0f766e',
    borderColor: '#0f766e',
  },
  categoryText: {
    fontSize: 13,
    fontWeight: '600',
    color: '#374151',
  },
  categoryTextActive: {
    color: '#ffffff',
  },
  listContent: {
    paddingHorizontal: 16,
    paddingBottom: 24,
  },
  column: {
    gap: 12,
  },
  card: {
    flex: 1,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#e5e7eb',
    padding: 10,
    gap: 4,
  },
  productImage: {
    width: '100%',
    height: 110,
    borderRadius: 8,
    backgroundColor: '#f3f4f6',
    marginBottom: 4,
  },
  productTitle: {
    fontSize: 14,
    fontWeight: '600',
    minHeight: 36,
  },
  priceUsd: {
    fontSize: 14,
    fontWeight: '700',
  },
  priceZwl: {
    fontSize: 12,
    color: '#4b5563',
  },
  stateContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 24,
  },
  errorText: {
    textAlign: 'center',
    color: '#dc2626',
  },
  footer: {
    paddingVertical: 16,
  },
  emptyState: {
    paddingVertical: 24,
    alignItems: 'center',
  },
});
