import { ActivityIndicator, Pressable, StyleSheet } from 'react-native';
import { router, useLocalSearchParams } from 'expo-router';
import { WebView } from 'react-native-webview';

import { Text, View } from '@/components/Themed';

const readParam = (value: string | string[] | undefined): string => {
  if (Array.isArray(value)) {
    return value[0] ?? '';
  }

  return value ?? '';
};

export default function PaymentScreen() {
  const params = useLocalSearchParams();
  const orderId = readParam(params.orderId);
  const redirectUrl = readParam(params.redirectUrl);

  const isSuccessRedirectUrl = (url: string): boolean => {
    const normalizedUrl = url.toLowerCase();
    const hasSuccessStatus =
      normalizedUrl.includes('status=success') || normalizedUrl.includes('paymentstatus=success');
    const hasSuccessPath = normalizedUrl.includes('/success') || normalizedUrl.includes('/confirmed');
    const isAppDeepLink = normalizedUrl.startsWith('mobile://');

    return isAppDeepLink && (hasSuccessStatus || hasSuccessPath);
  };

  const handleNavigation = (url: string) => {
    if (!isSuccessRedirectUrl(url)) {
      return;
    }

    router.replace({
      pathname: '/(customer)/order-confirmed',
      params: {
        orderId: orderId || 'Unknown',
      },
    });
  };

  if (redirectUrl.length === 0) {
    return (
      <View style={styles.container}>
        <Text style={styles.title}>Complete payment</Text>
        <Text style={styles.label}>Order ID</Text>
        <Text style={styles.value}>{orderId || 'Unknown order'}</Text>
        <Text style={styles.errorText}>Payment URL missing for this order.</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Complete payment</Text>
        <Text style={styles.label}>Order ID</Text>
        <Text style={styles.value}>{orderId || 'Unknown order'}</Text>
      </View>
      <WebView
        source={{ uri: redirectUrl }}
        startInLoadingState
        renderLoading={() => (
          <View style={styles.loadingContainer}>
            <ActivityIndicator size="large" color="#0f766e" />
          </View>
        )}
        onNavigationStateChange={(navigationState) => {
          handleNavigation(navigationState.url);
        }}
        onShouldStartLoadWithRequest={(request) => {
          handleNavigation(request.url);
          return true;
        }}
      />
      <Pressable
        style={styles.button}
        onPress={() => {
          router.replace('/(customer)');
        }}
      >
        <Text style={styles.buttonText}>Cancel payment</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  header: {
    paddingHorizontal: 16,
    paddingTop: 16,
    paddingBottom: 8,
    gap: 4,
  },
  title: {
    fontSize: 24,
    fontWeight: '700',
    marginBottom: 4,
  },
  label: {
    fontSize: 13,
    color: '#4b5563',
    fontWeight: '600',
    marginTop: 8,
  },
  value: {
    fontSize: 14,
    lineHeight: 20,
  },
  button: {
    margin: 16,
    backgroundColor: '#0f766e',
    borderRadius: 12,
    alignItems: 'center',
    paddingVertical: 13,
  },
  buttonText: {
    color: '#ffffff',
    fontSize: 15,
    fontWeight: '700',
  },
  loadingContainer: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#ffffff',
  },
  errorText: {
    color: '#dc2626',
    fontWeight: '600',
    marginTop: 10,
    paddingHorizontal: 16,
  },
});
