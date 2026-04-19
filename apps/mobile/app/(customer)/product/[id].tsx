import { useMemo } from 'react';
import {
  ActivityIndicator,
  Alert,
  FlatList,
  Image,
  Pressable,
  StyleSheet,
  useWindowDimensions,
} from 'react-native';
import { useLocalSearchParams } from 'expo-router';
import { useQuery } from '@tanstack/react-query';

import { Text, View } from '@/components/Themed';
import { productsService } from '@/lib/services/products-service';
import { useCartStore } from '@/store/cart-store';
import { toCartItemFromProduct } from '@/types/cart';
import type { Product } from '@/types/product';

const DEFAULT_USD_TO_ZWL_RATE = 30;

const readParam = (value: string | string[] | undefined): string => {
  if (Array.isArray(value)) {
    return value[0] ?? '';
  }

  return value ?? '';
};

const formatCurrency = (value: number, currency: 'USD' | 'ZWL'): string =>
  new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
  }).format(value);

const buildImageUrls = (product: Product): string[] => {
  const source = product.imageUrls && product.imageUrls.length > 0 ? product.imageUrls : [product.imageUrl];

  const normalized = source
    .filter((item): item is string => Boolean(item && item.trim().length > 0))
    .slice(0, 5);

  return normalized.length > 0 ? normalized : ['https://placehold.co/1000x700/png'];
};

export default function ProductDetailScreen() {
  const params = useLocalSearchParams();
  const { width } = useWindowDimensions();
  const productId = readParam(params.id);
  const addItem = useCartStore((state) => state.addItem);

  const productQuery = useQuery({
    queryKey: ['product', productId],
    enabled: productId.length > 0,
    queryFn: async () => productsService.getById(productId),
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
        <Text style={styles.errorText}>Unable to load product details right now.</Text>
      </View>
    );
  }

  const product = productQuery.data;
  const imageUrls = buildImageUrls(product);
  const stockQty = product.stockQuantity ?? 0;
  const inStock = stockQty > 0;
  const zwlPrice = product.priceZwl ?? product.priceUsd * DEFAULT_USD_TO_ZWL_RATE;

  return (
    <View style={styles.container}>
      <FlatList
        data={imageUrls}
        keyExtractor={(item, index) => `${item}-${index}`}
        horizontal
        pagingEnabled
        showsHorizontalScrollIndicator={false}
        renderItem={({ item }) => (
          <Image source={{ uri: item }} style={[styles.heroImage, { width: width - 32 }]} resizeMode="cover" />
        )}
        style={styles.carousel}
      />

      <View style={styles.content}>
        <View style={styles.badgeRow}>
          <Text style={[styles.stockBadge, inStock ? styles.stockBadgeInStock : styles.stockBadgeOutOfStock]}>
            {inStock ? `In stock (${stockQty})` : 'Out of stock'}
          </Text>
        </View>

        <Text style={styles.title}>{product.title}</Text>
        <Text style={styles.description}>
          {product.description?.trim() || 'No product description has been provided yet.'}
        </Text>

        <Text style={styles.seller}>Sold by: {product.sellerName?.trim() || 'ZimMarket Seller'}</Text>
        <Text style={styles.priceUsd}>{formatCurrency(product.priceUsd, 'USD')}</Text>
        <Text style={styles.priceZwl}>{formatCurrency(zwlPrice, 'ZWL')}</Text>
      </View>

      <Pressable
        style={[styles.addToCartButton, !inStock ? styles.disabledButton : null]}
        disabled={!inStock}
        onPress={() => {
          addItem(toCartItemFromProduct(product));
          Alert.alert('Added to cart', 'Product has been added to your cart.');
        }}
      >
        <Text style={styles.addToCartText}>Add to Cart</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 16,
    gap: 12,
  },
  carousel: {
    flexGrow: 0,
  },
  heroImage: {
    height: 240,
    borderRadius: 12,
    marginRight: 8,
    backgroundColor: '#f3f4f6',
  },
  content: {
    gap: 8,
  },
  badgeRow: {
    flexDirection: 'row',
  },
  stockBadge: {
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 999,
    fontWeight: '700',
    fontSize: 12,
  },
  stockBadgeInStock: {
    backgroundColor: '#dcfce7',
    color: '#166534',
  },
  stockBadgeOutOfStock: {
    backgroundColor: '#fee2e2',
    color: '#b91c1c',
  },
  title: {
    fontSize: 24,
    fontWeight: '700',
  },
  description: {
    fontSize: 14,
    lineHeight: 20,
    color: '#374151',
  },
  seller: {
    fontSize: 14,
    fontWeight: '500',
    color: '#4b5563',
  },
  priceUsd: {
    fontSize: 22,
    fontWeight: '800',
  },
  priceZwl: {
    fontSize: 14,
    color: '#4b5563',
  },
  addToCartButton: {
    marginTop: 'auto',
    backgroundColor: '#0f766e',
    borderRadius: 12,
    paddingVertical: 14,
    alignItems: 'center',
  },
  disabledButton: {
    opacity: 0.55,
  },
  addToCartText: {
    color: '#ffffff',
    fontWeight: '700',
    fontSize: 16,
  },
  stateContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 24,
  },
  errorText: {
    color: '#dc2626',
    textAlign: 'center',
  },
});
